using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.Parsing.FrontMatter;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing;

/// <summary>
///     Extension methods for registering parsing services.
/// </summary>
public static class ParsingServiceExtensions
{
	/// <summary>
	///     Adds MokaDocs parsing services to the service collection.
	/// </summary>
	public static IServiceCollection AddMokaDocsParsing(this IServiceCollection services)
	{
		services.AddSingleton<MarkdownParser>();
		services.AddSingleton<MarkdownParserOptions>();
		services.AddSingleton<FrontMatterExtractor>();
		services.AddSingleton<TocGenerator>();
		return services;
	}
}
