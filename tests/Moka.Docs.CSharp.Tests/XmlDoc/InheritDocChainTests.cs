using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.Metadata;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.CSharp.Tests.XmlDoc;

public sealed class InheritDocChainTests
{
	private readonly AssemblyAnalyzer _analyzer = new(NullLogger<AssemblyAnalyzer>.Instance);
	private readonly InheritDocResolver _resolver = new();

	private ApiReference AnalyzeAndResolve(string source)
	{
		ApiReference api = _analyzer.AnalyzeSyntaxTrees(
			[CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
			"TestAssembly");
		return _resolver.Resolve(api);
	}

	private static ApiMember Member(ApiReference api, string typeName, string memberName) =>
		api.Namespaces.SelectMany(ns => ns.Types).Single(t => t.Name == typeName).Members.Single(m => m.Name == memberName);

	[Fact]
	public void Resolve_GrandparentDocumentsMember_ChildInherits()
	{
		// Only the direct base type was searched, so a member documented two levels up stayed blank.
		ApiReference api = AnalyzeAndResolve("""
		                                     namespace TestNs
		                                     {
		                                         public abstract class Component
		                                         {
		                                             /// <summary>Renders the component.</summary>
		                                             public abstract void Render();
		                                         }

		                                         public abstract class Panel : Component
		                                         {
		                                             /// <inheritdoc/>
		                                             public override void Render() { }
		                                         }

		                                         public sealed class Dialog : Panel
		                                         {
		                                             /// <inheritdoc/>
		                                             public override void Render() { }
		                                         }
		                                     }
		                                     """);

		XmlDocBlock doc = Member(api, "Dialog", "Render").Documentation!;
		doc.Summary.Should().Be("Renders the component.");
		doc.IsInherited.Should().BeTrue();
	}

	[Fact]
	public void Resolve_InterfaceOfBaseType_ChildInherits()
	{
		// Interfaces implemented by a base type were never searched.
		ApiReference api = AnalyzeAndResolve("""
		                                     namespace TestNs
		                                     {
		                                         public interface IStore
		                                         {
		                                             /// <summary>Saves pending changes.</summary>
		                                             void Save();
		                                         }

		                                         public abstract class StoreBase : IStore
		                                         {
		                                             public abstract void Save();
		                                         }

		                                         public sealed class FileStore : StoreBase
		                                         {
		                                             /// <inheritdoc/>
		                                             public override void Save() { }
		                                         }
		                                     }
		                                     """);

		Member(api, "FileStore", "Save").Documentation!.Summary.Should().Be("Saves pending changes.");
	}

	[Fact]
	public void Resolve_InterfaceExtendsDocumentedInterface_ImplementationInherits()
	{
		// A member declared on an interface the implemented interface extends was out of reach.
		ApiReference api = AnalyzeAndResolve("""
		                                     namespace TestNs
		                                     {
		                                         public interface IReader
		                                         {
		                                             /// <summary>Reads one record.</summary>
		                                             string Read();
		                                         }

		                                         public interface IBufferedReader : IReader
		                                         {
		                                         }

		                                         public sealed class CsvReader : IBufferedReader
		                                         {
		                                             /// <inheritdoc/>
		                                             public string Read() => "";
		                                         }
		                                     }
		                                     """);

		Member(api, "CsvReader", "Read").Documentation!.Summary.Should().Be("Reads one record.");
	}

	[Fact]
	public void Resolve_GenericInterface_ImplementationInherits()
	{
		// The implemented interface is recorded as "TestNs.IRepository<TestNs.User>" and was looked up by that exact name.
		ApiReference api = AnalyzeAndResolve("""
		                                     namespace TestNs
		                                     {
		                                         public sealed class User { }

		                                         public interface IRepository<T>
		                                         {
		                                             /// <summary>Adds an entity.</summary>
		                                             void Add(T entity);
		                                         }

		                                         public sealed class UserRepository : IRepository<User>
		                                         {
		                                             /// <inheritdoc/>
		                                             public void Add(User entity) { }
		                                         }
		                                     }
		                                     """);

		Member(api, "UserRepository", "Add").Documentation!.Summary.Should().Be("Adds an entity.");
	}

	[Fact]
	public void Resolve_CrefToUnrelatedMember_TakesThatMembersDocs()
	{
		// The cref was ignored, so the docs came from a same-named base member or from nowhere.
		ApiReference api = AnalyzeAndResolve("""
		                                     using System.Threading.Tasks;

		                                     namespace TestNs
		                                     {
		                                         public class Parser
		                                         {
		                                             /// <summary>Parses the text.</summary>
		                                             /// <param name="text">The input.</param>
		                                             /// <returns>The parsed value.</returns>
		                                             public int Parse(string text) => 0;

		                                             /// <summary>Parses the whole file.</summary>
		                                             public int Parse(string text, bool strict) => 0;

		                                             /// <inheritdoc cref="Parse(string)"/>
		                                             public Task<int> ParseAsync(string text) => Task.FromResult(0);
		                                         }
		                                     }
		                                     """);

		XmlDocBlock doc = Member(api, "Parser", "ParseAsync").Documentation!;
		doc.Summary.Should().Be("Parses the text.");
		doc.Parameters.Should().ContainKey("text").WhoseValue.Should().Be("The input.");
		doc.Returns.Should().Be("The parsed value.");
	}

	[Fact]
	public void Resolve_CrefToType_TakesTheTypeDocs()
	{
		// A type that named its source with a cref got no summary, having no base type to fall back on.
		ApiReference api = AnalyzeAndResolve("""
		                                     namespace TestNs
		                                     {
		                                         /// <summary>Stores settings on disk.</summary>
		                                         public class SettingsStore { }

		                                         /// <inheritdoc cref="SettingsStore"/>
		                                         public sealed class JsonSettingsStore { }
		                                     }
		                                     """);

		api.Namespaces[0].Types.Single(t => t.Name == "JsonSettingsStore").Documentation!.Summary
			.Should().Be("Stores settings on disk.");
	}

	[Fact]
	public void Analyze_InheritDocCref_IsRecordedAsDocumentationId()
	{
		// Reading the comment dropped the cref, so nothing after analysis could follow it.
		ApiReference api = _analyzer.AnalyzeSyntaxTrees([CSharpSyntaxTree.ParseText("""
		                                                    namespace TestNs
		                                                    {
		                                                        public class Parser
		                                                        {
		                                                            /// <summary>Parses.</summary>
		                                                            public int Parse(string text) => 0;

		                                                            /// <inheritdoc cref="Parse(string)"/>
		                                                            public int TryParse(string text) => 0;
		                                                        }
		                                                    }
		                                                    """, cancellationToken: TestContext.Current.CancellationToken)],
			"TestAssembly");

		Member(api, "Parser", "TryParse").Documentation!.InheritDocCref.Should().Be("M:TestNs.Parser.Parse(System.String)");
	}

	[Fact]
	public void Resolve_OwnTagsWinOverInheritedOnes()
	{
		// The inherited doc block replaced the member's own, so the member's <param> text was lost.
		ApiReference api = AnalyzeAndResolve("""
		                                     namespace TestNs
		                                     {
		                                         public interface ISender
		                                         {
		                                             /// <summary>Sends a message.</summary>
		                                             /// <param name="message">The message.</param>
		                                             /// <param name="retries">How often to retry.</param>
		                                             void Send(string message, int retries);
		                                         }

		                                         public sealed class SmtpSender : ISender
		                                         {
		                                             /// <inheritdoc/>
		                                             /// <param name="retries">Ignored: SMTP retries on its own.</param>
		                                             public void Send(string message, int retries) { }
		                                         }
		                                     }
		                                     """);

		XmlDocBlock doc = Member(api, "SmtpSender", "Send").Documentation!;
		doc.Summary.Should().Be("Sends a message.");
		doc.Parameters["message"].Should().Be("The message.");
		doc.Parameters["retries"].Should().Be("Ignored: SMTP retries on its own.");
	}

	[Fact]
	public void Resolve_BaseClassIsSearchedBeforeInterfaces()
	{
		ApiReference api = AnalyzeAndResolve("""
		                                     namespace TestNs
		                                     {
		                                         public interface IClosable
		                                         {
		                                             /// <summary>From the interface.</summary>
		                                             void Close();
		                                         }

		                                         public class Connection
		                                         {
		                                             /// <summary>From the base class.</summary>
		                                             public virtual void Close() { }
		                                         }

		                                         public sealed class SqlConnection : Connection, IClosable
		                                         {
		                                             /// <inheritdoc/>
		                                             public override void Close() { }
		                                         }
		                                     }
		                                     """);

		Member(api, "SqlConnection", "Close").Documentation!.Summary.Should().Be("From the base class.");
	}

	[Fact]
	public void Resolve_MemberWithoutDocComment_StillInheritsFromInterface()
	{
		ApiReference api = AnalyzeAndResolve("""
		                                     namespace TestNs
		                                     {
		                                         public interface IClock
		                                         {
		                                             /// <summary>The current time.</summary>
		                                             long Now { get; }
		                                         }

		                                         public sealed class SystemClock : IClock
		                                         {
		                                             public long Now => 0;
		                                         }
		                                     }
		                                     """);

		Member(api, "SystemClock", "Now").Documentation!.Summary.Should().Be("The current time.");
	}
}
