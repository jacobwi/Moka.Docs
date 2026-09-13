using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Moka.Docs.Serve;

/// <summary>
///     Compiles and runs one REPL snippet with Roslyn scripting and captures its console output.
///     <see cref="ReplExecutionService" /> uses it in-process, and <see cref="ReplWorker" /> inside
///     the worker process.
/// </summary>
internal static class ReplScriptRunner
{
	/// <summary>The longest snippet accepted, in characters.</summary>
	internal const int MaxCodeLength = 10_000;

	/// <summary>The most console output kept per run, in characters.</summary>
	internal const int MaxOutputLength = 100_000;

	/// <summary>The imports and references every snippet starts with.</summary>
	internal static ScriptOptions DefaultOptions { get; } = ScriptOptions.Default
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
	///     Rejects a snippet that is empty or too long before anything compiles.
	/// </summary>
	/// <returns>The error result, or <c>null</c> when the snippet can run.</returns>
	internal static ReplResult? Validate(string code)
	{
		if (string.IsNullOrWhiteSpace(code))
		{
			return new ReplResult { Output = "", Error = "No code provided." };
		}

		return code.Length > MaxCodeLength
			? new ReplResult { Error = "Code exceeds maximum length of 10,000 characters." }
			: null;
	}

	/// <summary>
	///     Adds loaded assemblies as references and imports their root namespaces.
	/// </summary>
	internal static ScriptOptions WithAssemblies(ScriptOptions options, IReadOnlyCollection<Assembly> assemblies)
	{
		if (assemblies.Count == 0)
		{
			return options;
		}

		return options
			.AddReferences(assemblies)
			.AddImports(assemblies.SelectMany(NuGetPackageResolver.GetRootNamespaces).Distinct().Order());
	}

	/// <summary>
	///     Compiles and runs <paramref name="code" />. Compiler errors and exceptions thrown by the
	///     snippet come back in <see cref="ReplResult.Error" />; cancellation propagates.
	/// </summary>
	internal static async Task<ReplResult> RunAsync(string code, ScriptOptions options, CancellationToken ct)
	{
		TextWriter originalOut = Console.Out;
		TextWriter originalError = Console.Error;
		var output = new BoundedWriter(MaxOutputLength);
		var errorOutput = new BoundedWriter(MaxOutputLength);

		try
		{
			Console.SetOut(output);
			Console.SetError(errorOutput);

			Script<object> script = CSharpScript.Create(code, options);
			ImmutableArray<Diagnostic> diagnostics = script.Compile(ct);

			var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
			if (errors.Count > 0)
			{
				return new ReplResult { Error = string.Join("\n", errors.Select(e => e.GetMessage())) };
			}

			ScriptState<object> state = await script.RunAsync(cancellationToken: ct);

			string text = output.ToString();

			// A snippet that ends in an expression and prints nothing shows the expression's value.
			if (state.ReturnValue is not null && text.Length == 0)
			{
				text = state.ReturnValue.ToString() ?? "";
			}

			string errorText = errorOutput.ToString();
			if (errorText.Length > 0)
			{
				text = text.Length == 0 ? errorText : text + "\n" + errorText;
			}

			return new ReplResult { Output = text };
		}
		catch (CompilationErrorException ex)
		{
			return new ReplResult { Error = ex.Message };
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			return new ReplResult { Error = $"Runtime error: {ex.Message}" };
		}
		finally
		{
			Console.SetOut(originalOut);
			Console.SetError(originalError);
		}
	}

	/// <summary>
	///     Keeps console output up to a limit. Output used to go to an unbounded string, so a snippet
	///     printing in a loop grew it until the run was stopped.
	/// </summary>
	private sealed class BoundedWriter(int limit) : TextWriter
	{
		private readonly StringBuilder _text = new();
		private bool _truncated;

		public override Encoding Encoding => Encoding.UTF8;

		public override void Write(char value) => Append([value]);

		public override void Write(string? value) => Append(value.AsSpan());

		public override void Write(char[] buffer, int index, int count) => Append(buffer.AsSpan(index, count));

		public override void Write(ReadOnlySpan<char> buffer) => Append(buffer);

		public override string ToString() =>
			_truncated ? $"{_text}\n[Output truncated after {limit} characters.]" : _text.ToString();

		private void Append(ReadOnlySpan<char> value)
		{
			int room = limit - _text.Length;
			if (value.Length <= room)
			{
				_text.Append(value);
				return;
			}

			_text.Append(value[..Math.Max(room, 0)]);
			_truncated = true;
		}
	}
}
