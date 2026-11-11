using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Plugins;
using Moka.Docs.Plugins.OpenApi;

namespace Moka.Docs.AspNetCore.OpenApi;

/// <summary>
///     Reads the application's own OpenAPI document and hands it to the OpenAPI plugin.
/// </summary>
/// <remarks>
///     Microsoft.AspNetCore.OpenApi (<c>AddOpenApi</c>) and Swashbuckle (<c>AddSwaggerGen</c>) each declare
///     an internal <c>Microsoft.Extensions.ApiDescriptions.IDocumentProvider</c> and register it, so that
///     <c>dotnet-getdocument</c> can write the document at build time. This class uses the same contract
///     the same way: it finds the interface by its full name and calls <c>GetDocumentNames</c> and
///     <c>GenerateAsync(string, TextWriter)</c> by reflection. That keeps this package free of a
///     dependency on either library.
/// </remarks>
internal static class AppOpenApiDocument
{
	/// <summary>The full name of the interface both libraries register their document provider under.</summary>
	internal const string ProviderTypeName = "Microsoft.Extensions.ApiDescriptions.IDocumentProvider";

	/// <summary>
	///     The <c>openapi</c> plugin option, read by this host only, that names one of the application's
	///     documents. <see cref="MokaDocsOptions.OpenApiDocument" /> declares an entry with it.
	/// </summary>
	internal const string DocumentNameOption = "appDocument";

	/// <summary>The OpenAPI plugin's id, and the source of the warnings this class reports.</summary>
	internal const string PluginId = "openapi";

	/// <summary>
	///     Sets the <c>document</c> option of each loaded <c>openapi</c> entry that names one of the
	///     application's documents, reading the document again for every build.
	/// </summary>
	/// <remarks>
	///     A document that can't be read is a warning in <paramref name="diagnostics" />, and the entry gets
	///     an empty <c>document</c>, so the plugin generates nothing for it rather than falling back to
	///     looking for an <c>openapi.json</c> file.
	/// </remarks>
	internal static async Task SupplyAsync(PluginHost pluginHost, DiagnosticBag diagnostics, ILogger logger,
		CancellationToken ct)
	{
		foreach (LoadedPlugin loaded in pluginHost.LoadedPlugins)
		{
			if (!string.Equals(loaded.Plugin.Id, PluginId, StringComparison.OrdinalIgnoreCase)
			    || loaded.Declaration.Options.GetValueOrDefault(DocumentNameOption) is not string documentName)
			{
				continue;
			}

			// The plugin context resolves from the application's container, which is where AddOpenApi
			// registered the provider.
			ReadResult result = loaded.Context.GetService<IServiceProvider>() is { } services
				? await ReadAsync(services, documentName, ct)
				: new ReadResult(null, "OpenAPI: The application's services are not available, so its OpenAPI document can't be read.");

			if (result.Warning is not null)
			{
				diagnostics.Warning(result.Warning, PluginId);
				logger.LogWarning("{Warning}", result.Warning);
			}

			// The plugin reads its options when it runs, through a read-only view of this dictionary.
			loaded.Declaration.Options[OpenApiPlugin.DocumentOption] = result.Document ?? "";
		}
	}

	/// <summary>
	///     Generates the named document with the first registered provider that has it.
	/// </summary>
	/// <param name="services">The application's services.</param>
	/// <param name="documentName">The document name, such as <c>v1</c>. Matched without regard to case.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>The document's JSON, or a warning that says why there is none.</returns>
	internal static async Task<ReadResult> ReadAsync(IServiceProvider services, string documentName, CancellationToken ct)
	{
		var available = new List<string>();
		bool foundProvider = false;
		await using AsyncServiceScope scope = services.CreateAsyncScope();

		foreach (Type providerType in FindProviderTypes())
		{
			MethodInfo? getNames = providerType.GetMethod("GetDocumentNames", Type.EmptyTypes);
			MethodInfo? generate = providerType.GetMethod("GenerateAsync", [typeof(string), typeof(TextWriter)]);
			if (getNames is null || generate is null)
			{
				continue;
			}

			try
			{
				if (services.GetService(providerType) is not { } registered)
				{
					continue;
				}

				foundProvider = true;
				object provider = InScope(registered, scope.ServiceProvider);
				var names = ((IEnumerable<string>?)getNames.Invoke(provider, null) ?? []).ToList();
				if (names.FirstOrDefault(n => string.Equals(n, documentName, StringComparison.OrdinalIgnoreCase)) is not { } name)
				{
					available.AddRange(names);
					continue;
				}

				await using var writer = new StringWriter(CultureInfo.InvariantCulture);
				if (generate.Invoke(provider, [name, writer]) is Task generation)
				{
					await generation.WaitAsync(ct);
				}

				return new ReadResult(writer.ToString(), null);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Exception cause = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
				return new ReadResult(null,
					$"OpenAPI: Generating the application's OpenAPI document '{documentName}' failed: {cause.Message}");
			}
		}

		if (!foundProvider)
		{
			return new ReadResult(null,
				$"OpenAPI: The application has no OpenAPI document provider, so its document '{documentName}' " +
				"can't be read. Call builder.Services.AddOpenApi() (Microsoft.AspNetCore.OpenApi) or " +
				"builder.Services.AddSwaggerGen() (Swashbuckle.AspNetCore).");
		}

		string list = available.Count > 0 ? string.Join(", ", available.Distinct().Select(n => $"'{n}'")) : "none";
		return new ReadResult(null,
			$"OpenAPI: The application has no OpenAPI document named '{documentName}'. Available: {list}.");
	}

	/// <summary>
	///     Every loaded interface with the provider contract's full name. Each library declares its own.
	/// </summary>
	private static IEnumerable<Type> FindProviderTypes()
	{
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			Type? type;
			try
			{
				type = assembly.GetType(ProviderTypeName, false);
			}
			catch (Exception ex) when (ex is FileLoadException or FileNotFoundException or BadImageFormatException
				                           or TypeLoadException or NotSupportedException)
			{
				// An assembly whose types can't be loaded doesn't hold a provider anyone registered.
				continue;
			}

			if (type is { IsInterface: true })
			{
				yield return type;
			}
		}
	}

	/// <summary>
	///     A new instance of the registered provider's type, built from a scope, or the registered one when
	///     its type can't be built that way.
	/// </summary>
	/// <remarks>
	///     The provider is a singleton that keeps the root service provider. Microsoft.AspNetCore.OpenApi 9
	///     resolves <c>IOptionsSnapshot</c>, a scoped service, from it, which a host in the Development
	///     environment refuses ("Cannot resolve scoped service ... from root provider"). Built from a
	///     scope, the provider generates the document the way a request to <c>MapOpenApi</c> does.
	/// </remarks>
	private static object InScope(object registered, IServiceProvider scoped)
	{
		try
		{
			return ActivatorUtilities.CreateInstance(scoped, registered.GetType());
		}
		catch (InvalidOperationException)
		{
			return registered;
		}
	}

	/// <summary>The document's JSON, or why there is none.</summary>
	/// <param name="Document">The document, or <c>null</c>.</param>
	/// <param name="Warning">Why there is no document, or <c>null</c>.</param>
	internal readonly record struct ReadResult(string? Document, string? Warning);
}
