using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Serve;

/// <summary>
///     The theme lab: a button on every page the dev server serves that opens a panel where the
///     author tries the default theme's presets, styles, color presets, code themes, code styles,
///     layout variants and banner on the page, then saves them to the configuration file. Saving
///     triggers the usual rebuild and reload. The build never writes it into a site.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>GET /api/theme-lab/lab.js</c>: the panel's script, added to every page by <see cref="PageHtml" />.</item>
///         <item><c>GET /api/theme-lab/catalog</c>: the options, their choices and the values in the file.</item>
///         <item>
///             <c>POST /api/theme-lab/save</c>: <c>{ "version": "...", "values": { "style": "moka", "layout.navbar": null } }</c>.
///             Values are checked against the catalog, the file is edited as text so comments and
///             formatting stay, and the result must load with <see cref="SiteConfigReader" /> to the
///             intended values before it replaces the file.
///         </item>
///     </list>
/// </remarks>
public sealed class ThemeLabExtension : IDevServerExtension
{
	/// <summary>Where the lab's endpoints live, under <c>/api</c> so the server's same-origin check guards the save.</summary>
	internal const string ApiPath = "/api/theme-lab";

	/// <summary>Largest save body read. A save of every option with a long banner is about 3 KB.</summary>
	internal const int MaxSaveBodyBytes = 16 * 1024;

	private const string _noStore = "no-cache, no-store, must-revalidate";

	private static readonly Lazy<byte[]> _script = new(() => Encoding.UTF8.GetBytes(ThemeLabAssets.Script()));
	private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

	private readonly string _configPath;
	private readonly Func<string, string?> _environment;
	private readonly ILogger _logger;
	private readonly Action<string>? _report;
	private readonly Lock _saving = new();

	/// <summary>Creates the lab for the configuration file the site is built from.</summary>
	/// <param name="configPath">The configuration file (mokadocs.yaml, or the one <c>--config</c> named) that Save edits.</param>
	/// <param name="logger">Logs saves and refused saves.</param>
	/// <param name="report">Called with a one-line message after each save, for the console.</param>
	public ThemeLabExtension(string configPath, ILogger<ThemeLabExtension>? logger = null, Action<string>? report = null)
		: this(configPath, logger, report, Environment.GetEnvironmentVariable)
	{
	}

	internal ThemeLabExtension(string configPath, ILogger? logger, Action<string>? report, Func<string, string?> environment)
	{
		_configPath = Path.GetFullPath(configPath);
		_logger = logger ?? NullLogger.Instance;
		_report = report;
		_environment = environment;
	}

	/// <inheritdoc />
	public string PageHtml => $"<script src=\"{ApiPath}/lab.js\" defer></script>";

	private string FileName => Path.GetFileName(_configPath);

	/// <inheritdoc />
	public void MapEndpoints(IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet(ApiPath + "/lab.js", WriteScriptAsync);
		endpoints.MapGet(ApiPath + "/catalog", WriteCatalogAsync);
		endpoints.MapPost(ApiPath + "/save", SaveAsync);
	}

	/// <summary>The options with their choices and the values the file holds now.</summary>
	internal (int Status, object Body) Catalog()
	{
		ThemeLabConfigFile file;
		try
		{
			file = ThemeLabConfigFile.Read(_configPath);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
		{
			return (StatusCodes.Status500InternalServerError, new { error = $"Can't read {FileName}: {ex.Message}" });
		}

		IReadOnlyList<ThemeLabOption> options = ThemeLabCatalog.CreateOptions();
		IReadOnlyDictionary<string, string?> written = ConfigYamlEditor.ReadValues(file.Text, options.Select(o => o.YamlPath));
		SiteConfig? config = null;
		string? readOnly = ConfigYamlEditor.FindRefusal(file.Text, options.Select(o => o.YamlPath)) is { } refusal
			? $"Save is off because {FileName} can't be edited safely: {refusal}."
			: null;
		try
		{
			config = SiteConfigReader.Parse(file.Text, Path.GetDirectoryName(_configPath));
		}
		catch (SiteConfigException ex)
		{
			readOnly ??= $"Save is off because {FileName} doesn't load ({ex.Message}).";
		}

		string themeName = config?.Theme.Name ?? MokaDefaults.ThemeName;
		return (StatusCodes.Status200OK, new
		{
			configFile = FileName,
			version = file.Version,
			themeName,
			customTheme = !string.IsNullOrWhiteSpace(themeName) && !themeName.Equals("default", StringComparison.OrdinalIgnoreCase),
			customPrimaryColor = config is not null && !string.IsNullOrWhiteSpace(config.Theme.Options.PrimaryColor)
			                                        && !config.Theme.Options.PrimaryColor.Equals(MokaDefaults.PrimaryColor, StringComparison.OrdinalIgnoreCase),
			readOnly,
			options = options.Select(o => new
			{
				key = o.Key,
				label = o.Label,
				group = o.Group,
				kind = o.Kind.ToString().ToLowerInvariant(),
				@default = o.Default,
				value = written.GetValueOrDefault(o.YamlPath),
				attribute = o.Attribute,
				removeAttributeAtDefault = o.RemoveAttributeAtDefault,
				environment = o.EnvironmentVariable is { } name && ThemeLabCatalog.EnvironmentValue(_environment, name) is { } value
					? new { name, value }
					: null,
				readerStorageKeys = o.ReaderStorageKeys,
				liveWithLayoutSelector = o.LiveWithLayoutSelector,
				maxLength = o.MaxLength,
				choices = o.Choices
			})
		});
	}

	/// <summary>Checks and writes a save request. Callers have already read and capped the body.</summary>
	internal (int Status, object Body) Save(ThemeLabSaveRequest request)
	{
		if (request.Values is null)
		{
			return (StatusCodes.Status400BadRequest, new { error = "The request has no values object." });
		}

		lock (_saving)
		{
			ThemeLabConfigFile file;
			try
			{
				file = ThemeLabConfigFile.Read(_configPath);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
			{
				return (StatusCodes.Status500InternalServerError, new { error = $"Can't read {FileName}: {ex.Message}" });
			}

			if (request.Version is { Length: > 0 } version && version != file.Version)
			{
				return (StatusCodes.Status409Conflict, new
				{
					error = $"{FileName} changed since this page loaded it. Reload the page to see its values, then save again."
				});
			}

			IReadOnlyList<ThemeLabOption> options = ThemeLabCatalog.CreateOptions();
			Dictionary<string, string?> wanted = ThemeLabCatalog.Validate(options, request.Values, out string? invalid);
			if (invalid is not null)
			{
				return (StatusCodes.Status400BadRequest, new { error = invalid });
			}

			string[] paths = [.. options.Select(o => o.YamlPath)];
			if (ConfigYamlEditor.FindRefusal(file.Text, paths) is { } refusal)
			{
				return (StatusCodes.Status422UnprocessableEntity, new { error = Refused(refusal), refused = true });
			}

			string? directory = Path.GetDirectoryName(_configPath);
			try
			{
				SiteConfigReader.Parse(file.Text, directory);
			}
			catch (SiteConfigException ex)
			{
				return (StatusCodes.Status422UnprocessableEntity, new { error = $"{FileName} doesn't load ({ex.Message}). Fix it, then save." });
			}

			List<ConfigYamlChange> changes = ThemeLabCatalog.Changes(options, wanted, ConfigYamlEditor.ReadValues(file.Text, paths));
			if (changes.Count == 0)
			{
				return (StatusCodes.Status200OK, new
				{
					saved = false,
					changed = Array.Empty<string>(),
					version = file.Version,
					message = $"{FileName} already has these values."
				});
			}

			if (Edit(file.Text, changes, paths, directory, out string edited) is { } failure)
			{
				_logger.LogWarning("Theme lab didn't save {File}: {Reason}", FileName, failure.Message);
				return (StatusCodes.Status422UnprocessableEntity, new { error = failure.Message, refused = failure.Refused });
			}

			string newVersion;
			try
			{
				newVersion = ThemeLabConfigFile.Write(_configPath, edited, file.HasBom);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				return (StatusCodes.Status500InternalServerError, new { error = $"Can't write {FileName}: {ex.Message}" });
			}

			string[] keys = [.. changes.Select(c => c.Path[ThemeLabCatalog.OptionsPath.Length..])];
			_logger.LogInformation("Theme lab saved {Keys} to {File}", string.Join(", ", keys), _configPath);
			_report?.Invoke($"saved {string.Join(", ", keys)} to {FileName}");
			return (StatusCodes.Status200OK, new { saved = true, changed = keys, version = newVersion });
		}
	}

	// The edit, then the edited text loaded the way a build loads it, with every option at the value the
	// file now asks for. Nothing is written unless all of it holds.
	private (string Message, bool Refused)? Edit(string text, List<ConfigYamlChange> changes, string[] paths, string? directory,
		out string edited)
	{
		ConfigYamlEdit edit = ConfigYamlEditor.Apply(text, changes);
		edited = edit.Yaml ?? text;
		if (edit.Refusal is not null)
		{
			return (Refused(edit.Refusal), true);
		}

		SiteConfig loaded;
		try
		{
			loaded = SiteConfigReader.Parse(edited, directory);
		}
		catch (SiteConfigException ex)
		{
			return ($"The edited {FileName} doesn't load ({ex.Message}), so it wasn't saved.", false);
		}

		return ThemeLabCatalog.Mismatch(ThemeLabCatalog.CreateOptions(), loaded.Theme.Options,
				ConfigYamlEditor.ReadValues(edited, paths), _environment) is { } mismatch
			? ($"The edited {FileName} didn't load as intended ({mismatch}), so it wasn't saved.", false)
			: null;
	}

	private string Refused(string reason) =>
		$"{FileName} can't be edited safely: {reason}. Copy the YAML from Show YAML into the file instead.";

	private static async Task WriteScriptAsync(HttpContext context)
	{
		byte[] script = _script.Value;
		HttpResponse response = context.Response;
		response.ContentType = "application/javascript; charset=utf-8";
		response.Headers.CacheControl = _noStore;
		response.ContentLength = script.Length;
		await response.Body.WriteAsync(script, context.RequestAborted);
	}

	private async Task WriteCatalogAsync(HttpContext context)
	{
		context.Response.Headers.CacheControl = _noStore;
		(int status, object body) = Catalog();
		await DocsApiRequests.WriteJsonAsync(context, status, body);
	}

	private async Task SaveAsync(HttpContext context)
	{
		context.Response.Headers.CacheControl = _noStore;
		byte[]? body = await DocsApiRequests.ReadBodyAsync(context.Request, MaxSaveBodyBytes, context.RequestAborted);
		if (body is null)
		{
			await DocsApiRequests.WriteJsonAsync(context, StatusCodes.Status413PayloadTooLarge,
				new { error = "The request is too large to be a theme lab save." });
			return;
		}

		ThemeLabSaveRequest? request;
		try
		{
			request = JsonSerializer.Deserialize<ThemeLabSaveRequest>(body, _json);
		}
		catch (JsonException)
		{
			request = null;
		}

		(int status, object reply) = request is null
			? (StatusCodes.Status400BadRequest, (object)new { error = "The request body is not valid JSON." })
			: Save(request);
		await DocsApiRequests.WriteJsonAsync(context, status, reply);
	}
}

/// <summary>The theme lab's save request body.</summary>
internal sealed class ThemeLabSaveRequest
{
	/// <summary>The catalog version the page loaded; a save against a file changed since then is refused.</summary>
	public string? Version { get; set; }

	/// <summary>Option keys and their new values; <c>null</c> removes the key from the file.</summary>
	public Dictionary<string, JsonElement>? Values { get; set; }
}
