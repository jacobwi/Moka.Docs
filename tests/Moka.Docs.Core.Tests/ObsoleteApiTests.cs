using System.Reflection;
using FluentAssertions;
using Moka.Docs.Core.Content;

namespace Moka.Docs.Core.Tests;

/// <summary>
///     Public members that MokaDocs doesn't use stay in the shipped packages until 2.0, marked
///     obsolete so callers hear about it first.
/// </summary>
public sealed class ObsoleteApiTests
{
	public static TheoryData<string> UnusedMembers() =>
	[
		"Moka.Docs.Core.Content.DocSite",
		"Moka.Docs.Core.Theming.ThemeManifest",
		"Moka.Docs.Core.Configuration.SearchFeatureConfig.Provider",
		"Moka.Docs.Core.MokaDefaults.SearchProvider"
	];

	[Theory]
	[MemberData(nameof(UnusedMembers))]
	public void UnusedMember_IsObsoleteUntil2_0(string name)
	{
		Assembly core = typeof(DocPage).Assembly;
		MemberInfo? member = core.GetType(name);
		if (member is null)
		{
			int dot = name.LastIndexOf('.');
			member = core.GetType(name[..dot])?.GetProperty(name[(dot + 1)..]);
		}

		member.Should().NotBeNull();
		ObsoleteAttribute? obsolete = member!.GetCustomAttribute<ObsoleteAttribute>();
		obsolete.Should().NotBeNull("{0} is public in a shipped package and unused", name);
		obsolete!.Message.Should().Contain("removed in 2.0");
		obsolete.IsError.Should().BeFalse("callers keep compiling until 2.0");
	}
}
