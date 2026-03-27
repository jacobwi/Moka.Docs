// SampleLibrary — A sample library with comprehensive XML documentation for testing MokaDocs

namespace SampleLibrary;

/// <summary>
///     A calculator that performs arithmetic operations with full error handling.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="Calculator" /> class is the primary entry point for arithmetic in SampleLibrary.
///         It supports basic operations like addition and division, as well as generic functional
///         transformations via <see cref="Process{T}" />.
///     </para>
///     <para>
///         For division operations that may fail, prefer <see cref="TryDivide" /> over <see cref="Divide" />
///         to avoid exceptions in hot paths.
///     </para>
/// </remarks>
/// <example>
///     Basic usage:
///     <code>
/// var calc = new Calculator();
/// int sum = calc.Add(2, 3);           // returns 5
/// double quotient = calc.Divide(10, 3); // returns 3.333...
/// 
/// // Safe division
/// var result = calc.TryDivide(10, 0);
/// if (!result.Success)
///     Console.WriteLine(result.Error); // "Cannot divide by zero."
/// </code>
/// </example>
/// <seealso cref="OperationResult{T}" />
/// <seealso cref="LegacyCalculator" />
public sealed class Calculator
{
	/// <summary>
	///     Adds two integers together.
	/// </summary>
	/// <param name="a">The first operand.</param>
	/// <param name="b">The second operand.</param>
	/// <returns>The sum of <paramref name="a" /> and <paramref name="b" />.</returns>
	/// <example>
	///     <code>
	/// var calc = new Calculator();
	/// int result = calc.Add(10, 20); // 30
	/// </code>
	/// </example>
	public int Add(int a, int b) => a + b;

	/// <summary>
	///     Divides <paramref name="dividend" /> by <paramref name="divisor" />.
	/// </summary>
	/// <param name="dividend">The number to divide.</param>
	/// <param name="divisor">The number to divide by. Must be positive.</param>
	/// <returns>The quotient of <paramref name="dividend" /> divided by <paramref name="divisor" />.</returns>
	/// <exception cref="DivideByZeroException">
	///     Thrown when <paramref name="divisor" /> is zero.
	/// </exception>
	/// <exception cref="ArgumentOutOfRangeException">
	///     Thrown when <paramref name="divisor" /> is negative.
	/// </exception>
	/// <remarks>
	///     This method throws on invalid input. For a non-throwing alternative, use
	///     <see cref="TryDivide" /> which returns an <see cref="OperationResult{T}" />.
	/// </remarks>
	/// <example>
	///     <code>
	/// var calc = new Calculator();
	/// double result = calc.Divide(100, 3); // 33.333...
	/// </code>
	/// </example>
	/// <seealso cref="TryDivide" />
	public double Divide(double dividend, double divisor)
	{
		if (divisor == 0)
		{
			throw new DivideByZeroException("Cannot divide by zero.");
		}

		if (divisor < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(divisor), "Divisor must be non-negative.");
		}

		return dividend / divisor;
	}

	/// <summary>
	///     Attempts to divide <paramref name="dividend" /> by <paramref name="divisor" /> without throwing exceptions.
	/// </summary>
	/// <param name="dividend">The number to divide.</param>
	/// <param name="divisor">The number to divide by.</param>
	/// <returns>
	///     An <see cref="OperationResult{T}" /> containing the quotient on success,
	///     or an error message if the divisor is zero or negative.
	/// </returns>
	/// <remarks>
	///     Unlike <see cref="Divide" />, this method never throws. It wraps the result in an
	///     <see cref="OperationResult{T}" /> so callers can handle errors without try/catch.
	/// </remarks>
	/// <example>
	///     <code>
	/// var calc = new Calculator();
	/// var result = calc.TryDivide(10, 0);
	/// if (result.Success)
	///     Console.WriteLine(result.Value);
	/// else
	///     Console.WriteLine(result.Error); // "Cannot divide by zero."
	/// </code>
	/// </example>
	/// <seealso cref="Divide" />
	/// <seealso cref="OperationResult{T}" />
	public OperationResult<double> TryDivide(double dividend, double divisor)
	{
		if (divisor == 0)
		{
			return OperationResult<double>.Fail("Cannot divide by zero.");
		}

		if (divisor < 0)
		{
			return OperationResult<double>.Fail("Divisor must be non-negative.");
		}

		return OperationResult<double>.Ok(dividend / divisor);
	}

	/// <summary>
	///     Processes a value using the specified <paramref name="transform" /> function.
	/// </summary>
	/// <typeparam name="T">The type of value to process.</typeparam>
	/// <param name="value">The input value.</param>
	/// <param name="transform">A function that transforms the input value.</param>
	/// <returns>The result of applying <paramref name="transform" /> to <paramref name="value" />.</returns>
	/// <remarks>
	///     This is a convenience method for applying a single transformation. For chaining
	///     multiple transforms, consider using LINQ or a pipeline pattern.
	/// </remarks>
	/// <example>
	///     <code>
	/// var calc = new Calculator();
	/// string upper = calc.Process("hello", s => s.ToUpper()); // "HELLO"
	/// int doubled = calc.Process(21, x => x * 2);              // 42
	/// </code>
	/// </example>
	/// <seealso cref="Add" />
	public T Process<T>(T value, Func<T, T> transform) => transform(value);
}

/// <summary>
///     Represents the result of an operation that may succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the result value on success.</typeparam>
/// <remarks>
///     <para>
///         <see cref="OperationResult{T}" /> provides a structured alternative to exceptions.
///         Use <see cref="Ok" /> to create a success result and <see cref="Fail" /> to create a failure.
///     </para>
///     <para>
///         This pattern is sometimes called the Result monad or Either type in functional programming.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Success
/// var ok = OperationResult&lt;int&gt;.Ok(42);
/// Console.WriteLine(ok.Value); // 42
/// 
/// // Failure
/// var err = OperationResult&lt;int&gt;.Fail("Something went wrong");
/// Console.WriteLine(err.Error); // "Something went wrong"
/// </code>
/// </example>
public sealed record OperationResult<T>
{
	/// <summary>
	///     Gets a value indicating whether the operation succeeded.
	/// </summary>
	/// <value><see langword="true" /> if the operation completed successfully; otherwise, <see langword="false" />.</value>
	public required bool Success { get; init; }

	/// <summary>
	///     Gets the result value when <see cref="Success" /> is <see langword="true" />.
	/// </summary>
	/// <value>The computed value, or <see langword="default" /> if the operation failed.</value>
	public T? Value { get; init; }

	/// <summary>
	///     Gets the error message when <see cref="Success" /> is <see langword="false" />.
	/// </summary>
	/// <value>A human-readable error description, or <see langword="null" /> if the operation succeeded.</value>
	public string? Error { get; init; }

	/// <summary>
	///     Creates a successful result containing the specified <paramref name="value" />.
	/// </summary>
	/// <param name="value">The result value.</param>
	/// <returns>A new <see cref="OperationResult{T}" /> with <see cref="Success" /> set to <see langword="true" />.</returns>
	/// <example>
	///     <code>
	/// var result = OperationResult&lt;int&gt;.Ok(42);
	/// // result.Success == true, result.Value == 42
	/// </code>
	/// </example>
	public static OperationResult<T> Ok(T value) => new() { Success = true, Value = value };

	/// <summary>
	///     Creates a failed result with the specified <paramref name="error" /> message.
	/// </summary>
	/// <param name="error">A description of what went wrong.</param>
	/// <returns>A new <see cref="OperationResult{T}" /> with <see cref="Success" /> set to <see langword="false" />.</returns>
	/// <example>
	///     <code>
	/// var result = OperationResult&lt;int&gt;.Fail("Division by zero");
	/// // result.Success == false, result.Error == "Division by zero"
	/// </code>
	/// </example>
	public static OperationResult<T> Fail(string error) => new() { Success = false, Error = error };
}

/// <summary>
///     Defines a geometric shape that can calculate its area.
/// </summary>
/// <remarks>
///     Implement this interface to create custom shapes that integrate with
///     the SampleLibrary shape system. All implementations must provide a
///     human-readable <see cref="Name" /> and a <see cref="CalculateArea" /> method.
/// </remarks>
/// <example>
///     <code>
/// public record Triangle(double Base, double Height) : IShape
/// {
///     public string Name => "Triangle";
///     public double CalculateArea() => 0.5 * Base * Height;
/// }
/// </code>
/// </example>
/// <seealso cref="Circle" />
/// <seealso cref="Rectangle" />
public interface IShape
{
	/// <summary>
	///     Gets the human-readable name of the shape.
	/// </summary>
	/// <value>A non-null string identifying the shape type (e.g., "Circle", "Rectangle").</value>
	string Name { get; }

	/// <summary>
	///     Calculates the area of the shape.
	/// </summary>
	/// <returns>The area in square units. Always non-negative.</returns>
	double CalculateArea();
}

/// <summary>
///     A circle defined by its <see cref="Radius" />.
/// </summary>
/// <param name="Radius">The radius of the circle. Must be non-negative.</param>
/// <remarks>
///     Area is computed as <c>pi * r^2</c> using <see cref="Math.PI" /> for full double precision.
/// </remarks>
/// <example>
///     <code>
/// var circle = new Circle(5.0);
/// Console.WriteLine(circle.Name);            // "Circle"
/// Console.WriteLine(circle.CalculateArea());  // 78.5398...
/// </code>
/// </example>
/// <seealso cref="IShape" />
/// <seealso cref="Rectangle" />
public sealed record Circle(double Radius) : IShape
{
	/// <inheritdoc />
	public string Name => "Circle";

	/// <inheritdoc />
	/// <remarks>Uses the formula <c>pi * Radius^2</c>.</remarks>
	public double CalculateArea() => Math.PI * Radius * Radius;
}

/// <summary>
///     A rectangle defined by its <see cref="Width" /> and <see cref="Height" />.
/// </summary>
/// <param name="Width">The width of the rectangle. Must be non-negative.</param>
/// <param name="Height">The height of the rectangle. Must be non-negative.</param>
/// <remarks>
///     Area is computed as <c>Width * Height</c>. For a square, pass the same value for both parameters.
/// </remarks>
/// <example>
///     <code>
/// var rect = new Rectangle(4.0, 6.0);
/// Console.WriteLine(rect.Name);            // "Rectangle"
/// Console.WriteLine(rect.CalculateArea());  // 24.0
/// 
/// // A square is just a rectangle with equal sides
/// var square = new Rectangle(5.0, 5.0);    // area = 25.0
/// </code>
/// </example>
/// <seealso cref="IShape" />
/// <seealso cref="Circle" />
public sealed record Rectangle(double Width, double Height) : IShape
{
	/// <inheritdoc />
	public string Name => "Rectangle";

	/// <inheritdoc />
	/// <remarks>Uses the formula <c>Width * Height</c>.</remarks>
	public double CalculateArea() => Width * Height;
}

/// <summary>
///     Available color options for styling and theming.
/// </summary>
/// <remarks>
///     The <see cref="Yellow" /> member has an explicit value of <c>10</c>,
///     while the other members use the default sequential values.
/// </remarks>
public enum Color
{
	/// <summary>The color red (hex <c>#FF0000</c>).</summary>
	Red,

	/// <summary>The color green (hex <c>#00FF00</c>).</summary>
	Green,

	/// <summary>The color blue (hex <c>#0000FF</c>).</summary>
	Blue,

	/// <summary>The color yellow (hex <c>#FFFF00</c>). Explicit value of 10.</summary>
	Yellow = 10
}

/// <summary>
///     A callback delegate invoked when a value changes.
/// </summary>
/// <typeparam name="T">The type of the value being observed.</typeparam>
/// <param name="oldValue">The previous value before the change.</param>
/// <param name="newValue">The new value after the change.</param>
/// <remarks>
///     Use this delegate with event-driven patterns to observe state changes
///     in your application.
/// </remarks>
/// <example>
///     <code>
/// ValueChangedHandler&lt;int&gt; handler = (oldVal, newVal) =>
///     Console.WriteLine($"Changed from {oldVal} to {newVal}");
/// handler(1, 2); // "Changed from 1 to 2"
/// </code>
/// </example>
public delegate void ValueChangedHandler<T>(T oldValue, T newValue);

/// <summary>
///     Provides extension methods for <see cref="string" /> values.
/// </summary>
/// <remarks>
///     Import the <c>SampleLibrary</c> namespace to make these extension methods available
///     on all string instances.
/// </remarks>
public static class StringExtensions
{
	/// <summary>
	///     Truncates a string to the specified maximum length, appending a suffix if truncated.
	/// </summary>
	/// <param name="value">The string to truncate.</param>
	/// <param name="maxLength">The maximum allowed length, including the suffix.</param>
	/// <param name="suffix">The suffix to append when the string is truncated. Defaults to <c>"..."</c>.</param>
	/// <returns>
	///     The original string if its length is at most <paramref name="maxLength" />;
	///     otherwise, a truncated copy with <paramref name="suffix" /> appended.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">
	///     Thrown when <paramref name="maxLength" /> is less than the length of <paramref name="suffix" />.
	/// </exception>
	/// <example>
	///     <code>
	/// string text = "Hello, World!";
	/// string short1 = text.Truncate(8);          // "Hello..."
	/// string short2 = text.Truncate(8, " [+]");  // "Hell [+]"
	/// </code>
	/// </example>
	public static string Truncate(this string value, int maxLength, string suffix = "...")
	{
		if (value.Length <= maxLength)
		{
			return value;
		}

		return value[..(maxLength - suffix.Length)] + suffix;
	}
}

/// <summary>
///     Provides extension methods for numeric types.
/// </summary>
/// <remarks>
///     These utilities help with common numeric operations like clamping and range checking.
///     Import the <c>SampleLibrary</c> namespace to use them.
/// </remarks>
public static class NumericExtensions
{
	/// <summary>
	///     Clamps an integer value to the specified inclusive range.
	/// </summary>
	/// <param name="value">The value to clamp.</param>
	/// <param name="min">The minimum allowed value (inclusive).</param>
	/// <param name="max">The maximum allowed value (inclusive).</param>
	/// <returns>
	///     <paramref name="min" /> if <paramref name="value" /> is less than <paramref name="min" />;
	///     <paramref name="max" /> if <paramref name="value" /> is greater than <paramref name="max" />;
	///     otherwise, <paramref name="value" /> itself.
	/// </returns>
	/// <example>
	///     <code>
	/// int clamped = 150.Clamp(0, 100); // 100
	/// int inRange = 42.Clamp(0, 100);  // 42
	/// int atMin   = (-5).Clamp(0, 100); // 0
	/// </code>
	/// </example>
	public static int Clamp(this int value, int min, int max)
	{
		if (value < min)
		{
			return min;
		}

		if (value > max)
		{
			return max;
		}

		return value;
	}

	/// <summary>
	///     Determines whether an integer value falls within the specified inclusive range.
	/// </summary>
	/// <param name="value">The value to check.</param>
	/// <param name="min">The lower bound of the range (inclusive).</param>
	/// <param name="max">The upper bound of the range (inclusive).</param>
	/// <returns>
	///     <see langword="true" /> if <paramref name="value" /> is between <paramref name="min" />
	///     and <paramref name="max" /> (inclusive); otherwise, <see langword="false" />.
	/// </returns>
	/// <example>
	///     <code>
	/// bool yes = 42.IsInRange(0, 100);  // true
	/// bool no  = 150.IsInRange(0, 100); // false
	/// </code>
	/// </example>
	public static bool IsInRange(this int value, int min, int max) => value >= min && value <= max;
}

/// <summary>
///     An observable list that fires events when items are added.
/// </summary>
/// <typeparam name="T">The type of elements in the list. Must be a non-null type.</typeparam>
/// <remarks>
///     <para>
///         <see cref="ObservableList{T}" /> wraps a standard <see cref="List{T}" /> and raises the
///         <see cref="ItemAdded" /> event each time a new item is inserted. This makes it suitable
///         for building reactive UIs or audit trails.
///     </para>
///     <para>
///         Subscribe to <see cref="ItemAdded" /> before calling <see cref="Add" /> to ensure you
///         capture every event.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// var list = new ObservableList&lt;string&gt;();
/// list.ItemAdded += (sender, item) => Console.WriteLine($"Added: {item}");
/// list.Add("Hello");  // prints: Added: Hello
/// list.Add("World");  // prints: Added: World
/// Console.WriteLine(list.Count); // 2
/// </code>
/// </example>
public class ObservableList<T> where T : notnull
{
	private readonly List<T> _items = [];

	/// <summary>
	///     Gets the number of items currently in the list.
	/// </summary>
	/// <value>A non-negative integer representing the count of items.</value>
	public int Count => _items.Count;

	/// <summary>
	///     Gets the item at the specified zero-based <paramref name="index" />.
	/// </summary>
	/// <param name="index">The zero-based index of the item to retrieve.</param>
	/// <returns>The item at position <paramref name="index" />.</returns>
	/// <exception cref="ArgumentOutOfRangeException">
	///     Thrown when <paramref name="index" /> is less than zero or greater than or equal to <see cref="Count" />.
	/// </exception>
	public T this[int index] => _items[index];

	/// <summary>
	///     Occurs when an item is added to the list via <see cref="Add" />.
	/// </summary>
	/// <remarks>
	///     The event argument contains the item that was just added.
	///     Handlers are invoked synchronously on the calling thread.
	/// </remarks>
	public event EventHandler<T>? ItemAdded;

	/// <summary>
	///     Adds an item to the end of the list and raises <see cref="ItemAdded" />.
	/// </summary>
	/// <param name="item">The item to add. Must not be <see langword="null" />.</param>
	/// <remarks>
	///     The <see cref="ItemAdded" /> event fires after the item has been added to the
	///     internal collection, so <see cref="Count" /> will already reflect the new item
	///     when handlers execute.
	/// </remarks>
	/// <example>
	///     <code>
	/// var list = new ObservableList&lt;int&gt;();
	/// list.Add(42);
	/// Console.WriteLine(list[0]); // 42
	/// </code>
	/// </example>
	public virtual void Add(T item)
	{
		_items.Add(item);
		ItemAdded?.Invoke(this, item);
	}
}

/// <summary>
///     Provides a fluent builder for configuring and creating <see cref="OperationResult{T}" /> chains.
/// </summary>
/// <typeparam name="T">The type of the value being processed.</typeparam>
/// <remarks>
///     Use <see cref="ResultBuilder{T}" /> to chain multiple operations that each return
///     an <see cref="OperationResult{T}" />, short-circuiting on the first failure.
///     This is similar to monadic bind in functional programming.
/// </remarks>
/// <example>
///     <code>
/// var builder = new ResultBuilder&lt;double&gt;(10.0);
/// var result = builder
///     .Then(x => x > 0
///         ? OperationResult&lt;double&gt;.Ok(x)
///         : OperationResult&lt;double&gt;.Fail("Must be positive"))
///     .Then(x => OperationResult&lt;double&gt;.Ok(x * 2))
///     .Build();
/// 
/// Console.WriteLine(result.Value); // 20.0
/// </code>
/// </example>
/// <seealso cref="OperationResult{T}" />
public sealed class ResultBuilder<T>
{
	private OperationResult<T> _current;

	/// <summary>
	///     Initializes a new <see cref="ResultBuilder{T}" /> with a starting value.
	/// </summary>
	/// <param name="initialValue">The initial value to begin the chain with.</param>
	public ResultBuilder(T initialValue)
	{
		_current = OperationResult<T>.Ok(initialValue);
	}

	/// <summary>
	///     Chains the next operation. If the current result is a failure, the operation is skipped.
	/// </summary>
	/// <param name="operation">A function that takes the current value and returns a new result.</param>
	/// <returns>This builder instance for fluent chaining.</returns>
	/// <example>
	///     <code>
	/// var result = new ResultBuilder&lt;int&gt;(5)
	///     .Then(x => OperationResult&lt;int&gt;.Ok(x + 10))
	///     .Build();
	/// // result.Value == 15
	/// </code>
	/// </example>
	public ResultBuilder<T> Then(Func<T, OperationResult<T>> operation)
	{
		if (_current.Success && _current.Value is not null)
		{
			_current = operation(_current.Value);
		}

		return this;
	}

	/// <summary>
	///     Builds and returns the final <see cref="OperationResult{T}" />.
	/// </summary>
	/// <returns>The result of the entire chain — either the final success value or the first failure.</returns>
	public OperationResult<T> Build() => _current;
}

/// <summary>
///     A calculator that has been superseded by <see cref="Calculator" />.
/// </summary>
/// <remarks>
///     This class exists only for backward compatibility. All new code should use
///     <see cref="Calculator" /> directly.
/// </remarks>
[Obsolete("Use Calculator instead. This class will be removed in v3.0.")]
public sealed class LegacyCalculator
{
	/// <summary>
	///     Adds two numbers.
	/// </summary>
	/// <param name="a">The first operand.</param>
	/// <param name="b">The second operand.</param>
	/// <returns>The sum of <paramref name="a" /> and <paramref name="b" />.</returns>
	[Obsolete("Use Calculator.Add instead.")]
	public int Add(int a, int b) => a + b;
}
