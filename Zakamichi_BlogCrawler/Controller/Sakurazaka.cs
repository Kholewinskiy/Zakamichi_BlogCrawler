using HtmlAgilityPack;
using static Zakamichi_BlogCrawler.Global;
using Zakamichi_BlogCrawler.Model;

namespace Zakamichi_BlogCrawler.Zakamichi
{
    class Sakurazaka
    {
        private static Dictionary<string, Blog> Blogs = [];
        private static readonly List<Blog> newBlogs = [];
        public static void Sakurazaka46_Crawler()
        {
            Blogs = LoadExistingBlogs(Sakurazaka46_BlogStatus_FilePath);
            int threadCount = Environment.ProcessorCount;

            List<Thread> articleThreads = Enumerable.Range(0, threadCount)
                                           .Select(threadId => new Thread(() => ProcessPages(threadId, threadCount)))
                                           .ToList();

            StartAndJoinThreads(articleThreads);

            if (newBlogs.Count > 0)
            {
                SaveAllBlogImages(newBlogs, Sakurazaka46_Images_FilePath, Sakurazaka46_HomePage);
                SaveBlogsToFile(Blogs, IdolGroup.Sakurazaka46.ToString(), Sakurazaka46_BlogStatus_FilePath);
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
                    string url = $"{Sakurazaka46_HomePage}/s/s46/diary/blog/list?ima=0000&page={currentPage}";
                    var htmlDocument = GetHtmlDocument(url);

                    HtmlNodeCollection htmlNodeCollection = htmlDocument?.DocumentNode.SelectNodes("//li[@class='box']");
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
            string blogPath = $"{Sakurazaka46_HomePage}{element.Descendants("a").First().Attributes["href"].Value}";
            string blogMemberName = GetElementInnerText(element, "p", "class", "name").Trim().Replace(" ", "");
            string blogID = GetBlogID(new Uri(blogPath).LocalPath);

            if (!Blogs.ContainsKey(blogID))
            {
                var articleCollection = GetHtmlDocument(blogPath)?.DocumentNode.SelectNodes("//div[@class='box-article']");
                if (articleCollection != null)
                {
                    var imageList = articleCollection.First().Descendants("img")
                                                     .Select(e => e.GetAttributeValue("src", null))
                                                     .Where(s => !string.IsNullOrEmpty(s))
                                                     .ToList();
                    string blogDateTime = GetElementInnerText(element, "p", "class", "date wf-a");
                    string blogTitle = GetElementInnerText(element, "h3", "class", "title");

                    Blog newBlog = new()
                    {
                        ID = blogID,
                        Name = blogMemberName,
                        Title = blogTitle,
                        DateTime = ParseDateTime(blogDateTime, DateFormats[1]),
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
                    Console.WriteLine($"Not found on Blog Id {blogID} for Member {blogMemberName}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"Duplicate Blog Id {blogID} for Member {blogMemberName} found on Page {currentPage}");
                return false;
            }
        }

        //private static void SaveNewBlogs(int threadCount, List<Blog> newBlogs)
        //{


        //    int blogsPerThread = Math.Max(newBlogs.Count / threadCount, newBlogs.Count % threadCount);

        //    List<Thread> mainThreads = Enumerable.Range(0, threadCount).Select(i => new Thread(() =>
        //    {
        //        List<Blog> threadBlogs = newBlogs.Skip(i * blogsPerThread).Take(blogsPerThread).ToList();
        //        if (threadBlogs.Count != 0)
        //        {
        //            SaveBlogAllImage(threadBlogs, Sakurazaka46_Images_FilePath, string.Empty);
        //        }
        //    })).ToList();

        //    StartAndJoinThreads(mainThreads);
        //}



    }
}