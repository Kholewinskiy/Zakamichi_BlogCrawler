using HtmlAgilityPack;
using System.Text.Json;
using static Zakamichi_BlogCrawler.Global;
using Zakamichi_BlogCrawler.Model;
using System.Linq;

namespace Zakamichi_BlogCrawler.Zakamichi
{
    class Sakurazaka_test
    {
        static readonly Dictionary<string, Blog> Sakurazaka46_Blogs_Dictionary = [];
        public static Thread EnableThread(int threadId, int threadNumber)
        {

            Thread articleThread = new(() =>
            {
                bool endloop = false;
                for (int count = 0; count <= 100 && !endloop; count++)
                {
                    int currentPage = count * threadNumber + threadId;
                    Console.WriteLine($"Processing Page {currentPage}");
                    string url = $@"{Sakurazaka46_HomePage}/s/s46/diary/blog/list?ima=0000&page={currentPage}";
                    try
                    {
                        HtmlDocument htmlDocument = GetHtmlDocument(url, []);
                        if (htmlDocument != null)
                        {
                            HtmlNodeCollection htmlNodeCollection = htmlDocument.DocumentNode.SelectNodes("//li[@class='box']");
                            if (htmlNodeCollection != null)
                            {
                                foreach (HtmlNode element in htmlNodeCollection)
                                {
                                    DateTime start = DateTime.Now;

                                    string BlogPath = $"{Sakurazaka46_HomePage}{element.Descendants("a").First().Attributes["href"].Value}";
                                    string BlogDateTime = GetElementInnerText(element, "p", "class", "date wf-a");
                                    string BlogMemberName = GetElementInnerText(element, "p", "name").Trim().Replace(" ", "");
                                    string BlogTitle = GetElementInnerText(element, "h3", "title");
                                    string BlogID = GetBlogID(new Uri(BlogPath).LocalPath);
                                    HtmlNodeCollection ArticleCollection = GetHtmlDocument(BlogPath, []).DocumentNode.SelectNodes("//div[@class='box-article']");

                                    if (ArticleCollection != null)
                                    {
                                        HtmlNode ImageElement = ArticleCollection.First();
                                        List<string> ImageList = ImageElement.Descendants("img")
                                       .Select(e => e.GetAttributeValue("src", null))
                                       .Where(s => !string.IsNullOrEmpty(s)).ToList();

                                        Blog blog = new()
                                        {
                                            ID = BlogID,
                                            Name = BlogMemberName,
                                            Title = BlogTitle,
                                            DateTime = ParseDateTime(BlogDateTime, DateFormats[1]),
                                            ImageList = ImageList
                                        };
                                        if (Sakurazaka46_Blogs_Dictionary.TryAdd(blog.ID, blog))
                                        {
                                            DateTime end = DateTime.Now;
                                            TimeSpan diff = (end - start);
                                            Console.WriteLine("Total processing time for {5} Blog ID {4} : {0:00}:{1:00}:{2:00}.{3}", diff.Hours, diff.Minutes, diff.Seconds, diff.Milliseconds, blog.ID, blog.Name);
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Duplicate Blog Id {blog.ID} for Member {blog.Name} found on Page {threadId}");
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Not found on Blog Id {BlogID} for Member {BlogMemberName} ");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Not found in Page {currentPage}");
                                break;
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
            return articleThread;
        }

        public static void Sakurazaka46_Crawler_Ver_2()
        {
            List<Thread> mainThreads = [];
            List<Thread> articleThreads = [];

            foreach (Blog blog in GetMembers(Sakurazaka46_BlogStatus_FilePath).SelectMany(member => member.BlogList)) {
                Sakurazaka46_Blogs_Dictionary.TryAdd(blog.ID, blog);
            }

            Console.WriteLine("old Blog total: " + Sakurazaka46_Blogs_Dictionary.Count);

            int ThreadNumber = Environment.ProcessorCount;

            for (int threadId = 0; threadId < ThreadNumber; threadId++)
            {
                articleThreads.Add(EnableThread(threadId, ThreadNumber));
            }

            foreach (Thread articleThread in articleThreads)
            {
                articleThread.Start();
            }

            foreach (Thread articleThread in articleThreads)
            {
                articleThread.Join();
            }

  

            List<Member> new_Sakurazaka46_Members = [];

            List<Member> old_Sakurazaka46_Members = [.. GetMembers(Sakurazaka46_BlogStatus_FilePath)];
            List<Member> difference = [];

            foreach (Member member_new in new_Sakurazaka46_Members)
            {
                Member member_old = old_Sakurazaka46_Members.Find(member => member.Name == member_new.Name);
                List<Blog> blogs = (member_old == null) ?
                    member_new.BlogList :
                    member_new.BlogList.Where(blog_new =>
                    {
                        return member_old.BlogList.Find(blog_old => blog_old.ID == blog_new.ID) == null;
                    }).ToList();

                if (blogs.Count > 0)
                {
                    difference.Add(new Member
                    {
                        Name = member_new.Name,
                        BlogList = blogs
                    });
                }
            }



            List<Blog> bloglist = new_Sakurazaka46_Members.SelectMany(m => m.BlogList).ToList();
            if (bloglist.Count > 0)
            {
                int blogPerThread = bloglist.Count / ThreadNumber;
                for (int i = 0; i <= ThreadNumber; i++)
                {
                    int maxTakeThread = Math.Min(bloglist.Count - i * blogPerThread, blogPerThread);
                    List<Blog> threadBlogList = bloglist.Skip(i * blogPerThread).Take(maxTakeThread).ToList();
                    Thread mainThread = SaveBlogAllImage(threadBlogList, Sakurazaka46_Images_FilePath, Sakurazaka46_HomePage);
                    mainThreads.Add(mainThread);
                }
                foreach (Thread mainThread in mainThreads)
                {
                    mainThread.Start();
                }
                foreach (Thread mainThread in mainThreads)
                {
                    mainThread.Join();
                }
            }

            string JsonString = JsonSerializer.Serialize(new_Sakurazaka46_Members, jsonSerializerOptions);
            File.WriteAllText(Sakurazaka46_BlogStatus_FilePath, JsonString);
            Sakurazaka46_Blogs_Dictionary.Clear();
        }
    }
}
