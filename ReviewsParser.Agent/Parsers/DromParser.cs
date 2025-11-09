using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ReviewsParser.Agent.Parsers
{
    public class DromParser : IParser
    {
        public async IAsyncEnumerable<ParsedReview> ParseAsync(string? startIdentifier, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var scriptNodes = await GetScriptNodesAsync(cancellationToken);

            bool startPointFound = string.IsNullOrEmpty(startIdentifier);

            foreach (var scriptNode in scriptNodes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var reviewsFromNode = new List<ParsedReview>();

                try
                {
                    var json = JToken.Parse(scriptNode.InnerText);
                    var items = (json is JArray) ? json : new JArray { json };

                    foreach (var item in items)
                    {
                        if (item.Value<string>("@type") == "Review")
                        {
                            string url = item.SelectToken("url")?.ToString() ?? "";
                            if (string.IsNullOrEmpty(url)) continue; 

                            if (!startPointFound)
                            {
                                if (url == startIdentifier)
                                {
                                    startPointFound = true;
                                    Console.WriteLine($"  -> Точка возобновления найдена: {url}");
                                }
                                continue;
                            }
                            string brandName = item.SelectToken("itemReviewed.brand.name")?.ToString() ?? "";
                            string itemName = item.SelectToken("itemReviewed.name")?.ToString() ?? "";

                            reviewsFromNode.Add(new ParsedReview
                            {
                                Car = $"{brandName} {itemName.Replace(brandName, "").Trim()}".Trim(),
                                Author = item.SelectToken("author.name")?.ToString() ?? "Не указан",
                                Rating = item.SelectToken("reviewRating.ratingValue")?.ToString() ?? "Нет",
                                Url = url
                            });
                        }
                    }
                }
                catch { }

                foreach (var review in reviewsFromNode)
                {
                    await Task.Delay(200, cancellationToken);
                    yield return review;
                }
            }
        }
        private async Task<List<HtmlNode>> GetScriptNodesAsync(CancellationToken cancellationToken)
        {
            string url = "https://www.drom.ru/reviews/";
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var windows1251 = Encoding.GetEncoding("windows-1251");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0 Safari/537.36");
            var responseBytes = await httpClient.GetByteArrayAsync(url, cancellationToken);
            var html = windows1251.GetString(responseBytes);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            return htmlDoc.DocumentNode.SelectNodes("//script[@type='application/ld+json']")?.ToList() ?? new List<HtmlNode>();
        }
    }
}