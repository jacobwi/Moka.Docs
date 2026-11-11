using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace Moka.Docs.Integration.Tests.AspNetCore;

// Compiled rather than hand-written so the grouping and marker types, the implementation methods and the
// documentation IDs are the compiler's own. On .NET 9 the compiler embeds the ExtensionMarkerAttribute
// the framework lacks.
internal static class ExtensionFixtureAssembly
{
	public const string Source = """
	                             using System;
	                             using System.Collections.Generic;

	                             namespace Fixture.Extensions
	                             {
	                                 /// <summary>A shape.</summary>
	                                 public class Shape
	                                 {
	                                     /// <summary>The width.</summary>
	                                     public double Width { get; set; }
	                                 }

	                                 /// <summary>A square.</summary>
	                                 public class Square : Shape
	                                 {
	                                 }

	                                 /// <summary>A point.</summary>
	                                 public struct Point
	                                 {
	                                     /// <summary>The x coordinate.</summary>
	                                     public int X;
	                                 }

	                                 /// <summary>Shape helpers.</summary>
	                                 public static class ShapeExtensions
	                                 {
	                                     /// <summary>Describes a shape.</summary>
	                                     /// <param name="shape">The shape.</param>
	                                     public static string Describe(this Shape shape) => "";

	                                     /// <summary>Doubles a number.</summary>
	                                     /// <param name="value">The number.</param>
	                                     public static int Twice(int value) => value * 2;

	                                     /// <summary>Members for any shape.</summary>
	                                     /// <param name="shape">The shape to extend.</param>
	                                     extension(Shape shape)
	                                     {
	                                         /// <summary>The perimeter.</summary>
	                                         [Obsolete("Use Width.")]
	                                         public double Perimeter => shape.Width * 4;

	                                         /// <summary>A label.</summary>
	                                         public string Label { get => ""; set { } }

	                                         /// <summary>Grows the shape.</summary>
	                                         /// <param name="factor">The factor.</param>
	                                         /// <returns>The grown shape.</returns>
	                                         public Shape Grow(double factor) => shape;

	                                         /// <summary>A unit shape.</summary>
	                                         public static Shape Unit() => new();

	                                         /// <summary>The default shape.</summary>
	                                         public static Shape Default => new();

	                                         /// <summary>Combines two shapes.</summary>
	                                         /// <param name="left">The first shape.</param>
	                                         /// <param name="right">The second shape.</param>
	                                         public static Shape operator +(Shape left, Shape right) => left;

	                                         internal double Hidden => 0;
	                                     }

	                                     /// <summary>Members for lists of comparable items.</summary>
	                                     /// <typeparam name="T">The item type.</typeparam>
	                                     /// <param name="list">The list.</param>
	                                     extension<T>(List<T> list) where T : IComparable<T>
	                                     {
	                                         /// <summary>The largest item.</summary>
	                                         public T Largest() => list[0];

	                                         /// <summary>Adds an item several times.</summary>
	                                         /// <param name="item">The item.</param>
	                                         /// <param name="count">How many times.</param>
	                                         public void AddMany(T item, int count) { }

	                                         /// <summary>Converts the items.</summary>
	                                         /// <typeparam name="TOut">The result type.</typeparam>
	                                         /// <param name="convert">The conversion.</param>
	                                         public List<TOut> Convert<TOut>(Func<T, TOut> convert) => new();

	                                         /// <summary>Whether the list is empty.</summary>
	                                         public bool IsEmpty => list.Count == 0;
	                                     }

	                                     /// <param name="point">The point to move.</param>
	                                     extension(ref Point point)
	                                     {
	                                         /// <summary>Moves the point.</summary>
	                                         /// <param name="dx">The distance.</param>
	                                         public void Move(int dx) => point.X += dx;
	                                     }

	                                     extension(Shape)
	                                     {
	                                         /// <summary>Creates a shape.</summary>
	                                         /// <param name="sides">The number of sides.</param>
	                                         public static Shape Create(int sides) => new();
	                                     }

	                                     /// <summary>Members for a shape that may be missing.</summary>
	                                     /// <param name="shape">The shape, if there is one.</param>
	                                     extension(Shape? shape)
	                                     {
	                                         /// <summary>Whether there is no shape.</summary>
	                                         public bool IsMissing => shape is null;
	                                     }
	                                 }
	                             }
	                             """;

	private static readonly Lazy<Assembly> _assembly = new(() =>
		FixtureAssembly.Compile("Moka.Docs.ExtensionFixture", Source, LanguageVersion.CSharp14, "extension-fixture"));

	public static Assembly Instance => _assembly.Value;
}
