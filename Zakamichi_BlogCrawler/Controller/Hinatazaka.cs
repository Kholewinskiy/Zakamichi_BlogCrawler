using HtmlAgilityPack;
using System.Text.Json;
using static Zakamichi_BlogCrawler.Global;
using Zakamichi_BlogCrawler.Model;

namespace Zakamichi_BlogCrawler.Zakamichi
{
    public class Hinatazaka
    {
        static List<Blog> Hinatazaka46_Blogs = [];

        public static Thread EnableThread(int threadId, int threadNumber)
        {
            bool endloop = false;
            Thread articleThread = new(() =>
            {
                for (int count = 0; count <= 50 && !endloop; count++)
                {
                    int currentPage = count * threadNumber + threadId;
                    Console.WriteLine($"Processing Page {currentPage}");
                    string url = $@"{Hinatazaka46_HomePage}/s/official/diary/member/list?page={currentPage}";
                    try
                    {
                        HtmlNodeCollection htmlNodeCollection = GetHtmlDocument(url, [])?.DocumentNode.SelectNodes("//div[@class='p-blog-group']/div[@class='p-blog-article']") ?? null;
                        if (htmlNodeCollection == null)
                        {
                            Console.WriteLine($"Not found in Page {currentPage}");
                            break;
                        }
                        else
                        {
                            foreach (HtmlNode element in htmlNodeCollection)
                            {
                                DateTime start = DateTime.Now;
                                string BlogPath = $"{Hinatazaka46_HomePage}{element.Descendants("a").Where(n => n.HasClass("c-button-blog-detail")).Select(e => e.GetAttributeValue("href", null)).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "/00000"}";
                                string BlogDateTime = GetElementInnerText(element, "div", "c-blog-article__date");
                                string BlogMemberName = GetElementInnerText(element, "div", "c-blog-article__name").Replace(" ","");
                                string BlogTitle = GetElementInnerText(element, "div", "c-blog-article__title");
                                string BlogID = GetBlogID(new Uri(BlogPath).LocalPath);
                            
                                HtmlNode BlogInnerTextNode = element.SelectSingleNode(".//div[@class='c-blog-article__text']");
                                List<string> ImageList = BlogInnerTextNode.Descendants("img").Select(e => e.GetAttributeValue("src", null)).Where(s => !string.IsNullOrEmpty(s)).ToList();
                                if (ImageList.Count > 20)
                                {
                                    Console.WriteLine("Warning!");
                                    BlogInnerTextNode = GetHtmlDocument(BlogPath, []).DocumentNode.SelectSingleNode("//div[@class='c-blog-article__text']");
                                    ImageList = BlogInnerTextNode.Descendants("img").Select(e => e.GetAttributeValue("src", null)).Where(s => !string.IsNullOrEmpty(s)).ToList();
                                }

                                Blog blog = new()
                                {
                                    ID = BlogID,
                                    Name = BlogMemberName,
                                    Title = BlogTitle,
                                    DateTime = ParseDateTime(BlogDateTime, DateFormats[0], japanTime: true),
                                    ImageList = ImageList
                                };
                                int ix = Hinatazaka46_Blogs.FindIndex(x => x.ID == BlogID);
                                if (ix == -1)
                                {
                                    Hinatazaka46_Blogs.Add(blog);
                                    DateTime end = DateTime.Now;
                                    TimeSpan diff = (end - start);
                                    Console.WriteLine("Blog ID: [{4}][{5}] Image Count: [{6}] Total processing time: [{0:00}:{1:00}:{2:00}.{3}]", diff.Hours, diff.Minutes, diff.Seconds, diff.Milliseconds, BlogID, BlogMemberName, ImageList.Count);
                                }
                                else
                                {
                                    Console.WriteLine($"Duplicate Blog Id {BlogID} for Member {BlogMemberName} found on Page {currentPage}");
                                    endloop = true;
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Not found in Page {currentPage}: {ex.Message}");
                        break;
                    }
                }
            });
            return articleThread;
        }

        public static void Hinatazaka46_Crawler_Ver_2()
        {
            List<Thread> mainThreads = [];
            List<Thread> articleThreads = [];

            Hinatazaka46_Blogs = GetMembers(Hinatazaka46_BlogStatus_FilePath).SelectMany(member => member.BlogList).ToList();

            int ThreadNumber = Environment.ProcessorCount;
            for (int threadId = 0; threadId < ThreadNumber; threadId++)
            {
                articleThreads.Add(EnableThread(threadId, ThreadNumber));
            }

            articleThreads.ForEach(articleThread => articleThread.Start());
            articleThreads.ForEach(articleThread => articleThread.Join());

            Hinatazaka46_Blogs = [.. Hinatazaka46_Blogs.OrderBy(blog => blog.ID)];

            // Create a list of new Hinatazaka46 members by grouping blogs
            List<Member> new_Hinatazaka46_Members = Hinatazaka46_Blogs
                .GroupBy(blog => blog.Name)
                .Select(group => new Member
                {
                    Name = group.Key,
                    Group = IdolGroup.Hinatazaka46.ToString(),
                    BlogList = [.. group]
                })
                .ToList();

            // Get the old Hinatazaka46 members from a file
            List<Member> old_Hinatazaka46_Members = GetMembers(Hinatazaka46_BlogStatus_FilePath);

            // Find the difference between new and old members
            List<Member> difference = new_Hinatazaka46_Members
                .Where(member_new =>
                {
                    var member_old = old_Hinatazaka46_Members.Find(member => member.Name == member_new.Name);
                    return member_old == null || member_new.BlogList.Any(blog_new =>
                        member_old.BlogList.All(blog_old => blog_old.ID != blog_new.ID));
                })
                .ToList();


            List<Blog> bloglist = new_Hinatazaka46_Members.SelectMany(m => m.BlogList).ToList();
            if (bloglist.Count > 0)
            {
                int blogPerThread = bloglist.Count / ThreadNumber;

                for (int i = 0; i <= ThreadNumber; i++)
                {
                    int maxTakeThread = Math.Min(bloglist.Count - i * blogPerThread, blogPerThread);
                    List<Blog> threadBlogList = bloglist.Skip(i * blogPerThread).Take(maxTakeThread).ToList();
                    Thread mainThread = SaveBlogAllImage(threadBlogList,Hinatazaka46_Images_FilePath,string.Empty);
                    mainThreads.Add(mainThread);
                }
                mainThreads.ForEach(mainThread => mainThread.Start());
                mainThreads.ForEach(mainThread => mainThread.Join());
            }

            string JsonString = JsonSerializer.Serialize(new_Hinatazaka46_Members, jsonSerializerOptions);
            File.WriteAllText(Hinatazaka46_BlogStatus_FilePath, JsonString);
            Hinatazaka46_Blogs.Clear();
        }
    }
}
