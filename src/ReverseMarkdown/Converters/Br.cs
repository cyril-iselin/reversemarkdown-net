using System;
using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Br : ConverterBase
    {
        public Br(Converter converter) : base(converter)
        {
            Converter.Register("br", this);
        }

        public override string Convert(HtmlNode node)
        {
            if (Converter.Config.DisableLineBreaksWhenInsideFormattingTags)
            {
                var parentName = node.ParentNode.Name.ToLowerInvariant();
                var parentList = new[] { "strong", "b", "em", "i" };
                if (parentList.Contains(parentName))
                {
                    return string.Empty;
                }
            }
            return Converter.Config.GithubFlavored ? Environment.NewLine : $"  {Environment.NewLine}";
        }
    }
}
