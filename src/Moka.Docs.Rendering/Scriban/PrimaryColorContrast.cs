using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Moka.Docs.Rendering.Scriban;

// The default style's accent text shades for a custom primaryColor, worked out like the color presets'
// --color-primary-text: the OKLCH lightness moves (darker in light mode, lighter in dark mode) just far
// enough for 4.5:1 on every background accent text sits on. The presets' values in the stylesheet come
// from this code, and a test keeps the two in step.
internal static partial class PrimaryColorContrast
{
	private const double _minimumContrast = 4.5;
	private const double _lightnessStep = 0.001;

	private static readonly double[] _lightPage = Hex("#ffffff");
	private static readonly double[] _lightCode = Hex("#f1f5f9");
	private static readonly double[] _darkPage = Hex("#0f172a");
	private static readonly double[] _darkCode = Hex("#1e293b");

	// Admonition boxes are 8% of their color over the page, and links inside them use the accent.
	private static readonly double[][] _admonitionColors =
		[Hex("#3b82f6"), Hex("#22c55e"), Hex("#f59e0b"), Hex("#ef4444"), Hex("#8b5cf6")];

	private static readonly ConcurrentDictionary<string, PrimaryTextColors?> _cache = new(StringComparer.Ordinal);

	public static PrimaryTextColors? For(string? color) =>
		string.IsNullOrWhiteSpace(color) ? null : _cache.GetOrAdd(color.Trim(), Compute);

	public static string TextColor(double[] primary, bool dark)
	{
		double[] page = dark ? _darkPage : _lightPage;
		List<double[]> backgrounds = [dark ? _darkCode : _lightCode, Mix(primary, page, 0.08)];
		backgrounds.AddRange(_admonitionColors.Select(admonition => Mix(admonition, page, 0.08)));

		string original = ToHex(primary);
		if (Passes(Hex(original), backgrounds))
		{
			return original;
		}

		(double lightness, double chroma, double hue) = ToOklch(primary);
		for (int step = 1; step <= 1000; step++)
		{
			double l = lightness + (dark ? step : -step) * _lightnessStep;
			if (l is <= 0 or >= 1)
			{
				break;
			}

			string candidate = ToHex(FromOklch(l, chroma, hue));
			if (Passes(Hex(candidate), backgrounds))
			{
				return candidate;
			}
		}

		return dark ? "#ffffff" : "#000000";
	}

	public static double Contrast(double[] a, double[] b)
	{
		double la = Luminance(a);
		double lb = Luminance(b);
		return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
	}

	public static double[] Hex(string hex)
	{
		string digits = hex.TrimStart('#');
		if (digits.Length == 3)
		{
			digits = string.Concat(digits.Select(c => new string(c, 2)));
		}

		return
		[
			int.Parse(digits.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0,
			int.Parse(digits.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0,
			int.Parse(digits.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0
		];
	}

	// Hex, rgb(), hsl() and named colors. Anything else (oklch(), var(), a translucent color) gives
	// null, and the theme then uses the color for text as it is.
	public static double[]? Parse(string color)
	{
		string value = color.Trim().ToLowerInvariant();
		if (HexPattern().Match(value) is { Success: true } hex)
		{
			string digits = hex.Groups["rgb"].Value;
			string alpha = hex.Groups["alpha"].Value;
			return alpha is "" or "f" or "ff" ? Hex(digits) : null;
		}

		if (FunctionPattern().Match(value) is { Success: true } function)
		{
			string[] parts = function.Groups["args"].Value
				.Split([',', ' ', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (parts.Length is not (3 or 4) || (parts.Length == 4 && Alpha(parts[3]) is not 1.0))
			{
				return null;
			}

			return function.Groups["name"].Value.StartsWith("rgb", StringComparison.Ordinal)
				? RgbChannels(parts)
				: HslChannels(parts);
		}

		if (!NamePattern().IsMatch(value))
		{
			return null;
		}

		// System.Drawing knows the CSS named colors but spells grey only as gray.
		Color named = Color.FromName(value.Replace("grey", "gray", StringComparison.Ordinal));
		return named.IsKnownColor && !named.IsSystemColor && named.A == 255
			? [named.R / 255.0, named.G / 255.0, named.B / 255.0]
			: null;
	}

	private static PrimaryTextColors? Compute(string color)
	{
		if (Parse(color) is not { } primary)
		{
			return null;
		}

		string light = TextColor(primary, false);
		string dark = TextColor(primary, true);
		return new PrimaryTextColors(
			light,
			ToHex(Mix(Hex(light), [0, 0, 0], 0.8)),
			dark,
			ToHex(Mix(Hex(dark), [1, 1, 1], 0.75)));
	}

	private static bool Passes(double[] text, List<double[]> backgrounds) =>
		backgrounds.All(background => Contrast(text, background) >= _minimumContrast);

	private static double[]? RgbChannels(string[] parts)
	{
		double[] channels = new double[3];
		for (int i = 0; i < 3; i++)
		{
			double? channel = parts[i].EndsWith('%') ? Number(parts[i][..^1]) / 100 : Number(parts[i]) / 255;
			if (channel is null)
			{
				return null;
			}

			channels[i] = Math.Clamp(channel.Value, 0, 1);
		}

		return channels;
	}

	private static double[]? HslChannels(string[] parts)
	{
		double? hue = Angle(parts[0]);
		double? saturation = Number(parts[1].TrimEnd('%'));
		double? lightness = Number(parts[2].TrimEnd('%'));
		if (hue is null || saturation is null || lightness is null)
		{
			return null;
		}

		double h = (hue.Value % 360 + 360) % 360;
		double s = Math.Clamp(saturation.Value / 100, 0, 1);
		double l = Math.Clamp(lightness.Value / 100, 0, 1);
		double a = s * Math.Min(l, 1 - l);

		double Channel(int n)
		{
			double k = (n + h / 30) % 12;
			return l - a * Math.Max(-1, Math.Min(Math.Min(k - 3, 9 - k), 1));
		}

		return [Channel(0), Channel(8), Channel(4)];
	}

	private static double? Angle(string value)
	{
		// grad before rad: "grad" also ends with "rad".
		(string Unit, double Degrees)[] units = [("deg", 1), ("grad", 0.9), ("rad", 180 / Math.PI), ("turn", 360)];
		foreach ((string unit, double degrees) in units)
		{
			if (value.EndsWith(unit, StringComparison.Ordinal))
			{
				return Number(value[..^unit.Length]) * degrees;
			}
		}

		return Number(value);
	}

	private static double? Alpha(string value) =>
		value.EndsWith('%') ? Number(value[..^1]) / 100 : Number(value);

	private static double? Number(string value) =>
		double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) ? number : null;

	private static double[] Mix(double[] color, double[] background, double amount) =>
	[
		color[0] * amount + background[0] * (1 - amount),
		color[1] * amount + background[1] * (1 - amount),
		color[2] * amount + background[2] * (1 - amount)
	];

	private static string ToHex(double[] rgb) =>
		"#" + string.Concat(rgb.Select(channel =>
			((int)Math.Round(Math.Clamp(channel, 0, 1) * 255, MidpointRounding.AwayFromZero)).ToString("x2", CultureInfo.InvariantCulture)));

	private static double Luminance(double[] rgb) =>
		0.2126 * ToLinear(rgb[0]) + 0.7152 * ToLinear(rgb[1]) + 0.0722 * ToLinear(rgb[2]);

	private static double ToLinear(double channel) =>
		channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

	private static double FromLinear(double channel) =>
		channel <= 0.0031308 ? channel * 12.92 : 1.055 * Math.Pow(channel, 1 / 2.4) - 0.055;

	private static (double Lightness, double Chroma, double Hue) ToOklch(double[] rgb)
	{
		double r = ToLinear(rgb[0]);
		double g = ToLinear(rgb[1]);
		double b = ToLinear(rgb[2]);
		double l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
		double m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
		double s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);
		double okA = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
		double okB = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;
		return (0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s, Math.Sqrt(okA * okA + okB * okB), Math.Atan2(okB, okA));
	}

	// Keeps lightness and hue. A point outside sRGB has its chroma cut to the edge of the gamut.
	private static double[] FromOklch(double lightness, double chroma, double hue)
	{
		double[] color = OklchToRgb(lightness, chroma, hue);
		if (InGamut(color))
		{
			return color;
		}

		double low = 0;
		double high = chroma;
		for (int i = 0; i < 40; i++)
		{
			double middle = (low + high) / 2;
			if (InGamut(OklchToRgb(lightness, middle, hue)))
			{
				low = middle;
			}
			else
			{
				high = middle;
			}
		}

		return OklchToRgb(lightness, low, hue);
	}

	private static bool InGamut(double[] rgb) => rgb.All(channel => channel is >= -1e-6 and <= 1 + 1e-6);

	private static double[] OklchToRgb(double lightness, double chroma, double hue)
	{
		double a = chroma * Math.Cos(hue);
		double b = chroma * Math.Sin(hue);
		double l = Math.Pow(lightness + 0.3963377774 * a + 0.2158037573 * b, 3);
		double m = Math.Pow(lightness - 0.1055613458 * a - 0.0638541728 * b, 3);
		double s = Math.Pow(lightness - 0.0894841775 * a - 1.2914855480 * b, 3);
		double[] linear =
		[
			4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
			-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
			-0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s
		];
		return linear.Select(channel => channel < 0 ? -FromLinear(-channel) : FromLinear(channel)).ToArray();
	}

	[GeneratedRegex("^#(?:(?<rgb>[0-9a-f]{3})(?<alpha>[0-9a-f])?|(?<rgb>[0-9a-f]{6})(?<alpha>[0-9a-f]{2})?)$")]
	private static partial Regex HexPattern();

	[GeneratedRegex(@"^(?<name>rgba?|hsla?)\((?<args>[^()]*)\)$")]
	private static partial Regex FunctionPattern();

	[GeneratedRegex("^[a-z]+$")]
	private static partial Regex NamePattern();
}

internal sealed record PrimaryTextColors(string Light, string LightHover, string Dark, string DarkHover);
