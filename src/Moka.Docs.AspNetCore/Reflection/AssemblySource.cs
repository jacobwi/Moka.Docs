using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Moka.Docs.AspNetCore.Reflection;

/// <summary>
///     The source information of one assembly, read once per build: its PDB for links, and its
///     parsed declarations when <see cref="MokaDocsOptions.IncludeSourceCode" /> is on.
/// </summary>
internal sealed class AssemblySource
{
	private AssemblySource(PortablePdbSource? pdb, TypeDeclarationSource? declarations)
	{
		Pdb = pdb;
		Declarations = declarations;
	}

	/// <summary>The assembly's portable PDB, or null when it has none that could be read.</summary>
	public PortablePdbSource? Pdb { get; }

	/// <summary>The type declarations in its C# source, or null when that isn't wanted or available.</summary>
	public TypeDeclarationSource? Declarations { get; }

	/// <summary>
	///     Reads the assembly's PDB and, when asked, its source. Never throws: an assembly without a
	///     readable PDB gets no links and no source.
	/// </summary>
	public static AssemblySource Open(Assembly assembly, MokaDocsOptions options, ILogger logger)
	{
		string name = assembly.GetName().Name ?? "";
		LogLevel level = options.IncludeSourceCode ? LogLevel.Information : LogLevel.Debug;

		if (string.IsNullOrEmpty(assembly.Location))
		{
			logger.Log(level, "No source for assembly {Assembly}: it was not loaded from a file", name);
			return new AssemblySource(null, null);
		}

		PortablePdbSource? pdb;
		TypeDeclarationSource? declarations = null;
		try
		{
			pdb = PortablePdbSource.TryRead(assembly.Location, options.IncludeSourceCode);
			if (pdb is not null && options.IncludeSourceCode)
			{
				declarations = TypeDeclarationSource.Create(name, pdb.Documents);
			}
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			// Any failure reading a PDB, a format this reader doesn't know included, costs the
			// pages their source and nothing else.
			logger.Log(level, ex, "Could not read the PDB of assembly {Assembly}", name);
			return new AssemblySource(null, null);
		}

		if (pdb is null)
		{
			logger.Log(level, "No source for assembly {Assembly}: no portable PDB next to {Location} or embedded in it",
				name, assembly.Location);
		}
		else if (options.IncludeSourceCode && declarations is null)
		{
			logger.LogInformation(
				"No source for assembly {Assembly}: its PDB neither embeds its C# files nor names files that exist here",
				name);
		}

		return new AssemblySource(pdb, declarations);
	}
}
