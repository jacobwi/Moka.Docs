using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Api;
using Moka.Docs.Engine.Caching;

namespace Moka.Docs.Engine.Tests.Caching;

public sealed class BuildCacheTests : IDisposable
{
	private readonly BuildCache _cache = new(NullLogger<BuildCache>.Instance);
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-cache-" + Guid.NewGuid().ToString("N"));

	public BuildCacheTests() => Directory.CreateDirectory(_root);

	private string ProjectPath => Path.Combine(_root, "src", "Lib", "Lib.csproj");

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (IOException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	private static ApiReference Model(string typeName) => new()
	{
		Assemblies = ["Lib"],
		Namespaces =
		[
			new ApiNamespace
			{
				Name = "Lib",
				Types = [new ApiType { Name = typeName, FullName = "Lib." + typeName, Kind = ApiTypeKind.Class }]
			}
		]
	};

	[Fact]
	public void TryGet_AfterSet_ReturnsTheStoredModel()
	{
		_cache.Set(_root, ProjectPath, "fp-1", Model("Widget"));

		ApiReference? hit = _cache.TryGet(_root, ProjectPath, "fp-1");

		hit.Should().NotBeNull();
		hit!.Namespaces.Single().Types.Single().FullName.Should().Be("Lib.Widget");
	}

	[Fact]
	public void TryGet_WithDifferentSourceFingerprint_Misses()
	{
		_cache.Set(_root, ProjectPath, "fp-1", Model("Widget"));

		_cache.TryGet(_root, ProjectPath, "fp-2").Should().BeNull();
	}

	[Fact]
	public void TryGet_EntryWrittenByADifferentMokaDocsBuild_Misses()
	{
		// The source fingerprint only tracks the user's files. Without the tool identity, an
		// upgraded MokaDocs kept serving models from the old analyzer, so analyzer fixes never
		// reached anyone with a warm cache.
		_cache.Set(_root, ProjectPath, "fp-1", Model("Widget"));
		string file = Directory.GetFiles(Path.Combine(_root, ".mokadocs", "cache")).Single();
		string json = File.ReadAllText(file);
		json.Should().Contain("\"ToolIdentity\"");

		string fromOlderBuild = System.Text.RegularExpressions.Regex.Replace(
			json, "\"ToolIdentity\":\"[^\"]*\"", "\"ToolIdentity\":\"an-older-mokadocs\"");
		File.WriteAllText(file, fromOlderBuild);

		_cache.TryGet(_root, ProjectPath, "fp-1").Should().BeNull();
	}

	[Fact]
	public void TryGet_EntryWithNoToolIdentity_Misses()
	{
		// Every entry written by 1.6.0, which predates the field.
		_cache.Set(_root, ProjectPath, "fp-1", Model("Widget"));
		string file = Directory.GetFiles(Path.Combine(_root, ".mokadocs", "cache")).Single();
		string withoutField = System.Text.RegularExpressions.Regex.Replace(
			File.ReadAllText(file), ",?\"ToolIdentity\":\"[^\"]*\"", "");
		File.WriteAllText(file, withoutField);

		_cache.TryGet(_root, ProjectPath, "fp-1").Should().BeNull();
	}

	[Fact]
	public void TryGet_CorruptEntry_MissesWithoutThrowing()
	{
		_cache.Set(_root, ProjectPath, "fp-1", Model("Widget"));
		string file = Directory.GetFiles(Path.Combine(_root, ".mokadocs", "cache")).Single();
		File.WriteAllText(file, "{ not json");

		Action read = () => _cache.TryGet(_root, ProjectPath, "fp-1");

		read.Should().NotThrow();
		_cache.TryGet(_root, ProjectPath, "fp-1").Should().BeNull();
	}

	[Fact]
	public void ComputeFingerprint_ChangesWhenASourceFileChanges()
	{
		string dir = Path.Combine(_root, "src", "Lib");
		Directory.CreateDirectory(dir);
		string file = Path.Combine(dir, "Widget.cs");
		File.WriteAllText(file, "public class Widget {}");
		string before = BuildCache.ComputeFingerprint(dir, false);

		File.WriteAllText(file, "public class Widget { public int Size; }");
		File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddMinutes(1));

		BuildCache.ComputeFingerprint(dir, false).Should().NotBe(before);
		BuildCache.ComputeFingerprint(dir, true).Should().NotBe(BuildCache.ComputeFingerprint(dir, false));
	}
}
