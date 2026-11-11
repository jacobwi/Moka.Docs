namespace Moka.Docs.Serve;

/// <summary>
///     Thrown by <see cref="ReplExecutionService.ExecuteAsync" /> when as many runs as it queues are
///     already waiting. The REPL endpoints answer it with <c>429 Too Many Requests</c>.
/// </summary>
public sealed class ReplQueueFullException : Exception
{
	/// <summary>Creates the exception with the message the endpoints return.</summary>
	public ReplQueueFullException()
		: base("Too many snippets are waiting to run. Try again in a few seconds.")
	{
	}
}
