using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SBCrawler.Code
{
    class Processor
    {

        public Dictionary<string, string> ScannedPages = new Dictionary<string, string>();
        private List<CelebData> _celebListToSave = new List<CelebData>();
        private bool _isBreak = false;
        private string _url;
        private string _baseURL;
        private List<string> _processedURL = new List<string>();
        private List<string> _imageDataURL = new List<string>();
        private string _baseDirectory = @"D:\WallpaperDownload";

        public Processor(string url, string baseURL)
        {
            _url = url;
            _baseURL = baseURL;
        }

        public void CreateCelebList()
        {
            GetCelebList();
            ProcessHtml();
            ProcessCelebList();
            // SaveCelebInDatabase();
        }

        public void ProcessCelebList()
        {
            Directory.CreateDirectory(_baseDirectory);
            foreach (CelebData celebrity in _celebListToSave.Where(obj => obj.Name.Equals("Anushka Sharma", StringComparison.OrdinalIgnoreCase)))
            {
                GetCelebrityPage(celebrity);
                DownloadImageData(Path.Combine(_baseDirectory, celebrity.Name));
            }
        }

        private void DownloadImageData(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
            foreach (string imageLink in _imageDataURL)
            {
                var imageValidLink = GetValidImageLink(imageLink);
                if (!string.IsNullOrWhiteSpace(imageValidLink))
                    ProcessImageData(imageValidLink,directoryPath,_imageDataURL.IndexOf(imageLink));
            }
        }

        private string GetValidImageLink(string imageLink)
        {
            bool pageExists = false;
            for (int counter = 6; counter >= 0; counter--)
            {
                var imageNumber = "full" + (counter > 0 ? (counter).ToString() : string.Empty);
                var replaceMentText = "full" + (counter + 1);
                imageLink = imageLink.Contains(@"\full\") ? imageLink.Replace("full", imageNumber).Replace(replaceMentText,imageNumber) : imageLink.Replace("full1", imageNumber).Replace(replaceMentText, imageNumber);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(imageLink);
                {
                    request.Method = WebRequestMethods.Http.Head;
                    try
                    {

                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            pageExists = response.StatusCode == HttpStatusCode.OK;
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                if (pageExists) return imageLink;
            }
            return string.Empty;
        }

        private void ProcessImageData(string imageURL,string rootPath,int counter)
        {
            using (WebClient client = new WebClient())
            {
                byte[] imageData = client.DownloadData(imageURL);
                File.WriteAllBytes(Path.Combine(rootPath, counter.ToString() + ".jpg"), imageData);
            }
        }

        private void GetCelebrityPage(CelebData celebrity)
        {
            using (WebClient client = new WebClient())
            {
                var pageString = client.DownloadString(celebrity.PageURL);
                ProcessCelebrityPaggeing(pageString);
            }

        }

        private void ProcessCelebrityPaggeing(string pageString)
        {
            using (WebDocument document = new WebDocument(pageString))
            {
                ProcessCelebrityPage(document);
                if (document.DocumentNode.Descendants("ul").Any(ele => ele.Attributes.Contains("class") && ele.Attributes["class"].Value.Contains("tsc_pagination tsc_paginationA")))
                {
                    foreach (HtmlNode pageNumber in document.DocumentNode.Descendants("ul").Where(ele => ele.Attributes.Contains("class") && ele.Attributes["class"].Value.Contains("tsc_pagination tsc_paginationA")).FirstOrDefault().Descendants("li"))
                    {
                        var pagegedURL = pageNumber.FirstChild.Attributes.Contains("href") ? pageNumber.FirstChild.Attributes["href"].Value : string.Empty;
                        if ((!string.IsNullOrWhiteSpace(pagegedURL) && !pagegedURL.Equals("#")) && !_processedURL.Contains(_baseURL + pagegedURL) && !pagegedURL.Contains("page=1"))
                        {
                            using (WebClient client = new WebClient())
                            {
                                pageString = client.DownloadString(_baseURL + pagegedURL.Replace("&amp;", "&"));
                                _processedURL.Add(_baseURL + pagegedURL);
                                ProcessCelebrityPaggeing(pageString);
                            }
                        }
                    }
                }
            }
        }

        private void ProcessCelebrityPage(WebDocument document)
        {
            foreach (HtmlNode wrapperDivTag in document.DocumentNode.Descendants("div").Where(ele => ele.Attributes.Contains("class") && ele.Attributes["class"].Value.Contains("wallpapers-box-300x180-2-img")))
            {
                var pageString = string.Empty;
                using (WebClient client = new WebClient())
                {
                    pageString = client.DownloadString(_baseURL + wrapperDivTag.SelectSingleNode("a").Attributes["href"].Value);
                }
                using (WebDocument imageDocument = new WebDocument(pageString))
                {
                    _imageDataURL.Add(imageDocument.DocumentNode.Descendants("img").Where(ele => ele.Attributes.Contains("id") && ele.Attributes["id"].Value.Contains("wall")).FirstOrDefault().Attributes["src"].Value);
                }
            }
        }

        private void GetCelebList()
        {
            int counter = 1;
            while (!_isBreak)
            {
                GetHtmlFromURL();

                if (!ScannedPages[_url].Contains("wallpapers-box-300x180-2 wallpapers-margin-2"))
                {
                    ScannedPages.Remove(_url);
                    _isBreak = true;
                }

                counter++;
                _url = _url.Substring(0, _url.LastIndexOf("=") + 1);
                _url += counter;
            }
        }

        private void GetHtmlFromURL()
        {
            using (WebClient client = new WebClient())
            {
                ScannedPages.Add(_url, client.DownloadString(_url));
            }
        }

        private void ProcessHtml()
        {
            foreach (KeyValuePair<string, string> pageToProcess in ScannedPages)
            {
                using (WebDocument htmlDoc = new WebDocument())
                {
                    htmlDoc.LoadHtml(pageToProcess.Value);
                    var findClasses = htmlDoc.DocumentNode.Descendants("div").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("wallpapers-box-300x180-2 wallpapers-margin-2"));
                    GetDataFromHtml(findClasses, pageToProcess.Key);
                }
            }
        }

        private void GetDataFromHtml(IEnumerable<HtmlNode> findClasses, string menuURL)
        {
            foreach (HtmlNode foundDiv in findClasses)
            {
                CelebData dataToSave = new CelebData();
                foreach (HtmlNode anchor in foundDiv.Descendants("a"))
                {
                    if (anchor.LastChild.Name.Equals("img"))
                    {
                        dataToSave.ThumbURL = anchor.LastChild.Attributes["src"].Value.ToString();
                        dataToSave.Name = anchor.LastChild.Attributes["title"].Value.ToString();
                        dataToSave.PageURL = "http://www.santabanta.com" + anchor.Attributes["href"].Value.ToString();
                        dataToSave.MenuURL = menuURL;
                        break;
                    }
                }
                _celebListToSave.Add(dataToSave);
            }
        }

        private void SaveCelebInDatabase()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["WallApp"].ToString();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("SaveCelebDetails", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        //cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = txtFirstName.Text;
                        //cmd.Parameters.Add("@MenuURL", SqlDbType.VarChar).Value = txtLastName.Text;

                        connection.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {

                }
            }
        }

        class CelebData
        {
            public string Name { get; set; }
            public string ThumbURL { get; set; }
            public string PageURL { get; set; }
            public string MenuURL { get; set; }
        }
    }
}
