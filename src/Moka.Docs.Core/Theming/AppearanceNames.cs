namespace Moka.Docs.Core.Theming;

internal static class AppearanceNames
{
	public static T? Find<T>(IReadOnlyList<T> items, Func<T, string> nameOf, string? name) where T : class
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}

		string trimmed = name.Trim();
		foreach (T item in items)
		{
			if (nameOf(item).Equals(trimmed, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
		}

		return null;
	}
}
