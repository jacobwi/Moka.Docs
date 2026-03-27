// SampleApi — In-memory todo service

using SampleApi.Models;

namespace SampleApi.Services;

/// <summary>
///     In-memory service for managing todo items.
///     Provides CRUD operations with thread-safe access.
/// </summary>
public sealed class TodoService
{
	private readonly Lock _lock = new();
	private readonly List<TodoItem> _todos = [];
	private int _nextId = 1;

	/// <summary>
	///     Gets all todo items ordered by creation date.
	/// </summary>
	/// <returns>A list of all todo items.</returns>
	public List<TodoItem> GetAll()
	{
		lock (_lock)
		{
			return [.. _todos.OrderByDescending(t => t.CreatedAt)];
		}
	}

	/// <summary>
	///     Gets a todo item by its unique identifier.
	/// </summary>
	/// <param name="id">The todo item ID.</param>
	/// <returns>The todo item, or <c>null</c> if not found.</returns>
	public TodoItem? GetById(int id)
	{
		lock (_lock)
		{
			return _todos.FirstOrDefault(t => t.Id == id);
		}
	}

	/// <summary>
	///     Creates a new todo item from the given request.
	/// </summary>
	/// <param name="request">The creation request with title and priority.</param>
	/// <returns>The newly created todo item with an auto-generated ID.</returns>
	public TodoItem Create(CreateTodoRequest request)
	{
		lock (_lock)
		{
			var todo = new TodoItem(
				_nextId++,
				request.Title,
				false,
				request.Priority,
				request.DueDate,
				DateTime.UtcNow);
			_todos.Add(todo);
			return todo;
		}
	}

	/// <summary>
	///     Updates an existing todo item.
	/// </summary>
	/// <param name="id">The ID of the todo to update.</param>
	/// <param name="request">The fields to update (null fields are left unchanged).</param>
	/// <returns><c>true</c> if the item was found and updated; <c>false</c> otherwise.</returns>
	public bool Update(int id, UpdateTodoRequest request)
	{
		lock (_lock)
		{
			int index = _todos.FindIndex(t => t.Id == id);
			if (index < 0)
			{
				return false;
			}

			TodoItem existing = _todos[index];
			_todos[index] = existing with
			{
				Title = request.Title ?? existing.Title,
				IsCompleted = request.IsCompleted ?? existing.IsCompleted,
				Priority = request.Priority ?? existing.Priority,
				DueDate = request.DueDate ?? existing.DueDate
			};
			return true;
		}
	}

	/// <summary>
	///     Deletes a todo item by ID.
	/// </summary>
	/// <param name="id">The ID of the todo to delete.</param>
	/// <returns><c>true</c> if the item was found and deleted; <c>false</c> otherwise.</returns>
	public bool Delete(int id)
	{
		lock (_lock)
		{
			return _todos.RemoveAll(t => t.Id == id) > 0;
		}
	}
}
