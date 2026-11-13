using System.Globalization;
using System.Reflection;
using Moka.Docs.Core.Theming;
using YamlDotNet.Serialization.NamingConventions;

namespace Moka.Docs.Core.Configuration;

/// <summary>
///     Applies a <see cref="ThemePresetInfo" /> to theme options. A preset's keys are paths under
///     <c>theme.options</c>, matched to properties by their mokadocs.yaml names, so an option added to
///     the configuration needs no code here.
/// </summary>
internal static class ThemePresetMerge
{
	private static readonly MethodInfo _memberwiseClone =
		typeof(object).GetMethod(nameof(MemberwiseClone), BindingFlags.Instance | BindingFlags.NonPublic)!;

	/// <summary>
	///     Sets each option of <paramref name="preset" /> that <paramref name="options" />, as read from
	///     mokadocs.yaml, leaves out (null). What the yaml sets wins.
	/// </summary>
	public static void FillMissing(ThemeOptionsDto options, ThemePresetInfo preset)
	{
		foreach ((string key, string value) in preset.Options)
		{
			FillMissing(options, key.Split('.'), 0, value);
		}
	}

	/// <summary>
	///     A copy of <paramref name="options" /> where each option of <paramref name="preset" /> that is
	///     still at its default value has the preset's value. For options built in code, where an option
	///     that was never set can't be told from one set to its default.
	/// </summary>
	public static ThemeOptions FillDefaults(ThemeOptions options, ThemePresetInfo preset)
	{
		var defaults = new ThemeOptions();
		foreach ((string key, string value) in preset.Options)
		{
			options = (ThemeOptions)FillDefault(options, defaults, key.Split('.'), 0, value);
		}

		return options;
	}

	/// <summary>
	///     Whether <paramref name="key" /> names an option under <c>theme.options</c> that a preset can
	///     set: a path of mokadocs.yaml names ending at a string, bool or int option.
	/// </summary>
	public static bool IsOption(string key)
	{
		Type type = typeof(ThemeOptionsDto);
		string[] path = key.Split('.');
		for (int i = 0; i < path.Length; i++)
		{
			if (FindProperty(type, path[i]) is not { } property)
			{
				return false;
			}

			type = property.PropertyType;
			if (i < path.Length - 1 && !IsSettingsType(type))
			{
				return false;
			}
		}

		return Nullable.GetUnderlyingType(type) is { } underlying ? underlying == typeof(bool) || underlying == typeof(int) : type == typeof(string);
	}

	private static void FillMissing(object target, string[] path, int index, string value)
	{
		if (FindProperty(target.GetType(), path[index]) is not { } property)
		{
			return;
		}

		if (index == path.Length - 1)
		{
			if (property.GetValue(target) is null && Convert(value, property.PropertyType) is { } converted)
			{
				property.SetValue(target, converted);
			}

			return;
		}

		if (!IsSettingsType(property.PropertyType))
		{
			return;
		}

		object? child = property.GetValue(target);
		if (child is null)
		{
			child = Activator.CreateInstance(property.PropertyType)!;
			property.SetValue(target, child);
		}

		FillMissing(child, path, index + 1, value);
	}

	// Records are copied before a property is set, so the options passed in stay as they were.
	private static object FillDefault(object target, object defaults, string[] path, int index, string value)
	{
		if (FindProperty(target.GetType(), path[index]) is not { CanWrite: true } property)
		{
			return target;
		}

		object? current = property.GetValue(target);
		object? replacement;
		if (index == path.Length - 1)
		{
			replacement = Equals(current, property.GetValue(defaults)) ? Convert(value, property.PropertyType) : null;
		}
		else
		{
			replacement = current is not null && property.GetValue(defaults) is { } childDefaults && IsSettingsType(property.PropertyType)
				? FillDefault(current, childDefaults, path, index + 1, value)
				: null;
		}

		if (replacement is null || Equals(replacement, current))
		{
			return target;
		}

		object copy = _memberwiseClone.Invoke(target, null)!;
		property.SetValue(copy, replacement);
		return copy;
	}

	private static PropertyInfo? FindProperty(Type type, string name) =>
		type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.FirstOrDefault(p => p.GetIndexParameters().Length == 0
			                     && CamelCaseNamingConvention.Instance.Apply(p.Name).Equals(name, StringComparison.Ordinal));

	private static bool IsSettingsType(Type type) =>
		type is { IsClass: true, Namespace: "Moka.Docs.Core.Configuration" } && type != typeof(string);

	private static object? Convert(string value, Type type)
	{
		Type target = Nullable.GetUnderlyingType(type) ?? type;
		if (target == typeof(string))
		{
			return value;
		}

		if (target == typeof(bool))
		{
			return bool.TryParse(value, out bool flag) ? flag : null;
		}

		if (target == typeof(int))
		{
			return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) ? number : null;
		}

		return null;
	}
}
