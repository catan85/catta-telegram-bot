using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RestSharp;

public class OmdbApi
{
    const string omdbBaseAddress = "http://www.omdbapi.com";
    const string omdbApiKey = "e5763849";

    public static List<string> SearchImdbIds(string title)
    {

        List<string> result = new List<string>();
        var imdbsearch = "imdb " + title;
        var client = new RestClient("https://www.google.co.in");
        var request = new RestRequest("search", Method.GET);
        request.AddQueryParameter("q", String.Join("+", imdbsearch.Split(" ")));

        var queryResult = client.Execute(request);

        
        HtmlAgilityPack.HtmlDocument html = new HtmlAgilityPack.HtmlDocument();
        html.OptionOutputAsXml = true;
        html.LoadHtml(queryResult.Content);
        HtmlNode doc = html.DocumentNode;

        foreach (HtmlNode link in doc.SelectNodes("//a[@href]"))
        {
            //HtmlAttribute att = link.Attributes["href"];
            string hrefValue = link.GetAttributeValue("href", string.Empty);
            if (!hrefValue.ToString().ToUpper().Contains("GOOGLE") && hrefValue.ToString().Contains("/url?q=") && hrefValue.ToString().ToUpper().Contains("HTTPS://"))
            {
                int index = hrefValue.IndexOf("&");
                if (index > 0)
                {
                    hrefValue = hrefValue.Substring(0, index);
                    string foundLink = hrefValue.Replace("/url?q=", "");
                    
                    if (foundLink.Contains("https://www.imdb.com/title/tt"))
                    {
                       result.Add(foundLink.Substring(27, 9));
                    }

                }
            }
        }
        return result;
    }

    public static void SearchFilm(string title)
    {
        var client = new RestClient(omdbBaseAddress);
        var request = new RestRequest("", Method.GET);
        request.AddQueryParameter("apiKey", omdbApiKey);
        request.AddQueryParameter("s", String.Join("+",title.Split(" ")));
        var queryResult = client.Execute(request);

        Console.WriteLine("...");
    }

    public static JObject GetFilmData(string imdbId)
    {
        var client = new RestClient(omdbBaseAddress);
        var request = new RestRequest("", Method.GET);
        request.AddQueryParameter("apiKey", omdbApiKey);
        request.AddQueryParameter("i", imdbId);
        var queryResult = client.Execute(request);
        
        return JObject.Parse(queryResult.Content);
    }

}
