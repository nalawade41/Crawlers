using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alphacoders.Helpers;
using System.IO;
using System.Net;
using HtmlAgilityPack;

namespace Alphacoders
{
    class Program
    {
        private static List<string> _countryListToDownload = new List<string>();
        private static List<string> _processedURL = new List<string>();
        private static List<string> _imagePageURL = new List<string>();
        static void Main()
        {
            //E_countryListToDownload.Add("India");
            using (AlphacodersHelper alphaCoder = SetProcessingData())
            {
                ProcessSetdata(alphaCoder.DataToProcess, alphaCoder.RootFolderPath, alphaCoder.AlphacodersBaseURL);
            }
        }

        private static void ProcessSetdata(Dictionary<string, List<Tuple<string, string>>> DataToProcess, string rootPath, string baseURL)
        {
            Directory.CreateDirectory(rootPath);
            foreach (string country in DataToProcess.Keys)
            {
                //if (_countryListToDownload.Contains(country))
                    ProcessCountryData(Path.Combine(rootPath, country), DataToProcess[country], baseURL);
            }
        }

        private static void ProcessCountryData(string rootPath, List<Tuple<string, string>> countryDataToProcess, string baseURL)
        {
            Directory.CreateDirectory(rootPath);
            foreach (Tuple<string, string> actressDetails in countryDataToProcess)
            {
                ProcessActressData(Path.Combine(rootPath, actressDetails.Item1), actressDetails.Item2, baseURL);
            }
        }

        private static void ProcessActressData(string rootPath, string actressURL, string baseURL)
        {
            string[] actressImageData;
            Directory.CreateDirectory(rootPath);
            var pageString = string.Empty;
            using (WebClient client = new WebClient())
            {
                pageString = client.DownloadString(baseURL + actressURL);
                _processedURL.Add(baseURL + actressURL);
                ProcessActressDataPaging(pageString, baseURL);
            }
            actressImageData = new string[_imagePageURL.Count()];
            _imagePageURL.CopyTo(actressImageData);
            _imagePageURL.Clear();
            DownlaodImageAsync(actressImageData.ToList(), rootPath);
        }

        private static void DownlaodImageAsync(List<string> actressImageData, string rootPath)
        {
            try
            {
                int counter = 0;
                foreach (string image in actressImageData)
                {
                    using (WebClient client = new WebClient())
                    {
                        byte[] imageData = client.DownloadData(image);
                        File.WriteAllBytes(Path.Combine(rootPath, counter.ToString() + ".jpg"), imageData);
                    }
                    counter++;
                }
            }
            catch
            {

            }
        }

        private static void ProcessActressDataPaging(string pageString, string baseURL)
        {
            using (WebDocument documentProcess = new WebDocument())
            {
                documentProcess.LoadHtml(pageString);
                ProcessActressDataPage(documentProcess, baseURL);
                if (documentProcess.DocumentNode.Descendants("ul").Any(ele => ele.Attributes.Contains("class") && ele.Attributes["class"].Value.Contains("pagination")))
                {
                    foreach (HtmlNode listItem in documentProcess.DocumentNode.Descendants("ul").Where(ele => ele.Attributes.Contains("class") && ele.Attributes["class"].Value.Contains("pagination")).LastOrDefault().Descendants("li"))
                    {
                        var pagegedURL = listItem.FirstChild.Attributes.Contains("href") ? listItem.FirstChild.Attributes["href"].Value : string.Empty;
                        var isNextPage = listItem.FirstChild.Attributes.Contains("id") ? true : false;
                        if ((!string.IsNullOrWhiteSpace(pagegedURL) && !pagegedURL.Equals("#")) && !isNextPage && !_processedURL.Contains(baseURL + pagegedURL) && !pagegedURL.Contains("page=1"))
                        {
                            using (WebClient client = new WebClient())
                            {
                                pageString = client.DownloadString(baseURL + pagegedURL.Replace("&amp;", "&"));
                                _processedURL.Add(baseURL + pagegedURL);
                                ProcessActressDataPaging(pageString, baseURL);
                            }
                        }
                    }
                }
            }
        }

        private static void ProcessActressDataPage(WebDocument documentToProcess, string baseURL)
        {
            foreach (HtmlNode wrapperDivTag in documentToProcess.DocumentNode.Descendants("div").Where(ele => ele.Attributes.Contains("class") && ele.Attributes["class"].Value.Contains("thumb-container-big")))
            {
                if (!_imagePageURL.Contains(wrapperDivTag.Descendants("a").FirstOrDefault().Attributes["href"].Value))
                {
                    var pageString = string.Empty;
                    using (WebClient client = new WebClient())
                    {
                        pageString = client.DownloadString(baseURL + wrapperDivTag.Descendants("a").FirstOrDefault().Attributes["href"].Value);
                    }
                    using (WebDocument imageDocument = new WebDocument())
                    {
                        imageDocument.LoadHtml(pageString);
                        _imagePageURL.Add(imageDocument.DocumentNode.Descendants("span").Where(ele => ele.Attributes.Contains("class") && ele.Attributes["class"].Value.Contains("btn btn-success download-button")).FirstOrDefault().Attributes["data-href"].Value);
                    }
                }
            }
        }

        private static AlphacodersHelper SetProcessingData()
        {
            AlphacodersHelper alphaCoder = new AlphacodersHelper();
            alphaCoder.AlphacodersBaseURL = @"https://wall.alphacoders.com/";
            alphaCoder.CategoryBaseURL = @"show_group.php?collection_id=565";
            alphaCoder.DataToProcess = GetCountryURL(alphaCoder.AlphacodersBaseURL + alphaCoder.CategoryBaseURL);
            alphaCoder.RootFolderPath = @"D:\WallpaperDownloades\";
            return alphaCoder;
        }

        private static Dictionary<string, List<Tuple<string, string>>> GetCountryURL(string url)
        {
            Dictionary<string, List<Tuple<string, string>>> dictionaryToReturn = new Dictionary<string, List<Tuple<string, string>>>();
            var pageString = string.Empty;
            using (var client = new WebClient())
            {
                pageString = client.DownloadString(url);
            }
            ProcessBasicDataPage(dictionaryToReturn, pageString);
            return dictionaryToReturn;
        }

        private static void ProcessBasicDataPage(Dictionary<string, List<Tuple<string, string>>> dictionaryToReturn, string pageString)
        {
            using (WebDocument documentToProcess = new WebDocument())
            {
                var countryName = string.Empty;
                documentToProcess.LoadHtml(pageString);
                var listNodes = documentToProcess.DocumentNode.Descendants("div").Where(ele => ele.Attributes.Contains("class") && ele.Attributes["class"].Value.Contains("container_sub_categories")).FirstOrDefault();
                foreach (HtmlNode innerNode in listNodes.ChildNodes.Where(ele => !ele.Name.Equals("#text")))
                {
                    var anchorElement = innerNode.Descendants("a");
                    if (anchorElement != null)
                    {
                        if (anchorElement.Count() == 1)
                        {
                            if (anchorElement.FirstOrDefault().Attributes.Contains("class") && anchorElement.FirstOrDefault().Attributes["class"].Value.Contains("title-link"))
                                countryName = anchorElement.FirstOrDefault().InnerText;
                        }
                        else
                        {
                            List<Tuple<string, string>> actressList = new List<Tuple<string, string>>();
                            foreach (HtmlNode anchorNode in anchorElement)
                            {
                                Tuple<string, string> actressDetails = null;
                                if (anchorNode.Attributes.Contains("class") && anchorNode.Attributes["class"].Value.Contains("link-subcat"))
                                {
                                    actressDetails = new Tuple<string, string>(anchorNode.SelectSingleNode("p").InnerText, anchorNode.Attributes["href"].Value);
                                }
                                if (actressDetails != null)
                                {
                                    actressList.Add(actressDetails);
                                }
                            }
                            dictionaryToReturn.Add(countryName, actressList);
                        }
                    }
                }
            }
        }
    }
}
