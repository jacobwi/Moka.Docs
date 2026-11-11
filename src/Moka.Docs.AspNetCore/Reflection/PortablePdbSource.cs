using System.Buffers.Binary;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Moka.Docs.AspNetCore.Reflection;

/// <summary>
///     What an assembly's portable PDB says about its source: the documents each type is declared
///     in, the SourceLink map, and optionally the text of the C# documents.
/// </summary>
/// <remarks>
///     Types have no debug information of their own. Their documents and lines come from the
///     sequence points of their methods, including the lambdas, iterators and async methods the
///     compiler moves into nested types. A type without method bodies, such as an enum or an
///     interface, gets its documents from the compiler's type-definition-documents record, with
///     no line.
/// </remarks>
internal sealed class PortablePdbSource
{
	private static readonly Guid _sourceLinkKind = new("CC110556-A091-4D38-9FEC-25AB9A351A6A");
	private static readonly Guid _embeddedSourceKind = new("0E8A571B-6926-466E-B4AD-8AB04611F5FE");
	private static readonly Guid _typeDefinitionDocumentsKind = new("932E74BC-DBA9-4478-8D46-0F32A7BAB3D3");
	private static readonly Guid _csharpLanguage = new("3F5162F8-07C6-11D3-9053-00C04FA302A1");

	// A corrupt size field must not turn into a multi-gigabyte allocation.
	private const int _maxEmbeddedSourceBytes = 64 * 1024 * 1024;

	private readonly List<PdbDocument> _documents;
	private readonly SourceLinkMap? _sourceLink;

	/// <summary>Per type, by metadata token: the type's documents in PDB order, with its first line in each.</summary>
	private readonly Dictionary<int, SortedDictionary<int, int?>> _typeDocuments;

	private PortablePdbSource(
		List<PdbDocument> documents,
		Dictionary<int, SortedDictionary<int, int?>> typeDocuments,
		SourceLinkMap? sourceLink)
	{
		_documents = documents;
		_typeDocuments = typeDocuments;
		_sourceLink = sourceLink;
	}

	/// <summary>The documents the PDB lists, in its order.</summary>
	public IReadOnlyList<PdbDocument> Documents => _documents;

	/// <summary>
	///     Reads the PDB of the assembly at <paramref name="assemblyPath" />: the <c>.pdb</c> file next
	///     to it that the assembly names, or else the PDB embedded in it.
	/// </summary>
	/// <param name="assemblyPath">The assembly file.</param>
	/// <param name="readSourceText">
	///     Whether to read the text of the C# documents, from the PDB when it embeds them or else
	///     from disk.
	/// </param>
	/// <returns>Null when the assembly has no portable PDB.</returns>
	/// <exception cref="Exception">The assembly or its PDB can't be read.</exception>
	public static PortablePdbSource? TryRead(string assemblyPath, bool readSourceText)
	{
		using FileStream peStream = File.OpenRead(assemblyPath);
		using var peReader = new PEReader(peStream);

		// Checks that the PDB belongs to this build of the assembly, so a stale one is ignored.
		if (!peReader.TryOpenAssociatedPortablePdb(assemblyPath, OpenIfExists, out MetadataReaderProvider? provider, out _)
		    || provider is null)
		{
			return null;
		}

		using (provider)
		{
			MetadataReader pdb = provider.GetMetadataReader();
			MetadataReader metadata = peReader.GetMetadataReader();

			var embeddedSources = new Dictionary<DocumentHandle, BlobHandle>();
			var typeDocuments = new Dictionary<int, SortedDictionary<int, int?>>();
			SourceLinkMap? sourceLink = null;

			foreach (CustomDebugInformationHandle handle in pdb.CustomDebugInformation)
			{
				CustomDebugInformation info = pdb.GetCustomDebugInformation(handle);
				Guid kind = pdb.GetGuid(info.Kind);

				if (kind == _sourceLinkKind && info.Parent.Kind == HandleKind.ModuleDefinition)
				{
					sourceLink = SourceLinkMap.Parse(Encoding.UTF8.GetString(pdb.GetBlobBytes(info.Value)));
				}
				else if (kind == _embeddedSourceKind && info.Parent.Kind == HandleKind.Document)
				{
					embeddedSources[(DocumentHandle)info.Parent] = info.Value;
				}
				else if (kind == _typeDefinitionDocumentsKind && info.Parent.Kind == HandleKind.TypeDefinition)
				{
					int typeToken = MetadataTokens.GetToken(info.Parent);
					BlobReader reader = pdb.GetBlobReader(info.Value);
					while (reader.RemainingBytes > 0)
					{
						AddLocation(typeDocuments, typeToken, reader.ReadCompressedInteger(), null);
					}
				}
			}

			// The table is empty when the PDB carries no method information.
			if (pdb.MethodDebugInformation.Count == metadata.MethodDefinitions.Count)
			{
				foreach (MethodDefinitionHandle method in metadata.MethodDefinitions)
				{
					MethodDebugInformation info = pdb.GetMethodDebugInformation(method.ToDebugInformationHandle());
					if (info.SequencePointsBlob.IsNil)
					{
						continue;
					}

					int typeToken = MetadataTokens.GetToken(
						SourceOwner(metadata, metadata.GetMethodDefinition(method).GetDeclaringType()));
					foreach (SequencePoint point in info.GetSequencePoints())
					{
						if (!point.IsHidden && !point.Document.IsNil)
						{
							AddLocation(typeDocuments, typeToken, MetadataTokens.GetRowNumber(point.Document), point.StartLine);
						}
					}
				}
			}

			var documents = new List<PdbDocument>(pdb.Documents.Count);
			foreach (DocumentHandle handle in pdb.Documents)
			{
				Document document = pdb.GetDocument(handle);
				string path = pdb.GetString(document.Name);
				bool isCSharp = pdb.GetGuid(document.Language) == _csharpLanguage;
				bool isEmbedded = embeddedSources.TryGetValue(handle, out BlobHandle embedded);

				string? text = null;
				if (readSourceText && isCSharp)
				{
					text = isEmbedded ? DecodeEmbeddedSource(pdb.GetBlobBytes(embedded)) : ReadFromDisk(path);
				}

				documents.Add(new PdbDocument(path, isCSharp, isEmbedded, text));
			}

			return new PortablePdbSource(documents, typeDocuments, sourceLink);
		}
	}

	/// <summary>
	///     The page of the type's source in its repository, from the SourceLink map. Null when the
	///     PDB has no map or the map covers none of the type's documents.
	/// </summary>
	public string? GetSourceUrl(Type type)
	{
		if (_sourceLink is null || !_typeDocuments.TryGetValue(type.MetadataToken, out SortedDictionary<int, int?>? locations))
		{
			return null;
		}

		(string Url, int? Line, bool IsEmbedded)? first = null;
		foreach ((int row, int? line) in locations)
		{
			if (row < 1 || row > _documents.Count)
			{
				continue;
			}

			PdbDocument document = _documents[row - 1];
			if (_sourceLink.GetUrl(document.Path) is not { } url || !IsWebUrl(url))
			{
				continue;
			}

			// EmbedUntrackedSources puts the files git doesn't track, such as generated ones, in the
			// PDB. They aren't in the repository, so a link to one of them would be a 404. Unless the
			// project embeds every file, a document that isn't embedded is a real one.
			if (!document.IsEmbedded)
			{
				return SourceLinkMap.ToBrowseUrl(url, line);
			}

			first ??= (url, line, true);
		}

		return first is { } fallback ? SourceLinkMap.ToBrowseUrl(fallback.Url, fallback.Line) : null;
	}

	private static Stream? OpenIfExists(string path) => File.Exists(path) ? File.OpenRead(path) : null;

	/// <summary>
	///     The type whose source holds a method: the method's own type, unless that is one the
	///     compiler made for a lambda, an iterator or an async method, whose names start with
	///     <c>&lt;</c>. Those belong to the type that declares them.
	/// </summary>
	private static TypeDefinitionHandle SourceOwner(MetadataReader metadata, TypeDefinitionHandle handle)
	{
		TypeDefinition type = metadata.GetTypeDefinition(handle);
		while (metadata.StringComparer.StartsWith(type.Name, "<") && type.GetDeclaringType() is { IsNil: false } declaring)
		{
			handle = declaring;
			type = metadata.GetTypeDefinition(handle);
		}

		return handle;
	}

	private static void AddLocation(Dictionary<int, SortedDictionary<int, int?>> typeDocuments, int typeToken, int documentRow,
		int? line)
	{
		if (!typeDocuments.TryGetValue(typeToken, out SortedDictionary<int, int?>? locations))
		{
			locations = [];
			typeDocuments[typeToken] = locations;
		}

		if (!locations.TryGetValue(documentRow, out int? first) || first is null || (line is not null && line < first))
		{
			locations[documentRow] = line ?? first;
		}
	}

	/// <summary>
	///     An embedded source blob: a 32-bit size, zero when the text follows as is, else the size of
	///     the text the deflate-compressed rest expands to.
	/// </summary>
	private static string? DecodeEmbeddedSource(byte[] blob)
	{
		if (blob.Length < 4)
		{
			return null;
		}

		int size = BinaryPrimitives.ReadInt32LittleEndian(blob);
		byte[] bytes;
		if (size == 0)
		{
			bytes = blob[4..];
		}
		else if (size is > 0 and <= _maxEmbeddedSourceBytes)
		{
			bytes = new byte[size];
			using var deflate = new DeflateStream(new MemoryStream(blob, 4, blob.Length - 4), CompressionMode.Decompress);
			deflate.ReadExactly(bytes);
		}
		else
		{
			return null;
		}

		using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, true);
		return reader.ReadToEnd();
	}

	private static string? ReadFromDisk(string path)
	{
		try
		{
			// A path from a deterministic build, such as /_/src/File.cs, names no file here.
			return File.Exists(path) ? File.ReadAllText(path) : null;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
		{
			return null;
		}
	}

	private static bool IsWebUrl(string url) =>
		Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}

/// <summary>
///     A source document a PDB lists.
/// </summary>
/// <param name="Path">The path the compiler saw, possibly mapped, such as <c>/_/src/File.cs</c>.</param>
/// <param name="IsCSharp">Whether it is C# source.</param>
/// <param name="IsEmbedded">Whether the PDB holds the document's text.</param>
/// <param name="Text">The text, when it was asked for and could be read.</param>
internal sealed record PdbDocument(string Path, bool IsCSharp, bool IsEmbedded, string? Text);
