using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.CSharp.Metadata;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.CSharp;

/// <summary>
///     Extension methods for registering C# analysis services.
/// </summary>
public static class CSharpServiceExtensions
{
	/// <summary>
	///     Adds MokaDocs C# analysis services to the service collection.
	/// </summary>
	public static IServiceCollection AddMokaDocsCSharp(this IServiceCollection services)
	{
		services.AddSingleton<XmlDocParser>();
		services.AddSingleton<InheritDocResolver>();
		services.AddSingleton<AssemblyAnalyzer>();
		return services;
	}
}
