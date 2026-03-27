// SampleApi — Todo domain models

namespace SampleApi.Models;

/// <summary>
///     Represents a todo item with a title, completion status, and optional due date.
/// </summary>
/// <param name="Id">The unique identifier for the todo item.</param>
/// <param name="Title">The title or description of the todo item.</param>
/// <param name="IsCompleted">Whether the todo item has been completed.</param>
/// <param name="Priority">The priority level of the todo item.</param>
/// <param name="DueDate">Optional due date for the todo item.</param>
/// <param name="CreatedAt">When the todo item was created.</param>
public sealed record TodoItem(
	int Id,
	string Title,
	bool IsCompleted,
	TodoPriority Priority,
	DateOnly? DueDate,
	DateTime CreatedAt);

/// <summary>
///     Request model for creating a new todo item.
/// </summary>
/// <param name="Title">The title of the todo to create.</param>
/// <param name="Priority">The priority level. Defaults to <see cref="TodoPriority.Medium" />.</param>
/// <param name="DueDate">Optional due date.</param>
public sealed record CreateTodoRequest(
	string Title,
	TodoPriority Priority = TodoPriority.Medium,
	DateOnly? DueDate = null);

/// <summary>
///     Request model for updating an existing todo item.
/// </summary>
/// <param name="Title">The updated title, or <c>null</c> to keep the current title.</param>
/// <param name="IsCompleted">The updated completion status, or <c>null</c> to keep current.</param>
/// <param name="Priority">The updated priority, or <c>null</c> to keep current.</param>
/// <param name="DueDate">The updated due date, or <c>null</c> to keep current.</param>
public sealed record UpdateTodoRequest(
	string? Title = null,
	bool? IsCompleted = null,
	TodoPriority? Priority = null,
	DateOnly? DueDate = null);

/// <summary>
///     Defines the priority levels for a todo item.
/// </summary>
public enum TodoPriority
{
	/// <summary>Low priority — can be done whenever.</summary>
	Low,

	/// <summary>Medium priority — should be done soon.</summary>
	Medium,

	/// <summary>High priority — needs attention.</summary>
	High,

	/// <summary>Critical priority — must be done immediately.</summary>
	Critical
}
