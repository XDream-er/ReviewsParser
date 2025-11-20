using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ReviewsParser.Agent.Parsers
{
    public class DromParser : IParser
    {
        public async IAsyncEnumerable<ParsedReview> ParseAsync(ParsingTask task, ApiClient apiClient, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var scriptNodes = await GetMainPageScriptNodesAsync(task, cancellationToken);
            string? startIdentifier = task.ProgressIdentifier;
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

                            string fullText = "Отзыв не найден";
                            try
                            {
                                await Task.Delay(300, cancellationToken);
                                fullText = await GetReviewBodyFromPage(url, task, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка получения отзыва: {ex.Message}");
                            }

                            reviewsFromNode.Add(new ParsedReview
                            {
                                Car = $"{brandName} {itemName.Replace(brandName, "").Trim()}".Trim(),
                                Author = item.SelectToken("author.name")?.ToString() ?? "Не указан",
                                Rating = item.SelectToken("reviewRating.ratingValue")?.ToString() ?? "Нет",
                                ReviewText = fullText,
                                Url = url
                            });
                        }
                    }
                }
                catch { }

                foreach (var review in reviewsFromNode)
                {
                    yield return review;
                }
            }
        }

        private HttpClient CreateHttpClient(ParsingTask task)
        {
            var handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(task.ProxyAddress))
            {
                var proxy = new WebProxy(task.ProxyAddress);
                if (!string.IsNullOrEmpty(task.ProxyUsername))
                {
                    proxy.Credentials = new NetworkCredential(task.ProxyUsername, task.ProxyPassword);
                }
                handler.Proxy = proxy;
                handler.UseProxy = true;
            }
            else
            {
                handler.UseProxy = false;
            }
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0 Safari/537.36");
            return client;
        }

        private async Task<List<HtmlNode>> GetMainPageScriptNodesAsync(ParsingTask task, CancellationToken cancellationToken)
        {
            string url = "https://www.drom.ru/reviews/";
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var windows1251 = Encoding.GetEncoding("windows-1251");

            using var httpClient = CreateHttpClient(task);

            if (!string.IsNullOrEmpty(task.ProxyAddress))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"Используется прокси: {task.ProxyAddress}");
                Console.ResetColor();
            }

            var responseBytes = await httpClient.GetByteArrayAsync(url, cancellationToken);
            var html = windows1251.GetString(responseBytes);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            return htmlDoc.DocumentNode.SelectNodes("//script[@type='application/ld+json']")?.ToList() ?? new List<HtmlNode>();
        }
        private async Task<string> GetReviewBodyFromPage(string url, ParsingTask task, CancellationToken cancellationToken)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var windows1251 = Encoding.GetEncoding("windows-1251");

            using var httpClient = CreateHttpClient(task);

            var responseBytes = await httpClient.GetByteArrayAsync(url, cancellationToken);
            var html = windows1251.GetString(responseBytes);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var textNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@itemprop='reviewBody']");

            if (textNode != null)
            {
                string text = textNode.InnerHtml;
                text = text.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");
                text = Regex.Replace(text, "<.*?>", "");
                text = WebUtility.HtmlDecode(text);
                text = Regex.Replace(text, @"\s*\n\s*", "\n");
                return text.Trim();
            }
            return "Отзыв не найден на странице";
        }
    }
}