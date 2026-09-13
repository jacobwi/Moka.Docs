using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.Metadata;

namespace Moka.Docs.CSharp.Tests.Metadata;

public sealed class ViewSourceTests
{
	private const string _source = """
	                               namespace TestNs
	                               {
	                                   public class Account
	                                   {
	                                       private decimal _balance;
	                                       private static int _instances, _closed;

	                                       /// <summary>Deposits money.</summary>
	                                       public void Deposit(decimal amount)
	                                       {
	                                           _balance += Round(amount);
	                                       }

	                                       protected virtual void OnChanged() { }

	                                       internal void Audit() { }

	                                       private protected void Reconcile() { }

	                                       #region Helpers
	                                       private decimal Round(decimal value) => decimal.Round(value, 2);
	                                       #endregion

	                                       private sealed class Ledger { public int Entries; }

	                                       public sealed class Statement { private int _lines; public int Pages { get; set; } }
	                                   }
	                               }
	                               """;

	private readonly AssemblyAnalyzer _analyzer = new(NullLogger<AssemblyAnalyzer>.Instance);

	private ApiReference Analyze(bool includeInternals)
	{
		SyntaxTree tree = CSharpSyntaxTree.ParseText(_source, cancellationToken: TestContext.Current.CancellationToken);
		return _analyzer.AnalyzeSyntaxTrees([tree], "TestAssembly", includeInternals);
	}

	[Fact]
	public void ViewSource_PrivateMembers_AreLeftOut()
	{
		// The panel printed the whole declaration: private fields, private helpers and private nested types.
		ApiType account = Analyze(false).Namespaces[0].Types.Single(t => t.Name == "Account");

		account.SourceCode.Should().NotContain("_balance;")
			.And.NotContain("_instances")
			.And.NotContain("Round(decimal value)")
			.And.NotContain("class Ledger")
			.And.NotContain("_lines");
	}

	[Fact]
	public void ViewSource_DocumentedMembers_KeepTheirBodies()
	{
		ApiType account = Analyze(false).Namespaces[0].Types.Single(t => t.Name == "Account");

		account.SourceCode.Should().Contain("/// <summary>Deposits money.</summary>")
			.And.Contain("_balance += Round(amount);")
			.And.Contain("protected virtual void OnChanged()")
			.And.Contain("public sealed class Statement")
			.And.Contain("public int Pages");
	}

	[Fact]
	public void ViewSource_RemovedMemberWithRegion_KeepsRegionDirectivesBalanced()
	{
		// "#region Helpers" sits on the private helper; removing it with its trivia left a lone #endregion.
		string source = Analyze(false).Namespaces[0].Types.Single(t => t.Name == "Account").SourceCode!;

		source.Should().Contain("#region Helpers").And.Contain("#endregion");
	}

	[Fact]
	public void ViewSource_InternalMembers_FollowIncludeInternals()
	{
		// The page dropped internal members without includeInternals, but the source panel still printed them.
		string publicOnly = Analyze(false).Namespaces[0].Types.Single(t => t.Name == "Account").SourceCode!;
		publicOnly.Should().NotContain("void Audit()").And.NotContain("void Reconcile()");

		string withInternals = Analyze(true).Namespaces[0].Types.Single(t => t.Name == "Account").SourceCode!;
		withInternals.Should().Contain("internal void Audit()").And.Contain("private protected void Reconcile()");
		withInternals.Should().NotContain("_balance;");
	}
}
