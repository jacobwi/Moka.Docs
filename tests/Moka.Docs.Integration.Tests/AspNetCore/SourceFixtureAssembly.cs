using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     A library compiled at test time with a portable PDB, for View Source, SourceLink, namespace
///     filters and XML documentation files. The same source is emitted three ways, each as its own
///     assembly so all three load side by side.
/// </summary>
internal static class SourceFixtureAssembly
{
	/// <summary>The commit the SourceLink URLs point at.</summary>
	public const string Commit = "0123456789abcdef0123456789abcdef01234567";

	/// <summary>Repository-relative path of the main part of <c>Widget</c>.</summary>
	public const string WidgetPath = "src/Widgets/Widget.cs";

	/// <summary>Repository-relative path of the enum, the interface and <c>Sequences</c>.</summary>
	public const string KindsPath = "src/Widgets/Kinds.cs";

	/// <summary>The main part of <c>Widget</c>, with members the page and the source panel leave out.</summary>
	public const string WidgetSource = """
		using System.Collections.Generic;
		using System.ComponentModel;

		namespace Contoso.Widgets
		{
		    /// <summary>A widget.</summary>
		    public partial class Widget
		    {
		        /// <summary>The widget's name.</summary>
		        public string Name { get; set; } = "";

		        /// <summary>Spins the widget.</summary>
		        /// <returns>The number of turns.</returns>
		        public int Spin()
		        {
		            return Helper() + 1;
		        }

		        /// <summary>For the framework only.</summary>
		        [EditorBrowsable(EditorBrowsableState.Never)]
		        public void Legacy() { }

		        /// <summary>Called when the widget spins.</summary>
		        protected virtual void OnSpin() { }

		        /// <summary>Only for this assembly.</summary>
		        internal void InternalHelper() { }

		        private int Helper() => 41;
		    }
		}
		""";

	/// <summary>
	///     The second part of <c>Widget</c>. Under 200 bytes, so the compiler embeds it uncompressed;
	///     the other files are deflated.
	/// </summary>
	public const string WidgetPartSource = """
		namespace Contoso.Widgets
		{
		    public partial class Widget
		    {
		        /// <summary>Resets the widget.</summary>
		        public void Reset()
		        {
		        }
		    }
		}
		""";

	/// <summary>Types without method bodies, and one whose only code is an iterator.</summary>
	public const string KindsSource = """
		using System.Collections.Generic;
		using System.ComponentModel;

		namespace Contoso.Widgets;

		/// <summary>Kinds of widget.</summary>
		public enum WidgetKind
		{
		    /// <summary>Round.</summary>
		    Round,

		    /// <summary>Kept for old callers.</summary>
		    [EditorBrowsable(EditorBrowsableState.Never)]
		    Legacy
		}

		/// <summary>Something that spins.</summary>
		public interface ISpinner
		{
		    /// <summary>Spins once.</summary>
		    int Spin();
		}

		/// <summary>Number sequences.</summary>
		public static class Sequences
		{
		    /// <summary>Counts to two.</summary>
		    public static IEnumerable<int> Count()
		    {
		        yield return 1;
		        yield return 2;
		    }
		}
		""";

	/// <summary>Namespaces for the filters: nested ones, a lookalike prefix, the global one and a hidden type.</summary>
	public const string FiltersSource = """
		using System.ComponentModel;

		namespace Contoso.Internal
		{
		    /// <summary>The base of every entity.</summary>
		    public abstract class EntityBase
		    {
		        /// <summary>Describes the entity.</summary>
		        public abstract string Describe();
		    }
		}

		namespace Contoso.Api
		{
		    /// <summary>An endpoint.</summary>
		    public class Endpoint
		    {
		    }

		    /// <summary>Only for tooling.</summary>
		    [EditorBrowsable(EditorBrowsableState.Never)]
		    public class HiddenEndpoint
		    {
		        /// <summary>Nested in a hidden type.</summary>
		        public class Nested
		        {
		        }
		    }
		}

		namespace Contoso.Api.Models
		{
		    /// <summary>An order, built on <see cref="Contoso.Internal.EntityBase" />.</summary>
		    public class Order : Contoso.Internal.EntityBase
		    {
		        /// <inheritdoc />
		        public override string Describe() => "order";
		    }
		}

		namespace Contoso.Apis
		{
		    /// <summary>Not part of Contoso.Api.</summary>
		    public class Lookalike
		    {
		    }
		}

		/// <summary>Declared outside any namespace.</summary>
		public class GlobalThing
		{
		}
		""";

	// What EmbedUntrackedSources puts in a PDB: a generated part git doesn't track. It comes
	// first in the compilation, so a link to the first document would point at it.
	private const string _generatedPath = "obj/Generated/Widget.g.cs";

	private const string _generatedSource = """
		namespace Contoso.Widgets
		{
		    public partial class Widget
		    {
		        /// <summary>Generated.</summary>
		        public int Generated() => 1;
		    }
		}
		""";

	private static readonly Lazy<Assembly> _sidecar = new(() => Emit(Variant.Sidecar));
	private static readonly Lazy<Assembly> _embedded = new(() => Emit(Variant.Embedded));
	private static readonly Lazy<Assembly> _noPdb = new(() => Emit(Variant.NoPdb));

	/// <summary>
	///     A <c>.pdb</c> next to the DLL, with a GitHub SourceLink map and source paths that exist on
	///     disk. Only the generated part is embedded. The XML file is next to the DLL.
	/// </summary>
	public static Assembly Sidecar => _sidecar.Value;

	/// <summary>
	///     A PDB embedded in the DLL with every source file, deterministic <c>/_/</c> paths that
	///     name no file and a GitLab SourceLink map. The XML file is elsewhere:
	///     <see cref="EmbeddedXmlPath" />.
	/// </summary>
	public static Assembly Embedded => _embedded.Value;

	/// <summary>No PDB at all. The XML file is next to the DLL.</summary>
	public static Assembly NoPdb => _noPdb.Value;

	/// <summary>The XML documentation of <see cref="Embedded" />, in a folder of its own under another name.</summary>
	public static string EmbeddedXmlPath =>
		Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Embedded.Location)!, "..", "xml", "fixture-docs.xml"));

	/// <summary>The 1-based line of the first line of <paramref name="source" /> containing <paramref name="text" />.</summary>
	public static int LineOf(string source, string text)
	{
		string[] lines = source.Split('\n');
		int index = Array.FindIndex(lines, line => line.Contains(text, StringComparison.Ordinal));
		if (index < 0)
		{
			throw new ArgumentException($"'{text}' is not in the source.", nameof(text));
		}

		return index + 1;
	}

	private static Assembly Emit(Variant variant)
	{
		string root = Path.Combine(AppContext.BaseDirectory, "source-fixture", variant.ToString().ToLowerInvariant());
		try
		{
			return Assembly.LoadFrom(EmitInto(variant, root));
		}
		catch (IOException)
		{
			// Another test process for this framework still has the fixture loaded.
			return Assembly.LoadFrom(EmitInto(variant, $"{root}-{Environment.ProcessId}"));
		}
	}

	private static string EmitInto(Variant variant, string root)
	{
		string name = "Moka.Docs.SourceFixture." + variant;
		bool deterministic = variant == Variant.Embedded;

		var files = new List<(string RelativePath, string Text, bool Embed)>();
		if (variant == Variant.Sidecar)
		{
			files.Add((_generatedPath, _generatedSource, true));
		}

		files.Add((WidgetPath, WidgetSource, deterministic));
		files.Add(("src/Widgets/Widget.Part.cs", WidgetPartSource, deterministic));
		files.Add((KindsPath, KindsSource, deterministic));
		files.Add(("src/Filters.cs", FiltersSource, deterministic));

		var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose);
		var trees = new List<SyntaxTree>();
		var embeddedTexts = new List<EmbeddedText>();
		foreach ((string relativePath, string text, bool embed) in files)
		{
			// A deterministic build maps the project folder to /_/, which names no file on this machine.
			string path = deterministic
				? "/_/" + relativePath
				: Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
			SourceText sourceText = SourceText.From(text, Encoding.UTF8);
			SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceText, parseOptions, path);
			trees.Add(tree);

			if (embed)
			{
				embeddedTexts.Add(EmbeddedText.FromSource(path, sourceText));
			}
			else if (!deterministic)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, text);
			}
		}

		string platformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? "";
		CSharpCompilation compilation = CSharpCompilation.Create(
			name,
			trees,
			platformAssemblies
				.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
				.Select(path => MetadataReference.CreateFromFile(path)),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		string binDirectory = Path.Combine(root, "bin");
		Directory.CreateDirectory(binDirectory);
		string dllPath = Path.Combine(binDirectory, name + ".dll");
		string pdbPath = Path.ChangeExtension(dllPath, ".pdb");
		string xmlPath = deterministic
			? Path.Combine(root, "xml", "fixture-docs.xml")
			: Path.ChangeExtension(dllPath, ".xml");
		Directory.CreateDirectory(Path.GetDirectoryName(xmlPath)!);

		string sourceLinkJson = JsonSerializer.Serialize(new
		{
			documents = new Dictionary<string, string>
			{
				[deterministic ? "/_/*" : root + Path.DirectorySeparatorChar + "*"] = deterministic
					? $"https://gitlab.com/contoso/widgets/-/raw/{Commit}/*"
					: $"https://raw.githubusercontent.com/contoso/widgets/{Commit}/*"
			}
		});

		EmitResult result;
		using (FileStream pe = File.Create(dllPath))
		using (FileStream xml = File.Create(xmlPath))
		using (FileStream? pdb = variant == Variant.Sidecar ? File.Create(pdbPath) : null)
		using (var sourceLink = new MemoryStream(Encoding.UTF8.GetBytes(sourceLinkJson)))
		{
			result = variant switch
			{
				Variant.Sidecar => compilation.Emit(pe, pdb, xml, sourceLinkStream: sourceLink, embeddedTexts: embeddedTexts,
					options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: pdbPath)),
				Variant.Embedded => compilation.Emit(pe, xmlDocumentationStream: xml, sourceLinkStream: sourceLink,
					embeddedTexts: embeddedTexts, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded)),
				_ => compilation.Emit(pe, xmlDocumentationStream: xml)
			};
		}

		if (!result.Success)
		{
			throw new InvalidOperationException(
				"The source fixture did not compile: " + string.Join(Environment.NewLine, result.Diagnostics));
		}

		return dllPath;
	}

	private enum Variant
	{
		Sidecar,
		Embedded,
		NoPdb
	}
}
