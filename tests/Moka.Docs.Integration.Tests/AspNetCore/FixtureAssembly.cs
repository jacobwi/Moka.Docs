using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     A small library compiled once per test run, with its XML documentation file beside it,
///     for the reflection-based API model. Compiling it keeps the metadata real: compiler-generated
///     record members, nullable attributes and documentation IDs all come from the C# compiler.
/// </summary>
internal static class FixtureAssembly
{
	private static readonly Lazy<Assembly> _assembly = new(Compile);

	/// <summary>The loaded fixture assembly.</summary>
	public static Assembly Instance => _assembly.Value;

	private const string _source = """
	                               using System;
	                               using System.Threading;

	                               namespace Fixture.Shapes
	                               {
	                                   /// <summary>A shape.</summary>
	                                   public abstract class Shape
	                                   {
	                                       /// <summary>Creates a shape.</summary>
	                                       protected Shape() { }

	                                       /// <summary>Creates a named shape.</summary>
	                                       /// <param name="name">The name.</param>
	                                       protected internal Shape(string name) { Name = name; }

	                                       /// <summary>The name.</summary>
	                                       public string Name { get; private set; } = "";

	                                       /// <summary>The id.</summary>
	                                       public string Id { get; init; } = "";

	                                       /// <summary>The area.</summary>
	                                       public abstract double Area { get; }

	                                       /// <summary>The maximum number of sides.</summary>
	                                       public const int MaxSides = 12;

	                                       /// <summary>The unit of length.</summary>
	                                       public static readonly string Unit = "cm";

	                                       /// <summary>Changes since creation.</summary>
	                                       protected internal int Revision;

	                                       /// <summary>Raised when the shape changes.</summary>
	                                       public event EventHandler? Changed;

	                                       /// <summary>Called when the shape changes.</summary>
	                                       protected virtual void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);

	                                       /// <summary>Scales the shape.</summary>
	                                       /// <param name="factor">The factor.</param>
	                                       public abstract Shape Scale(double factor);

	                                       /// <summary>Only for this assembly.</summary>
	                                       internal void InternalHelper() { }

	                                       /// <summary>Only for derived types in this assembly.</summary>
	                                       private protected void PrivateProtectedHelper() { }

	                                       /// <summary>Describes a shape.</summary>
	                                       public sealed class Metadata
	                                       {
	                                           /// <summary>A free-form tag.</summary>
	                                           public string? Tag { get; set; }
	                                       }

	                                       /// <summary>Cached measurements.</summary>
	                                       protected class Cache
	                                       {
	                                           /// <summary>When the cache was filled.</summary>
	                                           public DateTime FilledAt { get; set; }
	                                       }
	                                   }

	                                   /// <summary>A square.</summary>
	                                   public class Square : Shape
	                                   {
	                                       /// <inheritdoc />
	                                       public override double Area => 1;

	                                       /// <inheritdoc />
	                                       public sealed override Shape Scale(double factor) => this;
	                                   }

	                                   /// <summary>A circle.</summary>
	                                   public sealed class Circle : Shape
	                                   {
	                                       /// <inheritdoc />
	                                       public override double Area => 3.14;

	                                       /// <inheritdoc />
	                                       public override Shape Scale(double factor) => this;

	                                       /// <summary>Not reachable: nothing can derive from a sealed class.</summary>
	                                       protected override void OnChanged() { }
	                                   }

	                                   /// <summary>An amount of money.</summary>
	                                   public readonly struct Money
	                                   {
	                                       /// <summary>Adds two amounts.</summary>
	                                       /// <param name="left">The first amount.</param>
	                                       /// <param name="right">The second amount.</param>
	                                       public static Money operator +(Money left, Money right) => left;

	                                       /// <summary>Negates an amount.</summary>
	                                       public static Money operator -(Money value) => value;

	                                       /// <summary>The amount as a number.</summary>
	                                       public static implicit operator decimal(Money value) => 0m;

	                                       /// <summary>An amount from a number.</summary>
	                                       public static explicit operator Money(decimal value) => default;
	                                   }

	                                   /// <summary>Handles a change.</summary>
	                                   /// <param name="sender">What changed.</param>
	                                   /// <param name="count">How many changes so far.</param>
	                                   /// <param name="note">A note about the change.</param>
	                                   /// <returns>Whether the change was handled.</returns>
	                                   public delegate bool ChangeHandler<in T>(T sender, ref int count, out string? note);

	                                   /// <summary>Extension methods for shapes.</summary>
	                                   public static class ShapeExtensions
	                                   {
	                                       /// <summary>Describes a shape.</summary>
	                                       public static string Describe(this Shape shape, string? prefix = null, params int[] values) => "";
	                                   }

	                                   /// <summary>A point.</summary>
	                                   public record Point(int X, int Y);

	                                   /// <summary>Something that can be resized.</summary>
	                                   public interface IResizable
	                                   {
	                                       /// <summary>The width.</summary>
	                                       double Width { get; }

	                                       /// <summary>Creates one with a width.</summary>
	                                       static abstract IResizable Create(double width);

	                                       /// <summary>Restores the original size.</summary>
	                                       void Reset() { }
	                                   }

	                                   /// <summary>A container.</summary>
	                                   public class Outer<T>
	                                   {
	                                       /// <summary>Lives inside the container.</summary>
	                                       public class Inner
	                                       {
	                                           /// <summary>Runs the inner part.</summary>
	                                           public void Run(T value, Outer<int>.Inner other, CancellationToken cancellationToken = default) { }
	                                       }
	                                   }
	                               }
	                               """;

	private static Assembly Compile()
	{
		string platformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? "";
		IEnumerable<MetadataReference> references = platformAssemblies
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Select(path => MetadataReference.CreateFromFile(path));

		CSharpCompilation compilation = CSharpCompilation.Create(
			"Moka.Docs.ReflectionFixture",
			[CSharpSyntaxTree.ParseText(_source, new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose))],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

		string directory = Path.Combine(AppContext.BaseDirectory, "reflection-fixture");
		try
		{
			return Assembly.LoadFrom(Emit(compilation, directory));
		}
		catch (IOException)
		{
			// Another test process for this framework still has the fixture loaded.
			return Assembly.LoadFrom(Emit(compilation, $"{directory}-{Environment.ProcessId}"));
		}
	}

	/// <summary>
	///     Writes the assembly and its XML documentation file into <paramref name="directory" />.
	///     The builder finds the XML file next to the assembly's location, so the fixture has to
	///     be loaded from disk rather than from bytes.
	/// </summary>
	private static string Emit(CSharpCompilation compilation, string directory)
	{
		Directory.CreateDirectory(directory);
		string dllPath = Path.Combine(directory, "Moka.Docs.ReflectionFixture.dll");

		using FileStream pe = File.Create(dllPath);
		using FileStream xml = File.Create(Path.ChangeExtension(dllPath, ".xml"));
		EmitResult result = compilation.Emit(pe, xmlDocumentationStream: xml);
		if (!result.Success)
		{
			throw new InvalidOperationException(
				"The fixture did not compile: " + string.Join(Environment.NewLine, result.Diagnostics));
		}

		return dllPath;
	}
}
