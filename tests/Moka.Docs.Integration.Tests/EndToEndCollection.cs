namespace Moka.Docs.Integration.Tests;

/// <summary>
///     Tests that run real dev servers, file watchers or dotnet builds and wait for them with a
///     timeout. In parallel with the rest of the suite they lost the CPU to each other and timed out
///     now and then, so they run alone.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EndToEndCollection
{
	public const string Name = "End to end";
}
