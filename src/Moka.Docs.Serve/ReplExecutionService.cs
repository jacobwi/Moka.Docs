using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace Moka.Docs.Serve;

/// <summary>
///     Executes C# code snippets using Roslyn scripting. Captures console output,
///     handles compilation errors, and enforces a timeout to prevent runaway code.
/// </summary>
public sealed class ReplExecutionService
{
    private readonly ILogger<ReplExecutionService> _logger;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     Base script options with safe namespace imports and assembly references.
    ///     Additional packages and project assemblies are appended via <see cref="LoadPackagesAsync" />.
    /// </summary>
    private ScriptOptions _scriptOptions = ScriptOptions.Default
        .WithImports(
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "System.Text",
            "System.Math",
            "System.Text.RegularExpressions")
        .WithReferences(
            typeof(object).Assembly, // System.Runtime
            typeof(Console).Assembly, // System.Console
            typeof(Enumerable).Assembly, // System.Linq
            typeof(Regex).Assembly, // System.Text.RegularExpressions
            Assembly.Load("System.Collections"), // System.Collections
            Assembly.Load("System.Runtime")); // System.Runtime

    /// <summary>
    ///     Creates a new REPL execution service.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public ReplExecutionService(ILogger<ReplExecutionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///     Resolves the given NuGet package specifications and adds their assemblies
    ///     and root namespaces to the REPL script options. Each spec is either
    ///     "PackageName" or "PackageName@Version".
    /// </summary>
    public async Task LoadPackagesAsync(IReadOnlyList<string> packageSpecs, CancellationToken ct = default)
    {
        if (packageSpecs.Count == 0)
            return;

        var resolver = new NuGetPackageResolver(_logger);
        var resolved = await resolver.ResolveAsync(packageSpecs, ct);
        AddResolvedPackages(resolved);
    }

    /// <summary>
    ///     Loads a project output assembly into the REPL so users can reference
    ///     the documented project's types directly.
    /// </summary>
    public void LoadProjectAssembly(string dllPath)
    {
        var resolver = new NuGetPackageResolver(_logger);
        var resolved = resolver.LoadAssemblyFromPath(dllPath);
        AddResolvedPackages(resolved);
    }

    private void AddResolvedPackages(NuGetPackageResolver.ResolvedPackages resolved)
    {
        if (resolved.Assemblies.Count > 0)
            _scriptOptions = _scriptOptions.AddReferences(resolved.Assemblies);

        if (resolved.Namespaces.Count > 0)
            _scriptOptions = _scriptOptions.AddImports(resolved.Namespaces);
    }

    /// <summary>
    ///     Executes the given C# code and returns the result.
    /// </summary>
    /// <param name="code">The C# source code to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The execution result containing output and/or error messages.</returns>
    public async Task<ReplResult> ExecuteAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return new ReplResult { Output = "", Error = "No code provided." };

        // Enforce maximum code length to prevent abuse
        if (code.Length > 10_000) return new ReplResult { Error = "Code exceeds maximum length of 10,000 characters." };

        _logger.LogDebug("REPL: Executing {Length} characters of code", code.Length);

        // Capture Console.Out by redirecting to a StringWriter
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        try
        {
            Console.SetOut(outputWriter);
            Console.SetError(errorWriter);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(_timeout);

            var script = CSharpScript.Create(code, _scriptOptions);
            var diagnostics = script.Compile(timeoutCts.Token);

            // Check for compilation errors
            var errors = diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (errors.Count > 0)
            {
                var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
                _logger.LogDebug("REPL: Compilation failed with {Count} error(s)", errors.Count);
                return new ReplResult { Error = errorMessages };
            }

            // Run the script
            var result = await script.RunAsync(cancellationToken: timeoutCts.Token);

            var output = outputWriter.ToString();
            var errorOutput = errorWriter.ToString();

            // If the script returned a value and nothing was written to Console, show the return value
            if (result.ReturnValue is not null && string.IsNullOrEmpty(output))
                output = result.ReturnValue.ToString() ?? "";

            if (!string.IsNullOrEmpty(errorOutput))
                output = string.IsNullOrEmpty(output)
                    ? errorOutput
                    : output + "\n" + errorOutput;

            _logger.LogDebug("REPL: Execution completed successfully");
            return new ReplResult { Output = output };
        }
        catch (CompilationErrorException ex)
        {
            _logger.LogDebug("REPL: Compilation error — {Message}", ex.Message);
            return new ReplResult { Error = ex.Message };
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("REPL: Execution timed out after {Timeout}s", _timeout.TotalSeconds);
            return new ReplResult { Error = $"Execution timed out after {_timeout.TotalSeconds} seconds." };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "REPL: Runtime error");
            return new ReplResult { Error = $"Runtime error: {ex.Message}" };
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}

/// <summary>
///     The result of a REPL code execution.
/// </summary>
public sealed class ReplResult
{
    /// <summary>The captured console output (stdout). Null or empty if no output.</summary>
    public string? Output { get; init; }

    /// <summary>Error message if compilation or execution failed. Null if successful.</summary>
    public string? Error { get; init; }
}