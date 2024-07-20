using HtmlAgilityPack;
using System.Text.Json;
using static Zakamichi_BlogCrawler.Global;
using Zakamichi_BlogCrawler.Model;
using System.Net;

namespace Zakamichi_BlogCrawler.Zakamichi
{
    class Sakurazaka
    {
        static readonly List<Blog> Sakurazaka46_Blogs = [];
        public static void Sakurazaka46_Crawler_Ver_2()
        {
            Sakurazaka46_Blogs.Clear();
            Sakurazaka46_Blogs.AddRange(GetMembers(Sakurazaka46_BlogStatus_FilePath).SelectMany(member => member.BlogList));
            Console.WriteLine("Old Blog total: " + Sakurazaka46_Blogs.Count);

            int threadCount = Environment.ProcessorCount;
            List<Thread> articleThreads = Enumerable.Range(0, threadCount)
                .Select(threadId => EnableThread(threadId, threadCount))
                .ToList();

            articleThreads.ForEach(thread => thread.Start());
            articleThreads.ForEach(thread => thread.Join());

            Sakurazaka46_Blogs.Sort((a, b) => a.ID.CompareTo(b.ID));

            List<Member> newMembers = Sakurazaka46_Blogs
                .GroupBy(blog => blog.Name)
                .Select(group => new Member
                {
                    Name = group.Key,
                    Group = IdolGroup.Sakurazaka46.ToString(),
                    BlogList = [.. group]
                })
                .ToList();

            List<Member> oldMembers = GetMembers(Sakurazaka46_BlogStatus_FilePath);
            List<Member> difference = newMembers
                .Where(newMember =>
                {
                    Member oldMember = oldMembers.Find(member => member.Name == newMember.Name);
                    return oldMember == null || newMember.BlogList.Any(newBlog => oldMember.BlogList.All(oldBlog => oldBlog.ID != newBlog.ID));
                })
                .ToList();

            if (difference.Count != 0)
            {
                int blogPerThread = (difference.SelectMany(m => m.BlogList).Count() + threadCount - 1) / threadCount;
                List<Thread> mainThreads = Enumerable.Range(0, threadCount).Select(i =>
                {
                    int maxTakeThread = Math.Min(difference.SelectMany(m => m.BlogList).Count() - i * blogPerThread, blogPerThread);
                    List<Blog> threadBlogList = difference.SelectMany(m => m.BlogList).Skip(i * blogPerThread).Take(maxTakeThread).ToList();
                    return SaveBlogAllImage(threadBlogList, Sakurazaka46_Images_FilePath, Sakurazaka46_HomePage);
                }).ToList();

                mainThreads.ForEach(thread => thread.Start());
                mainThreads.ForEach(thread => thread.Join());
            }

            string jsonString = JsonSerializer.Serialize(newMembers, jsonSerializerOptions);
            File.WriteAllText(Sakurazaka46_BlogStatus_FilePath, jsonString);
            Sakurazaka46_Blogs.Clear();
        }

        private static Thread EnableThread(int threadId, int threadCount)
        {
            bool endLoop = false;
            return new Thread(() =>
            {
                for (int currentPage = threadId; currentPage <= 100 && endLoop == false; currentPage += threadCount)
                {
                    Console.WriteLine($"Processing Page {currentPage}");
                    string url = $"{Sakurazaka46_HomePage}/s/s46/diary/blog/list?ima=0000&page={currentPage}";
                    HtmlDocument htmlDocument = GetHtmlDocument(url, []);
                    if (htmlDocument?.DocumentNode?.SelectNodes("//li[@class='box']") is { } htmlNodeCollection)
                    {
                        foreach (HtmlNode element in htmlNodeCollection)
                        {
                            bool result = ProcessBlog(element, currentPage);
                            if (!result)
                            {
                                endLoop = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Not found in Page {currentPage}");
                        break;
                    }
                }
            });
        }

        private static bool ProcessBlog(HtmlNode element, int currentPage)
        {
            DateTime start = DateTime.Now;
            string blogPath = $"{Sakurazaka46_HomePage}{element.Descendants("a").First().Attributes["href"].Value}";
            string blogDateTime = GetElementInnerText(element, "p", "class", "date wf-a");
            string blogMemberName = GetElementInnerText(element, "p", "name").Trim().Replace(" ", "");
            string blogTitle = GetElementInnerText(element, "h3", "title");
            string blogId = GetBlogID(new Uri(blogPath).LocalPath);
            HtmlNodeCollection articleCollection = GetHtmlDocument(blogPath, [])?.DocumentNode.SelectNodes("//div[@class='box-article']");

            if (articleCollection != null)
            {
                HtmlNode imageElement = articleCollection.First();
                List<string> imageList = imageElement.Descendants("img")
                    .Select(e => e.GetAttributeValue("src", null))
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                Blog blog = new()
                {
                    ID = blogId,
                    Name = blogMemberName,
                    Title = blogTitle,
                    DateTime = ParseDateTime(blogDateTime, DateFormats[1]),
                    ImageList = imageList
                };

                int index = Sakurazaka46_Blogs.FindIndex(x => x.ID == blogId);
                if (index == -1)
                {
                    Sakurazaka46_Blogs.Add(blog);
                }
                else
                {
                    Console.WriteLine($"Duplicate Blog Id {blogId} for Member {blogMemberName} found on Page {currentPage}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"Not found on Blog Id {blogId} for Member {blogMemberName}");
                return false;
            }

            DateTime end = DateTime.Now;
            TimeSpan diff = end - start;
            Console.WriteLine($"Total processing time for {blogMemberName} Blog ID {blogId}: {diff:hh\\:mm\\:ss\\.fff}");
            return true;
        }
    }
}
