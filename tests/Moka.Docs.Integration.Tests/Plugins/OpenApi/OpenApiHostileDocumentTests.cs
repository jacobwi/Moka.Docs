using System.Diagnostics;
using System.Text;
using FluentAssertions;
using Moka.Docs.Plugins.OpenApi;

namespace Moka.Docs.Integration.Tests.Plugins.OpenApi;

/// <summary>
///     Documents built to exhaust the parser: deep nesting, references that fan out, and YAML aliases and
///     merge keys that expand. Each must finish quickly, with bounded memory, and say what it cut short.
/// </summary>
/// <remarks>
///     Several of these overflowed the stack, which ends the process, or ran for minutes and allocated
///     gigabytes before the limits existed.
/// </remarks>
public sealed class OpenApiHostileDocumentTests
{
	private static readonly TimeSpan _quickly = TimeSpan.FromSeconds(30);

	private const long _boundedAllocation = 600L * 1024 * 1024;

	/// <summary>Parses on a thread of its own, with the time and the bytes that thread allocated.</summary>
	private static async Task<(OpenApiSpec? Spec, Exception? Error, TimeSpan Elapsed, long Allocated)> ParseAsync(string document)
	{
		var result = await Task.Run(() =>
		{
			long before = GC.GetAllocatedBytesForCurrentThread();
			var sw = Stopwatch.StartNew();
			try
			{
				OpenApiSpec spec = OpenApiParser.Parse(document);
				return (Spec: (OpenApiSpec?)spec, Error: (Exception?)null, sw.Elapsed, GC.GetAllocatedBytesForCurrentThread() - before);
			}
			catch (Exception ex)
			{
				return (Spec: null, Error: ex, sw.Elapsed, GC.GetAllocatedBytesForCurrentThread() - before);
			}
		}).WaitAsync(_quickly, TestContext.Current.CancellationToken);
		return result;
	}

	private static string Json(string schemas, string paths = "{}") =>
		"{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":" + paths + ",\"components\":{\"schemas\":{" + schemas + "}}}";

	[Fact]
	public async Task YamlNestedThousandsDeep_IsAParseError_NotAStackOverflow()
	{
		string yaml = "openapi: 3.0.0\ninfo: {title: t, version: '1'}\nx-deep: " + new string('[', 10_000) + new string(']', 10_000) + "\npaths: {}\n";

		var (spec, error, _, _) = await ParseAsync(yaml);

		spec.Should().BeNull();
		error.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Contain("nests deeper than 64 levels");
	}

	[Fact]
	public async Task JsonNestedPastTheLimit_IsAParseError()
	{
		string json = "{\"openapi\":\"3.0.0\",\"x-deep\":" + new string('[', 100) + new string(']', 100) + "}";

		var (_, error, _, _) = await ParseAsync(json);

		error.Should().BeOfType<InvalidOperationException>().Which.Message.Should().StartWith("Failed to parse OpenAPI spec");
	}

	[Fact]
	public async Task ReferenceChainThousandsLong_ParsesQuickly()
	{
		// Each schema's property refers to the next: S0 -> S1 -> ... -> S5000.
		var schemas = new StringBuilder();
		for (int i = 0; i < 5000; i++)
		{
			schemas.Append($"\"S{i}\":{{\"type\":\"object\",\"properties\":{{\"next\":{{\"$ref\":\"#/components/schemas/S{i + 1}\"}}}}}},");
		}

		schemas.Append("\"S5000\":{\"type\":\"string\"}");
		const string paths = """{"/a":{"get":{"responses":{"200":{"description":"ok","content":{"application/json":{"schema":{"$ref":"#/components/schemas/S0"}}}}}}}}""";

		var (spec, error, _, allocated) = await ParseAsync(Json(schemas.ToString(), paths));

		error.Should().BeNull();
		OpenApiSchema s0 = spec!.Endpoints.Single().Responses.Single().Schema!;
		s0.RefName.Should().Be("S0");
		s0.Properties["next"].RefName.Should().Be("S1");
		s0.Properties["next"].Properties.Should().BeEmpty("a reference inside an expanded one shows only its name");
		allocated.Should().BeLessThan(_boundedAllocation);
	}

	[Fact]
	public async Task AllOfChainThousandsLong_StopsAtTheDepthLimit_WithAWarning()
	{
		var schemas = new StringBuilder();
		for (int i = 0; i < 3000; i++)
		{
			schemas.Append($"\"S{i}\":{{\"allOf\":[{{\"$ref\":\"#/components/schemas/S{i + 1}\"}},{{\"properties\":{{\"p{i}\":{{\"type\":\"string\"}}}}}}]}},");
		}

		schemas.Append("\"S3000\":{\"type\":\"object\",\"properties\":{\"last\":{\"type\":\"integer\"}}}");

		var (spec, error, _, _) = await ParseAsync(Json(schemas.ToString()));

		error.Should().BeNull();
		spec!.Schemas["S2990"].Properties.Keys.Should().Contain(["p2990", "p2999", "last"], "a short chain still merges every part");
		spec.Schemas["S0"].Properties.Should().ContainKey("p0").And.NotContainKey("last");
		spec.Warnings.Should().ContainSingle(w => w.Contains("nests more than 64 schemas deep"));
	}

	[Theory]
	[InlineData(24)]
	[InlineData(40)]
	public async Task ReferencesThatFanOutAtEveryLevel_ParseQuickly(int levels)
	{
		// Each schema refers to the next one twice: expanding them all is 2^levels schemas.
		var schemas = new StringBuilder();
		for (int i = 0; i < levels; i++)
		{
			schemas.Append($"\"S{i}\":{{\"type\":\"object\",\"properties\":{{\"l\":{{\"$ref\":\"#/components/schemas/S{i + 1}\"}},\"r\":{{\"$ref\":\"#/components/schemas/S{i + 1}\"}}}}}},");
		}

		schemas.Append($"\"S{levels}\":{{\"type\":\"string\"}}");

		var (spec, error, elapsed, allocated) = await ParseAsync(Json(schemas.ToString()));

		error.Should().BeNull();
		spec!.Schemas["S0"].Properties["l"].Properties["r"].RefName.Should().Be("S2");
		spec.Warnings.Should().BeEmpty();
		elapsed.Should().BeLessThan(_quickly);
		allocated.Should().BeLessThan(_boundedAllocation);
	}

	[Fact]
	public async Task AllOfPartsThatFanOutAtEveryLevel_StopAtTheBudget_WithAWarning()
	{
		// allOf parts merge at any depth, so a fan-out through them is what the budget is for. Twenty
		// levels are a million schemas: enough to pass the budget, few enough to finish without it.
		var schemas = new StringBuilder();
		for (int i = 0; i < 20; i++)
		{
			schemas.Append($"\"S{i}\":{{\"allOf\":[{{\"$ref\":\"#/components/schemas/S{i + 1}\"}},{{\"$ref\":\"#/components/schemas/S{i + 1}\"}},{{\"properties\":{{\"p{i}\":{{\"type\":\"string\"}}}}}}]}},");
		}

		schemas.Append("\"S20\":{\"type\":\"object\"}");

		var (spec, error, elapsed, allocated) = await ParseAsync(Json(schemas.ToString()));

		error.Should().BeNull();
		spec!.Warnings.Should().ContainSingle(w => w.Contains("reference each other too often"));
		elapsed.Should().BeLessThan(_quickly);
		allocated.Should().BeLessThan(_boundedAllocation);
	}

	[Fact]
	public async Task SchemasThatEachReferenceAllOthers_ParseQuickly()
	{
		// Nine of these took 276 seconds and allocated 140 GB.
		const int count = 12;
		var schemas = new List<string>();
		for (int i = 0; i < count; i++)
		{
			string properties = string.Join(",", Enumerable.Range(0, count).Where(j => j != i)
				.Select(j => $"\"to{j}\":{{\"$ref\":\"#/components/schemas/S{j}\"}}"));
			schemas.Add($"\"S{i}\":{{\"type\":\"object\",\"properties\":{{\"id\":{{\"type\":\"string\"}},{properties}}}}}");
		}

		var (spec, error, elapsed, allocated) = await ParseAsync(Json(string.Join(",", schemas)));

		error.Should().BeNull();
		OpenApiSchema nested = spec!.Schemas["S0"].Properties["to1"].Properties["to2"];
		nested.RefName.Should().Be("S2");
		nested.Properties.Should().BeEmpty();
		spec.Warnings.Should().BeEmpty();
		elapsed.Should().BeLessThan(_quickly);
		allocated.Should().BeLessThan(_boundedAllocation);
	}

	[Fact]
	public async Task YamlAliasesThatExpandToMillions_AreAParseError()
	{
		// 400 bytes that expand to ten million values. Each level more multiplies that by ten.
		var yaml = new StringBuilder("openapi: 3.0.0\ninfo: {title: t, version: '1'}\nx-a: &a [x,x,x,x,x,x,x,x,x,x]\n");
		string previous = "a";
		foreach (string name in new[] { "b", "c", "d", "e", "f", "g" })
		{
			yaml.Append($"x-{name}: &{name} [").Append(string.Join(",", Enumerable.Repeat("*" + previous, 10))).Append("]\n");
			previous = name;
		}

		yaml.Append("paths: {}\n");

		var (_, error, elapsed, allocated) = await ParseAsync(yaml.ToString());

		error.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Contain("aliases expand it");
		elapsed.Should().BeLessThan(_quickly);
		allocated.Should().BeLessThan(_boundedAllocation);
	}

	[Fact]
	public async Task YamlMergeKeysThatDoubleAtEveryLevel_AreAParseError()
	{
		var yaml = new StringBuilder("openapi: 3.0.0\ninfo: {title: t, version: '1'}\nx-0: &m0 {k: v}\n");
		for (int i = 1; i <= 22; i++)
		{
			yaml.Append($"x-{i}: &m{i} {{l: {{<<: *m{i - 1}}}, r: {{<<: *m{i - 1}}}}}\n");
		}

		yaml.Append("paths: {}\n");

		var (_, error, elapsed, _) = await ParseAsync(yaml.ToString());

		error.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Contain("aliases expand it");
		elapsed.Should().BeLessThan(_quickly);
	}

	[Fact]
	public async Task YamlAnchorThatContainsItsOwnAlias_IsAParseError()
	{
		var (_, error, _, _) = await ParseAsync("openapi: 3.0.0\nx-loop: &a [ *a ]\npaths: {}\n");

		error.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Contain("contains its own alias");
	}

	[Fact]
	public async Task YamlKeyThatIsAnAliasOrAList_IsAParseError_BeforeYamlDotNetHashesIt()
	{
		// YamlDotNet hashes a key when it adds it to a mapping, and an alias key that expands to a
		// billion values kept it busy for a minute before any of this code ran.
		var yaml = new StringBuilder("openapi: 3.0.0\nx-a: &a [x,x,x,x,x,x,x,x,x,x]\n");
		string previous = "a";
		foreach (string name in new[] { "b", "c", "d", "e", "f", "g", "h", "i" })
		{
			yaml.Append($"x-{name}: &{name} [").Append(string.Join(",", Enumerable.Repeat("*" + previous, 10))).Append("]\n");
			previous = name;
		}

		yaml.Append("x-key:\n  ? *i\n  : value\npaths: {}\n");

		var (_, aliasKey, elapsed, _) = await ParseAsync(yaml.ToString());
		var (_, listKey, _, _) = await ParseAsync("openapi: 3.0.0\nx-b:\n  ? [x, y]\n  : value\npaths: {}\n");

		aliasKey.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Contain("must be text");
		elapsed.Should().BeLessThan(_quickly);
		listKey.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Contain("must be text");
	}

	[Fact]
	public async Task YamlDocumentOverAHundredThousandEvents_Parses()
	{
		// YamlDotNet's MergingParser refused any document over 100,000 events, about 1 MB of YAML.
		var yaml = new StringBuilder("openapi: 3.0.0\ninfo: {title: t, version: '1'}\npaths:\n");
		for (int i = 0; i < 30_000; i++)
		{
			yaml.Append($"  /p{i}:\n    get:\n      responses: {{}}\n");
		}

		var (spec, error, _, _) = await ParseAsync(yaml.ToString());

		error.Should().BeNull();
		spec!.Endpoints.Should().HaveCount(30_000);
	}

	[Fact]
	public void YamlMergeKey_KeysWrittenInTheMappingWin_ThenTheEarlierMergedMapping()
	{
		const string yaml = """
		                    openapi: 3.0.0
		                    info: { title: t, version: '1' }
		                    x-base: &base { type: integer, format: int32, description: Base }
		                    x-more: &more { description: More, enum: [1, 2] }
		                    paths: {}
		                    components:
		                      schemas:
		                        Count:
		                          <<: [*base, *more]
		                          format: int64
		                    """;

		OpenApiSpec spec = OpenApiParser.Parse(yaml);

		OpenApiSchema count = spec.Schemas["Count"];
		count.ToDisplayString().Should().Be("integer (int64)");
		count.Description.Should().Be("Base");
		count.EnumValues.Should().Equal("1", "2");
	}
}
