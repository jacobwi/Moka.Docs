using System.Globalization;

namespace Moka.Docs.Engine.Tests.Theming;

// sRGB colors as channels from 0 to 1, for the theme contrast tests. Kept apart from the theme's own
// color code so a mistake there can't cancel itself out here.
internal static class ThemeColors
{
	public static readonly double[] White = [1, 1, 1];
	public static readonly double[] Black = [0, 0, 0];

	public static double[] Hex(string hex) =>
	[
		int.Parse(hex.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0,
		int.Parse(hex.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0,
		int.Parse(hex.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0
	];

	// color-mix(in srgb, top amount, bottom), the same as top at that opacity over bottom.
	public static double[] Mix(double[] top, double amount, double[] bottom) =>
		[top[0] * amount + bottom[0] * (1 - amount), top[1] * amount + bottom[1] * (1 - amount), top[2] * amount + bottom[2] * (1 - amount)];

	public static double Contrast(double[] a, double[] b)
	{
		static double Channel(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
		static double Luminance(double[] c) => 0.2126 * Channel(c[0]) + 0.7152 * Channel(c[1]) + 0.0722 * Channel(c[2]);
		double la = Luminance(a);
		double lb = Luminance(b);
		return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
	}

	// color-mix(in oklab, a amount, b), clipped to sRGB.
	public static double[] MixOklab(double[] a, double amount, double[] b)
	{
		double[] la = ToOklab(a);
		double[] lb = ToOklab(b);
		return FromOklab([la[0] * amount + lb[0] * (1 - amount), la[1] * amount + lb[1] * (1 - amount), la[2] * amount + lb[2] * (1 - amount)]);
	}

	public static double OklchHue(double[] rgb)
	{
		double[] lab = ToOklab(rgb);
		return (Math.Atan2(lab[2], lab[1]) * 180 / Math.PI + 360) % 360;
	}

	private static double[] ToOklab(double[] rgb)
	{
		static double Linear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
		double r = Linear(rgb[0]), g = Linear(rgb[1]), bl = Linear(rgb[2]);
		double l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * bl);
		double m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * bl);
		double s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * bl);
		return
		[
			0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
			1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
			0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s
		];
	}

	private static double[] FromOklab(double[] lab)
	{
		static double Gamma(double c) => Math.Clamp(c <= 0.0031308 ? c * 12.92 : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055, 0, 1);
		double l = Math.Pow(lab[0] + 0.3963377774 * lab[1] + 0.2158037573 * lab[2], 3);
		double m = Math.Pow(lab[0] - 0.1055613458 * lab[1] - 0.0638541728 * lab[2], 3);
		double s = Math.Pow(lab[0] - 0.0894841775 * lab[1] - 1.2914855480 * lab[2], 3);
		return
		[
			Gamma(4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s),
			Gamma(-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s),
			Gamma(-0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s)
		];
	}
}
