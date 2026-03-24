namespace Moka.Docs.Samples.AspNetCore.Models;

/// <summary>
///     Represents a product in the catalog.
/// </summary>
/// <remarks>
///     Products are the core entity in the system. Each product belongs to a
///     <see cref="Category" /> and tracks inventory via <see cref="StockQuantity" />.
/// </remarks>
public sealed class Product
{
    /// <summary>Unique product identifier.</summary>
    public int Id { get; set; }

    /// <summary>Product display name.</summary>
    /// <example>Wireless Bluetooth Headphones</example>
    public required string Name { get; set; }

    /// <summary>Detailed product description.</summary>
    public string Description { get; set; } = "";

    /// <summary>Price in USD.</summary>
    /// <example>49.99</example>
    public decimal Price { get; set; }

    /// <summary>Product category.</summary>
    public Category Category { get; set; } = Category.General;

    /// <summary>Number of units currently in stock.</summary>
    public int StockQuantity { get; set; }

    /// <summary>Whether the product is available for purchase.</summary>
    public bool IsAvailable => StockQuantity > 0;

    /// <summary>Date the product was added to the catalog.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Optional tags for search and filtering.</summary>
    public List<string> Tags { get; set; } = [];
}

/// <summary>
///     Product category classification.
/// </summary>
public enum Category
{
    /// <summary>General uncategorized products.</summary>
    General,

    /// <summary>Electronic devices and accessories.</summary>
    Electronics,

    /// <summary>Clothing and apparel.</summary>
    Clothing,

    /// <summary>Books and publications.</summary>
    Books,

    /// <summary>Food and beverages.</summary>
    Food,

    /// <summary>Home and garden products.</summary>
    Home
}

/// <summary>
///     Request model for creating a new product.
/// </summary>
/// <param name="Name">Product display name.</param>
/// <param name="Price">Price in USD. Must be greater than zero.</param>
/// <param name="Category">Product category.</param>
/// <param name="StockQuantity">Initial stock quantity.</param>
public sealed record CreateProductRequest(
    string Name,
    decimal Price,
    Category Category = Category.General,
    int StockQuantity = 0);

/// <summary>
///     Request model for updating an existing product.
/// </summary>
/// <param name="Name">Updated product name.</param>
/// <param name="Price">Updated price in USD.</param>
/// <param name="StockQuantity">Updated stock quantity.</param>
public sealed record UpdateProductRequest(
    string? Name = null,
    decimal? Price = null,
    int? StockQuantity = null);