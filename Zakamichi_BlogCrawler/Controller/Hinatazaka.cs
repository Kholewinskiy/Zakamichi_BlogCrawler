using HtmlAgilityPack;
using System.Text.Json;
using static Zakamichi_BlogCrawler.Global;
using Zakamichi_BlogCrawler.Model;

namespace Zakamichi_BlogCrawler.Zakamichi
{
    public class Hinatazaka
    {
        static List<Blog> Hinatazaka46_Blogs = [];

        public static Thread EnableThread(int threadId, int threadCount)
        {
            return new Thread(() =>
            {
                for (int currentPage = threadId; currentPage <= 1000; currentPage += threadCount)
                {
                    try
                    {
                        Console.WriteLine($"Processing Page {currentPage}");
                        string url = $"{Hinatazaka46_HomePage}/s/official/diary/member/list?page={currentPage}";
                        if (GetHtmlDocument(url)?.DocumentNode.SelectNodes("//div[@class='p-blog-group']/div[@class='p-blog-article']") is { } htmlNodeCollection)
                        {
                            foreach (HtmlNode element in htmlNodeCollection)
                            {
                                DateTime start = DateTime.Now;
                                string blogPath = $"{Hinatazaka46_HomePage}{element.Descendants("a").FirstOrDefault(n => n.HasClass("c-button-blog-detail"))?.GetAttributeValue("href", "/00000")}";
                                string blogMemberName = GetElementInnerText(element, "div", "c-blog-article__name").Replace(" ", "");
                                string blogID = GetBlogID(new Uri(blogPath).LocalPath);
                                if (!Hinatazaka46_Blogs.Any(x => x.ID == blogID))
                                {
                                    string blogTitle = GetElementInnerText(element, "div", "c-blog-article__title");
                                    string blogDateTime = GetElementInnerText(element, "div", "c-blog-article__date");
                                    HtmlNode blogInnerTextNode = element.SelectSingleNode(".//div[@class='c-blog-article__text']");
                                    List<string> imageList = blogInnerTextNode.Descendants("img").Select(e => e.GetAttributeValue("src", null)).Where(s => !string.IsNullOrEmpty(s)).ToList();
                                    if (imageList.Count > 20)
                                    {
                                        Console.WriteLine("Warning!");
                                        blogInnerTextNode = GetHtmlDocument(blogPath).DocumentNode.SelectSingleNode("//div[@class='c-blog-article__text']");
                                        imageList = blogInnerTextNode.Descendants("img").Select(e => e.GetAttributeValue("src", null)).Where(s => !string.IsNullOrEmpty(s)).ToList();
                                    }
                                    Hinatazaka46_Blogs.Add(new Blog
                                    {
                                        ID = blogID,
                                        Name = blogMemberName,
                                        Title = blogTitle,
                                        DateTime = ParseDateTime(blogDateTime, DateFormats[0], japanTime: true),
                                        ImageList = imageList
                                    });
                                    DateTime end = DateTime.Now;
                                    TimeSpan diff = end - start;
                                    Console.WriteLine($"Blog ID: [{blogID}][{blogMemberName}] Image Count: [{imageList.Count}] Total processing time: [{diff:h\\:mm\\:ss\\.fff}]");
                                }
                                else
                                {
                                    Console.WriteLine($"Duplicate Blog Id {blogID} for Member {blogMemberName} found on Page {currentPage}");
                                    return;
                                }
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
                        Console.WriteLine($"Not found in Page {currentPage}: {ex.Message}");
                        break;
                    }
                }
            });
        }

        public static void Hinatazaka46_Crawler()
        {
            Hinatazaka46_Blogs.Clear();
            Hinatazaka46_Blogs = GetMembers(Hinatazaka46_BlogStatus_FilePath).SelectMany(member => member.BlogList).ToList();

            int threadCount = Environment.ProcessorCount;
            List<Thread> articleThreads = Enumerable.Range(0, threadCount).Select(threadId => EnableThread(threadId, threadCount)).ToList();

            articleThreads.ForEach(t => t.Start());
            articleThreads.ForEach(t => t.Join());
            Hinatazaka46_Blogs.Sort((a, b) => a.ID.CompareTo(b.ID));

            List<Blog> old_Hinatazaka46_Blogs = [.. GetMembers(Hinatazaka46_BlogStatus_FilePath).SelectMany(member => member.BlogList).OrderBy(blog => blog.ID)];
            List<Blog> diff = [.. Hinatazaka46_Blogs.Where(predicate: blog => !old_Hinatazaka46_Blogs.Any(old_Blog => old_Blog.ID == blog.ID))];

            if (diff.Count > 0)
            {
                int blogsPerThread = Math.Max(diff.Count / threadCount, diff.Count % threadCount);
                List<Thread> mainThreads = [];

                for (int i = 0; i < threadCount; i++)
                {
                    int takeBlogsCount = Math.Min(diff.Count - i * blogsPerThread, blogsPerThread);
                    if (takeBlogsCount <= 0) break;
                    List<Blog> threadBlogs = diff.Skip(i * blogsPerThread).Take(takeBlogsCount).ToList();
                    mainThreads.Add(SaveBlogAllImage(threadBlogs, Hinatazaka46_Images_FilePath, string.Empty));
                }

                mainThreads.ForEach(t => t.Start());
                mainThreads.ForEach(t => t.Join());
            }

            IEnumerable<Member> newMembers = Hinatazaka46_Blogs.GroupBy(blog => blog.Name).Select(group => new Member
            {
                Name = group.Key,
                Group = IdolGroup.Hinatazaka46.ToString(),
                BlogList = [.. group]
            });

            string jsonString = JsonSerializer.Serialize(newMembers, jsonSerializerOptions);
            File.WriteAllText(Hinatazaka46_BlogStatus_FilePath, jsonString);
            Hinatazaka46_Blogs.Clear();
        }
    }
}