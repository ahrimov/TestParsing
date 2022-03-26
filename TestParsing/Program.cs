using System;
using AngleSharp;
using AngleSharp.Dom;
using System.Threading.Tasks;
using System.IO;
using AngleSharp.Html.Dom;

namespace TestParsing
{
    class Program 
    {
        private static async Task<bool> parse(string url, string first_page, string city_code = null)
        {
            var config = Configuration.Default.WithDefaultLoader();
            using var context = BrowsingContext.New(config);
            var doc = await context.OpenAsync(url + first_page);

            if (city_code != null)
            {
                var div = doc.QuerySelector("div[id='region']");
                var form = div.QuerySelector<IHtmlFormElement>("form");
                doc = await form.SubmitAsync(new { city = city_code });
            }

            string link_next_page = first_page;
            string info;
            while (link_next_page != null)
            {
                info = await getInfo(doc, context, url, city_code);
                writeInfo(info, "result.csv");
                link_next_page = findNextPage(doc.QuerySelectorAll("li[class='page-item']"));
                doc = await context.OpenAsync(url + link_next_page);

                Console.Write("."); // loading bar
            }
            Console.WriteLine("\n");
            return true;
        }
        
        static async Task<string> getInfo(IDocument doc, IBrowsingContext context, string url, string city_code = null)
        {
            var links = doc.QuerySelectorAll("a[class='d-block img-link text-center gtm-click']");
            string info = "";

            foreach (var link in links)
            {
                var val = link.Attributes["href"].Value;
                string path = url + val;
                if (city_code != null)
                    path += "?city=" + city_code;
                using var page = await context.OpenAsync(path);
                info += page.QuerySelector("a[data-src='#region']").TextContent.Trim();
                var breadcrumbs = page.QuerySelectorAll("a[class='breadcrumb-item']");
                foreach (var breadcrumb in breadcrumbs)
                {
                    info += breadcrumb.QuerySelector("span").TextContent.Trim();
                }
                info += ';';
                info += page.QuerySelector("h1[class='detail-name']").TextContent.Trim() + ';';
                info += page.QuerySelector("span[class='price']").TextContent.Trim() + ';';
                var old_price = page.QuerySelector("span[class='old-price']");
                if (old_price != null)
                {
                    info += old_price.TextContent.Trim() + ';';
                }
                else
                {
                    info += "None;";
                }
                info += page.QuerySelector("span[class='ok']").TextContent.Trim() + ';';
                var images = page.QuerySelectorAll("img[class='img-fluid']");
                foreach (var image in images)
                {
                    info += image.Attributes["src"].Value;
                }
                info += ';';
                info += path + ";\n";
            }
            return info;
        }
        
        static string findNextPage(IHtmlCollection<IElement> page_list)
        {
            foreach (var page in page_list)
            {
                if (page.TextContent.Trim() == "След.")
                {
                    return page.QuerySelector("a").Attributes["href"].Value;
                }
            }
            return null;
        }

        static async void writeInfo(string info, string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                await writer.WriteLineAsync(info);
            }
        }
        
        static void Main(string[] args)
        {
            var url = "https://www.toy.ru";
            string first_page = "/catalog/boy_transport/";
            string rostov_city_code = "61000001000";

            Console.WriteLine("Data collection in progress. Please wait");
            var m = parse(url, first_page);
            var r = parse(url, first_page, rostov_city_code);
            if (m.Result == true && r.Result == true)
            {
                Console.WriteLine("Data collect is done.");
            }
            else
            {
                Console.WriteLine("Something get wrong.");
            }
        }

    }
}

