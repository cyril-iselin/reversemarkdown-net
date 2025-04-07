
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReverseMarkdown
{
    public static class Cleaner
    {
        private static readonly Regex SlackBoldCleaner = new Regex(@"\*(\s\*)+");
        private static readonly Regex SlackItalicCleaner = new Regex(@"_(\s_)+");
        private static readonly string[] RelevantParentForSplittingBr = { "strong", "em", "b", "i" };

        private static string CleanTagBorders(string content)
        {
            // content from some htl editors such as CKEditor emits newline and tab between tags, clean that up
            content = content.Replace("\n\t", "");
            content = content.Replace(Environment.NewLine + "\t", "");
            return content;
        }

        private static string NormalizeSpaceChars(string content)
        {
            // replace unicode and non-breaking spaces to normal space
            content = Regex.Replace(content, @"[\u0020\u00A0]", " ");
            return content;
        }

        public static string PreTidy(string content, bool normalizeSpaceChars, bool cleanupTagBorders)
        {
            if (normalizeSpaceChars)
            {
                content = NormalizeSpaceChars(content);
            }

            if (cleanupTagBorders)
            {
                content = CleanTagBorders(content);
            }

            return content;
        }

        public static string SlackTidy(string content)
        {
            // Slack's escaping rules depend on whether the key characters appear in
            // next to word characters or not.
            content = SlackBoldCleaner.Replace(content, "*");
            content = SlackItalicCleaner.Replace(content, "_");

            return content;
        }

        public static HtmlNode UnwrapTags(this HtmlNode html)
        {
            if (html == null)
            {
                return null;
            }

            SplitFormatTagsOnBr(html);
            ExtractBrFromFormatTags(html);

            return html;
        }

        private static void SplitFormatTagsOnBr(HtmlNode rootNode)
        {
            if (rootNode == null) return;

            foreach (var descendant in rootNode.Descendants().ToList())
            {
                if (string.IsNullOrWhiteSpace(descendant.Name) ||
                    descendant.Name == "#text" ||
                    !RelevantParentForSplittingBr.Contains(descendant.Name) ||
                    descendant.ChildNodes.All(n => n.Name != "br"))
                {
                    continue;
                }

                var parent = descendant.ParentNode;
                if (parent == null) continue;

                var newNodes = new List<HtmlNode>();
                var buffer = new List<HtmlNode>();

                foreach (var child in descendant.ChildNodes.ToList())
                {
                    if (child.Name == "br")
                    {
                        if (buffer.Any())
                        {
                            var newTag = HtmlNode.CreateNode($"<{descendant.Name}></{descendant.Name}>");
                            foreach (var b in buffer) newTag.AppendChild(b);
                            newNodes.Add(newTag);
                            buffer.Clear();
                        }

                        newNodes.Add(child); // br separat
                    }
                    else
                    {
                        buffer.Add(child);
                    }
                }

                if (buffer.Any())
                {
                    var newTag = HtmlNode.CreateNode($"<{descendant.Name}></{descendant.Name}>");
                    foreach (var b in buffer) newTag.AppendChild(b);
                    newNodes.Add(newTag);
                }

                var insertAfter = descendant.NextSibling;
                descendant.Remove();
                foreach (var n in newNodes)
                {
                    parent.InsertBefore(n, insertAfter);
                }
            }
        }

        private static void ExtractBrFromFormatTags(HtmlNode rootNode)
        {
            if (rootNode == null) return;

            var brs = rootNode.Descendants("br").ToList();

            foreach (var br in brs)
            {
                var parent = br.ParentNode;

                while (parent != null && RelevantParentForSplittingBr.Contains(parent.Name))
                {
                    var grand = parent.ParentNode;
                    br.Remove();
                    grand?.InsertAfter(br, parent);
                    parent = br.ParentNode;
                }
            }
        }
    }
}
