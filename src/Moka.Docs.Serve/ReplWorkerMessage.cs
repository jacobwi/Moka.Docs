using System.Text.Json;

namespace Moka.Docs.Serve;

/// <summary>
///     One line of the protocol between <see cref="ReplExecutionService" /> and
///     <see cref="ReplWorker" />, serialized as a JSON object per line.
/// </summary>
internal sealed class ReplWorkerMessage
{
	/// <summary>Parent to worker, first line: the assemblies to reference.</summary>
	internal const string ConfigureType = "configure";

	/// <summary>Parent to worker: run <see cref="Code" />.</summary>
	internal const string ExecuteType = "execute";

	/// <summary>Worker to parent: configured and warmed up.</summary>
	internal const string ReadyType = "ready";

	/// <summary>Worker to parent: the result of the last run.</summary>
	internal const string ResultType = "result";

	private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

	/// <summary>One of the <c>*Type</c> constants.</summary>
	public string Type { get; set; } = "";

	/// <summary>The snippet, for <see cref="ExecuteType" />.</summary>
	public string? Code { get; set; }

	/// <summary>Assembly file paths, for <see cref="ConfigureType" />.</summary>
	public List<string>? References { get; set; }

	/// <summary>Console output or the snippet's value, for <see cref="ResultType" />.</summary>
	public string? Output { get; set; }

	/// <summary>Compiler or runtime error, for <see cref="ResultType" />.</summary>
	public string? Error { get; set; }

	/// <summary>Serializes the message as one line of JSON.</summary>
	public string Serialize() => JsonSerializer.Serialize(this, _json);

	/// <summary>Parses a line, or returns <c>null</c> for a blank line or one that isn't a message.</summary>
	public static ReplWorkerMessage? Parse(string? line)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return null;
		}

		try
		{
			return JsonSerializer.Deserialize<ReplWorkerMessage>(line, _json);
		}
		catch (JsonException)
		{
			return null;
		}
	}
}
