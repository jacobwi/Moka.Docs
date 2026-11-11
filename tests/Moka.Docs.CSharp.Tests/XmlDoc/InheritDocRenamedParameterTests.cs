using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.Metadata;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.CSharp.Tests.XmlDoc;

/// <summary>
///     Inherited <c>&lt;param&gt;</c> and <c>&lt;typeparam&gt;</c> docs follow the parameter's position when
///     the inheriting member renames it.
/// </summary>
public sealed class InheritDocRenamedParameterTests
{
	private readonly AssemblyAnalyzer _analyzer = new(NullLogger<AssemblyAnalyzer>.Instance);
	private readonly InheritDocResolver _resolver = new();

	private XmlDocBlock Doc(string typeName, string memberName)
	{
		ApiReference api = _resolver.Resolve(_analyzer.AnalyzeSyntaxTrees([CSharpSyntaxTree.ParseText("""
			namespace Lib
			{
			    public interface IStore
			    {
			        /// <summary>Saves.</summary>
			        /// <typeparam name="T">The item type.</typeparam>
			        /// <param name="value">The value.</param>
			        /// <param name="count">How many.</param>
			        void Save<T>(T value, int count);
			    }

			    public class Store : IStore
			    {
			        /// <inheritdoc />
			        public void Save<TItem>(TItem item, int times) { }

			        /// <inheritdoc cref="IStore.Save{T}(T, int)" />
			        /// <param name="copies">Copies to keep.</param>
			        public void Archive<TEntry>(TEntry entry, int copies) { }
			    }
			}
			""")], "Lib"));

		return api.Namespaces.SelectMany(ns => ns.Types).Single(t => t.Name == typeName)
			.Members.Single(m => m.Name == memberName).Documentation!;
	}

	[Fact]
	public void InheritDoc_RenamedParameters_KeepTheirDescriptions()
	{
		XmlDocBlock doc = Doc("Store", "Save");

		doc.Parameters.Should().BeEquivalentTo(new Dictionary<string, string>
		{
			["item"] = "The value.",
			["times"] = "How many."
		});
		doc.TypeParameters.Should().BeEquivalentTo(new Dictionary<string, string> { ["TItem"] = "The item type." });
	}

	[Fact]
	public void InheritDocCref_RenamedParameters_KeepTheirDescriptions_AndOwnTagsWin()
	{
		XmlDocBlock doc = Doc("Store", "Archive");

		doc.Parameters.Should().BeEquivalentTo(new Dictionary<string, string>
		{
			["entry"] = "The value.",
			["copies"] = "Copies to keep."
		});
		doc.TypeParameters.Should().BeEquivalentTo(new Dictionary<string, string> { ["TEntry"] = "The item type." });
	}
}
