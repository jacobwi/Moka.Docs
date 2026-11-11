namespace Microsoft.Extensions.ApiDescriptions;

/// <summary>
///     The contract MokaDocs reads documents through, declared here the way Microsoft.AspNetCore.OpenApi and
///     Swashbuckle each declare their own copy. MokaDocs matches it by its full name.
/// </summary>
internal interface IDocumentProvider
{
	IEnumerable<string> GetDocumentNames();

	Task GenerateAsync(string documentName, TextWriter writer);
}

/// <summary>A provider whose generation fails, as one with a broken document transformer does.</summary>
internal sealed class ThrowingDocumentProvider : IDocumentProvider
{
	public IEnumerable<string> GetDocumentNames() => ["v1"];

	public Task GenerateAsync(string documentName, TextWriter writer) =>
		Task.FromException(new InvalidOperationException("A document transformer failed."));
}
