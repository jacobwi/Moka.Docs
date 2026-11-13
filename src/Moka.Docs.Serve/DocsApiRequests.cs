using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Moka.Docs.Serve;

/// <summary>
///     Reads and answers the POST requests the docs pages send: the feedback widget's vote and a
///     REPL snippet. Origin checks stay with each host, since the dev server and the ASP.NET Core
///     host apply different rules; the body caps, validation and replies are shared.
/// </summary>
internal static class DocsApiRequests
{
	/// <summary>Largest feedback body read. A vote is about a hundred bytes.</summary>
	internal const int MaxFeedbackBodyBytes = 4 * 1024;

	/// <summary>Longest page path accepted in a vote.</summary>
	internal const int MaxFeedbackPageLength = 1024;

	/// <summary>
	///     Largest REPL body read. Snippets are capped at 10,000 characters, and JSON can escape a
	///     character as six bytes.
	/// </summary>
	internal const int MaxReplBodyBytes = 64 * 1024;

	private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

	/// <summary>
	///     Reads a vote. A body over the cap gets 413 and one that isn't a valid vote gets 400, and
	///     both return <c>null</c>. The page ends up in the log, so control characters are refused.
	/// </summary>
	internal static async Task<FeedbackVote?> ReadFeedbackAsync(HttpContext context)
	{
		byte[]? body = await ReadBodyAsync(context.Request, MaxFeedbackBodyBytes, context.RequestAborted);
		if (body is null)
		{
			await WriteJsonAsync(context, StatusCodes.Status413PayloadTooLarge, new { ok = false });
			return null;
		}

		if (Deserialize<FeedbackRequest>(body) is not { Page: { Length: > 0 and <= MaxFeedbackPageLength } page, Helpful: { } helpful }
		    || page.Any(char.IsControl))
		{
			await WriteJsonAsync(context, StatusCodes.Status400BadRequest, new { ok = false });
			return null;
		}

		return new FeedbackVote(page, helpful);
	}

	/// <summary>
	///     Reads a snippet. A body over the cap gets 413 and one that isn't JSON gets 400, and both
	///     return <c>null</c>.
	/// </summary>
	internal static async Task<string?> ReadReplCodeAsync(HttpContext context)
	{
		byte[]? body = await ReadBodyAsync(context.Request, MaxReplBodyBytes, context.RequestAborted);
		if (body is null)
		{
			await WriteJsonAsync(context, StatusCodes.Status413PayloadTooLarge,
				new { error = "The code is too long to run." });
			return null;
		}

		if (Deserialize<ReplRequest>(body) is not { } request)
		{
			await WriteJsonAsync(context, StatusCodes.Status400BadRequest,
				new { error = "The request body is not valid JSON." });
			return null;
		}

		return request.Code ?? "";
	}

	/// <summary>
	///     Runs a snippet and answers 200 with its output and error, or 429 when the queue is full.
	///     Cancelling <paramref name="ct" /> cancels the run, and the service replaces the worker.
	/// </summary>
	internal static async Task RunReplAsync(HttpContext context, ReplExecutionService service, string code,
		CancellationToken ct)
	{
		ReplResult result;
		try
		{
			result = await service.ExecuteAsync(code, ct);
		}
		catch (ReplQueueFullException ex)
		{
			await WriteJsonAsync(context, StatusCodes.Status429TooManyRequests, new { error = ex.Message });
			return;
		}

		await WriteJsonAsync(context, StatusCodes.Status200OK,
			new { output = result.Output ?? "", error = result.Error ?? "" });
	}

	/// <summary>
	///     Reads the body, or returns <c>null</c> when it is longer than <paramref name="maxBytes" />.
	///     Bodies used to be read whole: 400 MB posted to /api/feedback took serve to 3.2 GB.
	/// </summary>
	internal static async Task<byte[]?> ReadBodyAsync(HttpRequest request, int maxBytes, CancellationToken ct)
	{
		if (request.ContentLength > maxBytes)
		{
			return null;
		}

		using var buffer = new MemoryStream();
		byte[] chunk = new byte[4096];
		int read;
		while ((read = await request.Body.ReadAsync(chunk, ct)) > 0)
		{
			if (buffer.Length + read > maxBytes)
			{
				return null;
			}

			buffer.Write(chunk, 0, read);
		}

		return buffer.ToArray();
	}

	/// <summary>Writes <paramref name="payload" /> as a JSON response with its length set.</summary>
	internal static async Task WriteJsonAsync<T>(HttpContext context, int statusCode, T payload)
	{
		byte[] body = JsonSerializer.SerializeToUtf8Bytes(payload, _json);
		HttpResponse response = context.Response;
		response.StatusCode = statusCode;
		response.ContentType = "application/json; charset=utf-8";
		response.ContentLength = body.Length;
		await response.Body.WriteAsync(body, context.RequestAborted);
	}

	private static T? Deserialize<T>(byte[] body) where T : class
	{
		try
		{
			return JsonSerializer.Deserialize<T>(body, _json);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	/// <summary>The REPL request body.</summary>
	private sealed class ReplRequest
	{
		public string? Code { get; set; }
	}

	/// <summary>The feedback widget's request body.</summary>
	private sealed class FeedbackRequest
	{
		public string? Page { get; set; }
		public bool? Helpful { get; set; }
	}
}

/// <summary>A "Was this page helpful?" vote that passed validation.</summary>
/// <param name="Page">The page path the vote is for.</param>
/// <param name="Helpful">Whether the reader found the page helpful.</param>
internal readonly record struct FeedbackVote(string Page, bool Helpful);
