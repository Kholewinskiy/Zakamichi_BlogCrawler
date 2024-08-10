using HtmlAgilityPack;
using System.Text.Json;
using static Zakamichi_BlogCrawler.Global;
using Zakamichi_BlogCrawler.Model;

namespace Zakamichi_BlogCrawler.Zakamichi
{
    public class Hinatazaka
    {
        private static Dictionary<string, Blog> Blogs = [];
        private static readonly List<Blog> newBlogs = [];

        public static void Hinatazaka46_Crawler()
        {
            Blogs = LoadExistingBlogs(Hinatazaka46_BlogStatus_FilePath);
            int threadCount = Environment.ProcessorCount;

            List<Thread> articleThreads = Enumerable.Range(0, threadCount)
                .Select(threadId => new Thread(() => ProcessPages(threadId, threadCount)))
                .ToList();

            StartAndJoinThreads(articleThreads);

            if (newBlogs.Count > 0)
            {
                //SaveNewBlogs(threadCount);
                SaveAllBlogImages(newBlogs, Hinatazaka46_Images_FilePath, string.Empty);
                SaveBlogsToFile(Blogs, IdolGroup.Hinatazaka46.ToString(), Hinatazaka46_BlogStatus_FilePath);
            }
            Blogs.Clear();
            newBlogs.Clear();
        }
        private static void ProcessPages(int threadId, int threadCount)
        {
            for (int currentPage = threadId; currentPage <= 1000; currentPage += threadCount)
            {
                try
                {
                    Console.WriteLine($"Processing Page {currentPage}");
                    string url = $"{Hinatazaka46_HomePage}/s/official/diary/member/list?page={currentPage}";

                    HtmlDocument htmlDocument = GetHtmlDocument(url);
                    HtmlNodeCollection htmlNodeCollection = htmlDocument?.DocumentNode.SelectNodes("//div[@class='p-blog-group']/div[@class='p-blog-article']");
                    if (htmlNodeCollection != null)
                    {
                        foreach (HtmlNode element in htmlNodeCollection)
                        {
                            if (!ProcessBlog(element, currentPage))
                                return;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Not found in Page {currentPage}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error on Page {currentPage}: {ex.Message}");
                    break;
                }
            }
        }
        private static bool ProcessBlog(HtmlNode element, int currentPage)
        {
            DateTime start = DateTime.Now;
            string blogPath = $"{Hinatazaka46_HomePage}{element.Descendants("a").FirstOrDefault(n => n.HasClass("c-button-blog-detail"))?.GetAttributeValue("href", "/00000")}";
            string blogMemberName = GetElementInnerText(element, "div", "c-blog-article__name").Replace(" ", "");
            string blogID = GetBlogID(new Uri(blogPath).LocalPath);

            if (!Blogs.ContainsKey(blogID))
            {
                string blogTitle = GetElementInnerText(element, "div", "c-blog-article__title");
                string blogDateTime = GetElementInnerText(element, "div", "c-blog-article__date");
                HtmlNode blogInnerTextNode = element.SelectSingleNode(".//div[@class='c-blog-article__text']");
                List<string> imageList = blogInnerTextNode.Descendants("img")
                    .Select(e => e.GetAttributeValue("src", null))
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                if (imageList.Count > 20)
                {
                    Console.WriteLine("Warning! Too many images.");
                    blogInnerTextNode = GetHtmlDocument(blogPath).DocumentNode.SelectSingleNode("//div[@class='c-blog-article__text']");
                    imageList = blogInnerTextNode.Descendants("img")
                        .Select(e => e.GetAttributeValue("src", null))
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }

                Blog newBlog = new()
                {
                    ID = blogID,
                    Name = blogMemberName,
                    Title = blogTitle,
                    DateTime = ParseDateTime(blogDateTime, DateFormats[0], japanTime: true),
                    ImageList = imageList
                };

                Blogs[blogID] = newBlog;
                newBlogs.Add(newBlog);

                TimeSpan diff = DateTime.Now - start;
                Console.WriteLine($"Blog ID: [{blogID}][{blogMemberName}] Image Count: [{imageList.Count}] Total processing time: [{diff:h\\:mm\\:ss\\.fff}]");

                return true;
            }
            else
            {
                Console.WriteLine($"Duplicate Blog Id {blogID} for Member {blogMemberName} found on Page {currentPage}");
                return false;
            }
        }

        //private static void SaveNewBlogs(int threadCount)
        //{
        //    int blogsPerThread = Math.Max(newBlogs.Count / threadCount, newBlogs.Count % threadCount);
        //    List<Thread> mainThreads = Enumerable.Range(0, threadCount)
        //        .Select(i => new Thread(() =>
        //        {
        //            var threadBlogs = newBlogs.Skip(i * blogsPerThread).Take(blogsPerThread).ToList();
        //            if (threadBlogs.Count != 0)
        //                SaveBlogAllImage(threadBlogs, Hinatazaka46_Images_FilePath, string.Empty);
        //        }))
        //        .ToList();

        //    StartAndJoinThreads(mainThreads);
        //}



    }
}