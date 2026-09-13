namespace Moka.Docs.Samples.AspNetCore.Models;

/// <summary>
///     An amount of money in one currency.
/// </summary>
/// <param name="Amount">The amount.</param>
/// <param name="Currency">The ISO 4217 currency code, such as <c>USD</c>.</param>
public readonly record struct Money(decimal Amount, string Currency = "USD")
{
	/// <summary>Adds two amounts in the same currency.</summary>
	/// <param name="left">The first amount.</param>
	/// <param name="right">The second amount.</param>
	/// <exception cref="InvalidOperationException">The currencies differ.</exception>
	public static Money operator +(Money left, Money right) =>
		left.Currency == right.Currency
			? new Money(left.Amount + right.Amount, left.Currency)
			: throw new InvalidOperationException("Amounts in different currencies cannot be added.");

	/// <summary>The amount without its currency.</summary>
	/// <param name="money">The amount of money.</param>
	public static implicit operator decimal(Money money) => money.Amount;
}

/// <summary>
///     Decides the price a customer pays for a product.
/// </summary>
/// <remarks>
///     Derive from this class to add a pricing rule, such as a seasonal discount.
/// </remarks>
public abstract class PricingRule
{
	/// <summary>Creates a rule with the given name.</summary>
	/// <param name="name">A name for logs and receipts.</param>
	protected PricingRule(string name) => Name = name;

	/// <summary>The rule's name.</summary>
	public string Name { get; }

	/// <summary>Applies the rule to a price.</summary>
	/// <param name="product">The product being priced.</param>
	/// <param name="price">The price before this rule.</param>
	/// <returns>The price after this rule, and why it changed.</returns>
	public Adjustment Apply(Product product, Money price) =>
		new(Name, price, AppliesTo(product) ? Adjust(price) : price);

	/// <summary>Whether the rule applies to the product. It applies to every product unless overridden.</summary>
	/// <param name="product">The product being priced.</param>
	protected virtual bool AppliesTo(Product product) => true;

	/// <summary>Changes the price.</summary>
	/// <param name="price">The price before this rule.</param>
	/// <returns>The new price.</returns>
	protected abstract Money Adjust(Money price);

	/// <summary>A price change made by a rule, for receipts.</summary>
	/// <param name="RuleName">The rule that changed the price.</param>
	/// <param name="Before">The price before the rule.</param>
	/// <param name="After">The price after the rule.</param>
	public sealed record Adjustment(string RuleName, Money Before, Money After);
}

/// <summary>Called when a product's stock changes.</summary>
/// <param name="product">The product whose stock changed.</param>
/// <param name="previousQuantity">The stock before the change.</param>
public delegate void StockChangedHandler(Product product, int previousQuantity);
