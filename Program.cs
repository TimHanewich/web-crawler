using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crawler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Page p = new Page("https://google.com");
            p.CrawlAsync(1).Wait();

            Console.WriteLine(JsonConvert.SerializeObject(p, Formatting.Indented));
        }
    }

    public class Page
    {
        public string Url {get; set;}
        public Page[] Children {get; set;}
        public string[] MediaUrls {get; set;}
        
        public Page()
        {
            Url = string.Empty;
            Children = new Page[]{};
            MediaUrls = new string[]{};
        }

        public Page(string url)
        {
            Url = url;
            Children = new Page[]{};
            MediaUrls = new string[]{};
        }

        public async Task CrawlAsync(int depth = 0)
        {
            HttpClient hc = new HttpClient();
            HttpRequestMessage req = new HttpRequestMessage();
            req.Method = HttpMethod.Get;
            req.RequestUri = new Uri(Url);
            HttpResponseMessage resp = await hc.SendAsync(req);

            //Get content
            string content = await resp.Content.ReadAsStringAsync();
            
            //Get every href from this page
            string[] hrefs = ExtractHrefs(content);

            //For each href found, make a child webpage
            List<Page> MyChildren = new List<Page>();
            foreach (string href in hrefs)
            {
                Page p = new Page();


                //Add the URL - but if it is a partial URL, add my root
                if (href.ToLower().StartsWith("http"))
                {
                    p.Url = href;
                }
                else
                {
                    p.Url = CombineUrl(Url, href);
                }

                //If the depth is high enough, pass it along (recursively)
                if (depth > 0)
                {
                    try
                    {
                        await p.CrawlAsync(depth - 1);
                    }
                    catch
                    {

                    }
                }

                //Only add it if this is not me
                Uri cMine = new Uri(Url);
                Uri cThis = new Uri(p.Url);
                if (cMine.ToString() != cThis.ToString())
                {
                    MyChildren.Add(p);
                }
            }

            //Extract images
            MediaUrls = ExtractMediaUrls(content);

            //Append children
            Children = MyChildren.ToArray();
        }


        //TOOLKIT BELOW
        private string[] ExtractHrefs(string body)
        {
            List<string> ToReturn = new List<string>();
            string[] parts = body.Split(new string[]{"href="}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                for (int t = 1; t < parts.Length; t++)
                {
                    int loc1 = parts[t].IndexOf("\"");
                    int loc2 = parts[t].IndexOf("\"", loc1 + 1);
                    if (loc1 > -1 && loc2 > -1 && loc2 > loc1)
                    {
                        string href = parts[t].Substring(loc1 + 1, loc2 - loc1 - 1);
                        href = href.ToLower();
                        if (ToReturn.Contains(href) == false)
                        {
                            ToReturn.Add(href);
                        }
                    }
                }
            }
            return ToReturn.ToArray();
        }

        private string[] ExtractMediaUrls(string body)
        {
            //Construct a list of extensions
            List<string> extensions = new List<string>();
            extensions.Add("jpg");
            extensions.Add("png");
            extensions.Add("gif");
            extensions.Add("svg");
            extensions.Add("webp");
            extensions.Add("jpeg");
            extensions.Add("mp4");

            //data
            List<string> ToReturn = new List<string>();
            string bodyl = body.ToLower();
            foreach (string extension in extensions)
            {
                string[] parts = bodyl.Split(new string[]{"." + extension}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    for (int t = 0; t < parts.Length - 1; t++)
                    {
                        int loc1 = parts[t].LastIndexOf("\"");
                        int loc2 = parts[t].LastIndexOf("'");
                        int loc3 = parts[t].LastIndexOf("(");
                        int loc = Math.Max(Math.Max(loc1, loc2), loc3);
                        if (loc > -1)
                        {
                            string url = parts[t].Substring(loc + 1);
                            url = url + "." + extension; //tack on the extension
                            url = url.ToLower(); //lower
                            if (url.StartsWith("http") == false)
                            {
                                url = CombineUrl(Url, url);
                            }
                            ToReturn.Add(url);
                        }
                    }
                }
            }

            //Return
            return ToReturn.ToArray();
        }

        private string CombineUrl(string part1, string part2)
        {
            Uri b = new Uri(Cleanse(part1));
            Uri full = new Uri(b, Cleanse(part2));
            return full.ToString();
        }

        private string Cleanse(string before)
        {
            string ToReturn = before;
            ToReturn = ToReturn.Replace("\\", "");
            return ToReturn;
        }

    }
}