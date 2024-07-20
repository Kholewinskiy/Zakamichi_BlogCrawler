using HtmlAgilityPack;
using System.Text.Json;
using static Zakamichi_BlogCrawler.Global;
using System.Net;
using Zakamichi_BlogCrawler.Model;

namespace Zakamichi_BlogCrawler.Zakamichi
{
    class Bokuao
    {
        static readonly Dictionary<string, string> Bokuao_Members = new()
        {
          {"10","青木 宙帆"},
          {"11","秋田 莉杏"},
          {"12","安納 蒼衣"},
          {"13","伊藤 ゆず"},
          {"14","今井 優希"},
          {"15","岩本 理瑚"},
          {"16","金澤 亜美"},
          {"17","木下 藍"},
          {"18","工藤 唯愛"},
          {"19","塩釜 菜那"},
          {"20","杉浦 英恋"},
          {"21","須永 心海"},
          {"22","西森 杏弥"},
          {"23","萩原 心花"},
          {"24","長谷川 稀未"},
          {"25","早﨑 すずき"},
          {"26","宮腰 友里亜"},
          {"27","持永 真奈"},
          {"28","八重樫 美伊咲"},
          {"29","八木 仁愛"},
          {"30","柳堀 花怜"},
          {"31","山口 結杏"},
          {"32","吉本 此那"},
        };

        static readonly Dictionary<string, string> desired_Bokuao_Members = new()
        {
            {"12","安納 蒼衣"},
            {"16","金澤 亜美"},
            {"25","早﨑 すずき"},
            {"26","宮腰 友里亜"},
        };

        static List<Blog> Bokuao_Blogs = [];
        public static Thread EnableThread(int threadId, int threadNumber, KeyValuePair<string, string> member)
        {
            bool endloop = false;
            Thread articleThread = new(() =>
            {
                for (int count = 0; count <= 100 && !endloop; count++)
                {
                    int currentPage = count * threadNumber + threadId + 1;
                    Console.WriteLine($"Processing Member {member.Value} Page {currentPage}");
                    string url = $@"{Bokuao_HomePage}/blog/list/1/0/?writer={member.Key}&page={currentPage}";
                    Console.WriteLine($"url:{url}");

                    List<Cookie> cookies =[
                        new()
                        {
                            Name = "bokuao.com",
                            Domain = "bokuao.com",
                            Value = "466e56a660fd2c1db539b79dd7e579687070cce9af022a12531559a7e5e69645"
                        },
                        new()
                        {
                            Name = "_ga",
                            Domain = ".bokuao.com",
                            Value = "GA1.1.1597477387.1689080792",
                        },
                        new()
                        {
                            Name = "_ga_REPH28T1S0",
                            Domain = ".bokuao.com",
                            Value = "GS1.1.1720706617.17.1.1720706833.0.0.0"
                        },
                        new()
                        {
                            Domain = "bokuao.com",
                            Name = "PHPSESSID",
                            Value = "a0sl25akjhmeitf95hheqn6qs0"
                        }
                        ];

                    try
                    {
                        HtmlDocument htmlDocument = GetHtmlDocument(url, cookies);
                        if (htmlDocument == null)
                        {
                            Console.WriteLine($"Not found in Page {currentPage} for Member {member.Value}");
                            break;
                        }
                        else
                        {
                            HtmlNodeCollection htmlNodeCollection = htmlDocument.DocumentNode.SelectNodes("//li[@data-delighter]");
                            if (htmlNodeCollection == null)
                            {
                                Console.WriteLine($"Not found in Page {currentPage} for Member {member.Value}");
                                break;
                            }
                            else
                            {
                                foreach (HtmlNode element in htmlNodeCollection)
                                {
                                    DateTime start = DateTime.Now;

                                    string BlogPath = $"{Bokuao_HomePage}{element.Descendants("a").First().Attributes["href"].Value}";
                                    string BlogDateTime = GetElementInnerText(element, "time", "date");
                                    string BlogMemberName = GetElementInnerText(element, "p", "writer").Replace(" ", "");
                                    string BlogTitle = GetElementInnerText(element, "p", "tit");
                                    string BlogID = GetBlogID(new Uri(BlogPath).LocalPath);
                                    HtmlNodeCollection ArticleCollection = GetHtmlDocument(BlogPath, cookies).DocumentNode.SelectNodes("//main[@class='content-main']");

                                    if (ArticleCollection == null)
                                    {
                                        Console.WriteLine($"Not found on Blog Id {BlogID} for Member {BlogMemberName} ");
                                        break;
                                    }
                                    else
                                    {
                                        HtmlNode ImageElement = ArticleCollection.First();
                                        List<string> ImageList = ImageElement.Descendants("img")
                                       .Select(e => e.GetAttributeValue("src", null))
                                       .Where(s => !string.IsNullOrEmpty(s) && s != "/static/common/global-image/dummy.gif" && s != "/static/ligareaz/official/common/cover_video.png").ToList();

                                        Blog blog = new()
                                        {
                                            ID = BlogID,
                                            Name = BlogMemberName,
                                            Title = BlogTitle,
                                            DateTime = ParseDateTime(BlogDateTime, DateFormats[3]),
                                            ImageList = ImageList
                                        };

                                        int ix = Bokuao_Blogs.FindIndex(x => x.ID == BlogID);
                                        DateTime end = DateTime.Now;
                                        TimeSpan diff = (end - start);
                                        Console.WriteLine($"Total processing time for {BlogMemberName} Blog ID {BlogID} ImageCount:{ImageList.Count} {diff.Hours:00}:{diff.Minutes:00}:{diff.Seconds:00}.{diff.Milliseconds}");
                                        if (ix != -1)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine($"Duplicate Blog Id {BlogID} for Member {BlogMemberName} found on Page {currentPage}");
                                            Console.ForegroundColor = ConsoleColor.White;
                                            if (currentPage != 2 || count > 2)
                                            {
                                                //endloop = true;
                                                //break;
                                            }
                                        }

                                        Bokuao_Blogs.Add(blog);

                                    }
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

        public static void Bokuao_Crawler_Ver_2()
        {
            List<Thread> mainThreads = [];
            List<Thread> articleThreads = [];
            if (Directory.Exists(Bokuao_Images_FilePath))
            {
                Directory.CreateDirectory(Bokuao_Images_FilePath);
            }
            Bokuao_Blogs = GetMembers(Bokuao_BlogStatus_FilePath).SelectMany(member => member.BlogList).ToList();

            Console.WriteLine("old Blog total: " + Bokuao_Blogs.Count);

            int ThreadNumber = Environment.ProcessorCount / desired_Bokuao_Members.Count;

            foreach (KeyValuePair<string, string> member in desired_Bokuao_Members)
            {
                for (int threadId = 0; threadId < ThreadNumber; threadId++)
                {
                    articleThreads.Add(EnableThread(threadId, ThreadNumber, member));
                }
            }

            articleThreads.ForEach(articleThread => articleThread.Start());
            articleThreads.ForEach(articleThread => articleThread.Join());
            Bokuao_Blogs = [.. Bokuao_Blogs.OrderBy(blog => blog.ID)];

            List<Member> new_Bokuao_Members = [];

            foreach (IGrouping<string, Blog> BlogList_Groupby_Members in Bokuao_Blogs.GroupBy(blog => blog.Name))
            {
                new_Bokuao_Members.Add(new Member
                {
                    Name = BlogList_Groupby_Members.Key,
                    Group = IdolGroup.Bokuao.ToString(),
                    BlogList = [.. BlogList_Groupby_Members]
                });
            }

            List<Member> old_Bokuao_Members = GetMembers(Bokuao_BlogStatus_FilePath);

            List<Member> difference = [];
            foreach (Member member_new in new_Bokuao_Members)
            {
                Member member_old = old_Bokuao_Members.Find(member => member.Name == member_new.Name);
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



            List<Blog> bloglist = new_Bokuao_Members.SelectMany(m => m.BlogList).ToList();

            if (bloglist.Count > 0)
            {
                int blogPerThread = bloglist.Count / ThreadNumber;
                for (int i = 0; i <= ThreadNumber; i++)
                {
                    int maxTakeThread = Math.Min(bloglist.Count - i * blogPerThread, blogPerThread);
                    List<Blog> threadBlogList = bloglist.Skip(i * blogPerThread).Take(maxTakeThread).ToList();
                    Thread mainThread = SaveBlogAllImage(threadBlogList, Bokuao_Images_FilePath, string.Empty);
                    mainThreads.Add(mainThread);
                }
                mainThreads.ForEach(mainThread => mainThread.Start());
                mainThreads.ForEach(mainThread => mainThread.Join());

            }

            string JsonString = JsonSerializer.Serialize(new_Bokuao_Members, jsonSerializerOptions);
            File.WriteAllText(Bokuao_BlogStatus_FilePath, JsonString);
        }
    }
}
