using HtmlAgilityPack;
using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace PawnTakesPawn
{
    class Program
    {
        static void Main(string[] args)
        {
            using var client = new WebClient
            {
                Encoding = Encoding.UTF8
            };

            var contents = client.DownloadString("https://pawntakespawn.com/tv");

            var doc = new HtmlDocument();
            doc.LoadHtml(contents);

            foreach (var tape in doc.DocumentNode.SelectNodes("//a[contains(@class, 'tape-fade-in')]"))
            {
                var attribute = tape.GetAttributeValue("xlink:href", string.Empty);
                if (attribute == default)
                {
                    continue;
                }

                var match = Regex.Match(attribute, @"\(&#39;(.*)&#39;\)");

                if (match.Groups.Count != 2)
                {
                    continue;
                }

                var streamUrl = WebUtility.HtmlDecode(match.Groups[1].ToString());
            }
        }
    }
}
