using Moka.Docs.Samples.AspNetCore.Models;

namespace Moka.Docs.Samples.AspNetCore.Services;

/// <summary>
///     Service for managing products in the catalog.
/// </summary>
/// <remarks>
///     This is an in-memory implementation for demonstration purposes.
///     In production, replace with a database-backed implementation.
/// </remarks>
public interface IProductService
{
	/// <summary>Returns all products in the catalog.</summary>
	IReadOnlyList<Product> GetAll();

	/// <summary>Finds a product by its unique identifier.</summary>
	/// <param name="id">The product ID.</param>
	/// <returns>The product, or <c>null</c> if not found.</returns>
	Product? GetById(int id);

	/// <summary>Creates a new product from the given request.</summary>
	/// <param name="request">The creation request.</param>
	/// <returns>The newly created product with an assigned ID.</returns>
	Product Create(CreateProductRequest request);

	/// <summary>Updates an existing product.</summary>
	/// <param name="id">The product ID to update.</param>
	/// <param name="request">The fields to update.</param>
	/// <returns><c>true</c> if the product was found and updated; <c>false</c> otherwise.</returns>
	bool Update(int id, UpdateProductRequest request);

	/// <summary>Deletes a product by ID.</summary>
	/// <param name="id">The product ID to delete.</param>
	/// <returns><c>true</c> if the product was found and deleted; <c>false</c> otherwise.</returns>
	bool Delete(int id);
}

/// <inheritdoc />
public sealed class ProductService : IProductService
{
	private readonly List<Product> _products =
	[
		new()
		{
			Id = 1, Name = "Wireless Headphones", Price = 79.99m, Category = Category.Electronics, StockQuantity = 150,
			Tags = ["audio", "wireless"]
		},
		new()
		{
			Id = 2, Name = "C# in Depth", Price = 44.99m, Category = Category.Books, StockQuantity = 42,
			Tags = ["programming", "csharp"]
		},
		new()
		{
			Id = 3, Name = "Ergonomic Keyboard", Price = 129.99m, Category = Category.Electronics, StockQuantity = 0,
			Tags = ["keyboard", "ergonomic"]
		}
	];

	private int _nextId = 4;

	/// <inheritdoc />
	public IReadOnlyList<Product> GetAll() => _products.AsReadOnly();

	/// <inheritdoc />
	public Product? GetById(int id) => _products.FirstOrDefault(p => p.Id == id);

	/// <inheritdoc />
	public Product Create(CreateProductRequest request)
	{
		var product = new Product
		{
			Id = _nextId++,
			Name = request.Name,
			Price = request.Price,
			Category = request.Category,
			StockQuantity = request.StockQuantity
		};
		_products.Add(product);
		return product;
	}

	/// <inheritdoc />
	public bool Update(int id, UpdateProductRequest request)
	{
		Product? product = GetById(id);
		if (product is null)
		{
			return false;
		}

		if (request.Name is not null)
		{
			product.Name = request.Name;
		}

		if (request.Price.HasValue)
		{
			product.Price = request.Price.Value;
		}

		if (request.StockQuantity.HasValue)
		{
			product.StockQuantity = request.StockQuantity.Value;
		}

		return true;
	}

	/// <inheritdoc />
	public bool Delete(int id)
	{
		Product? product = GetById(id);
		if (product is null)
		{
			return false;
		}

		_products.Remove(product);
		return true;
	}
}
