namespace Moka.Docs.Core.Diagnostics;

/// <summary>
///     Collects diagnostic messages during the build pipeline.
/// </summary>
public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics = [];

    /// <summary>All collected diagnostics.</summary>
    public IReadOnlyList<Diagnostic> All => _diagnostics;

    /// <summary>Whether there are any error-level diagnostics.</summary>
    public bool HasErrors => _diagnostics.Exists(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>Whether there are any warning-level diagnostics.</summary>
    public bool HasWarnings => _diagnostics.Exists(d => d.Severity == DiagnosticSeverity.Warning);

    /// <summary>Number of diagnostics.</summary>
    public int Count => _diagnostics.Count;

    /// <summary>Add an error diagnostic.</summary>
    public void Error(string message, string? source = null)
    {
        _diagnostics.Add(new Diagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Message = message,
            Source = source
        });
    }

    /// <summary>Add a warning diagnostic.</summary>
    public void Warning(string message, string? source = null)
    {
        _diagnostics.Add(new Diagnostic
        {
            Severity = DiagnosticSeverity.Warning,
            Message = message,
            Source = source
        });
    }

    /// <summary>Add an info diagnostic.</summary>
    public void Info(string message, string? source = null)
    {
        _diagnostics.Add(new Diagnostic
        {
            Severity = DiagnosticSeverity.Info,
            Message = message,
            Source = source
        });
    }
}

/// <summary>
///     A single diagnostic message produced during the build.
/// </summary>
public sealed record Diagnostic
{
    /// <summary>The severity level.</summary>
    public required DiagnosticSeverity Severity { get; init; }

    /// <summary>The diagnostic message.</summary>
    public required string Message { get; init; }

    /// <summary>The source file or phase that produced this diagnostic.</summary>
    public string? Source { get; init; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"[{Severity}] {Message}";
    }
}

/// <summary>
///     Diagnostic severity levels.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>Informational message.</summary>
    Info,

    /// <summary>Warning that should be reviewed.</summary>
    Warning,

    /// <summary>Error that prevents correct output.</summary>
    Error
}