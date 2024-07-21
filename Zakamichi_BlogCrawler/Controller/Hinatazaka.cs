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
                    Console.WriteLine($"Processing Page {currentPage}");
                    string url = $"{Hinatazaka46_HomePage}/s/official/diary/member/list?page={currentPage}";
                    try
                    {
                        if(GetHtmlDocument(url)?.DocumentNode.SelectNodes("//div[@class='p-blog-group']/div[@class='p-blog-article']") is { } htmlNodeCollection)
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

            Hinatazaka46_Blogs = [.. Hinatazaka46_Blogs.OrderBy(blog => blog.ID)];

            List<Member> newHinatazaka46Members = Hinatazaka46_Blogs
                .GroupBy(blog => blog.Name)
                .Select(group => new Member
                {
                    Name = group.Key,
                    Group = IdolGroup.Hinatazaka46.ToString(),
                    BlogList = [.. group]
                })
                .ToList();

            List<Member> oldHinatazaka46Members = GetMembers(Hinatazaka46_BlogStatus_FilePath);

            List<Member> difference = newHinatazaka46Members
                .Where(newMember => oldHinatazaka46Members.All(oldMember => oldMember.Name != newMember.Name ||
                     oldMember.BlogList.All(oldBlog => newMember.BlogList.Any(newBlog => oldBlog.ID != newBlog.ID))))
                .ToList();

            List<Blog> blogList = newHinatazaka46Members.SelectMany(m => m.BlogList).ToList();
            if (blogList.Count > 0)
            {
                int blogsPerThread = blogList.Count / threadCount;

                List<Thread> mainThreads = Enumerable.Range(0, threadCount).Select(i =>
                {
                    List<Blog> threadBlogs = blogList.Skip(i * blogsPerThread).Take(blogsPerThread).ToList();
                    return SaveBlogAllImage(threadBlogs, Hinatazaka46_Images_FilePath, string.Empty);
                }) .ToList();


                mainThreads.ForEach(t => t.Start());
                mainThreads.ForEach(t => t.Join());
            }

            var jsonString = JsonSerializer.Serialize(newHinatazaka46Members, jsonSerializerOptions);
            File.WriteAllText(Hinatazaka46_BlogStatus_FilePath, jsonString);
            Hinatazaka46_Blogs.Clear();
        }
    }
}