using System.Security.Cryptography;
using System.Text;
using Markdig.Syntax;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Gives tab groups and code groups ids built from the page and their position in it, so the
///     same file renders the same HTML on every build and ids don't repeat across pages.
/// </summary>
internal static class GroupIds
{
	/// <summary>Renames every group in <paramref name="document" /> to <c>{kind}-{page key}-{n}</c>.</summary>
	internal static void ScopeToPage(MarkdownDocument document, string sourcePath)
	{
		string page = PageKey(sourcePath);
		int tabGroups = 0;
		int codeGroups = 0;
		foreach (Block block in document.AllDescendants())
		{
			switch (block)
			{
				case TabGroupBlock tabs:
					tabs.GroupId = $"tabs-{page}-{++tabGroups}";
					break;
				case CodeGroupBlock code:
					code.GroupId = $"codegroup-{page}-{++codeGroups}";
					break;
			}
		}
	}

	private static string PageKey(string sourcePath)
	{
		byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sourcePath.Replace('\\', '/')));
		return Convert.ToHexStringLower(hash, 0, 4);
	}
}
