using HtmlAgilityPack;
using System.Text.Json;
using static Zakamichi_BlogCrawler.Global;
using Zakamichi_BlogCrawler.Model;

namespace Zakamichi_BlogCrawler.Zakamichi
{
    public static class Nogizaka
    {
        static Dictionary<string, Blog> Nogizaka46_Blogs = [];

        static readonly List<string> Nogizaka46_5ki_Members =
        [
            "五百城茉央",
            "池田瑛紗",
            "一ノ瀬美空",
            "井上和",
            "岡本姫奈",
            "小川彩",
            "奥田いろは",
            "川﨑桜",
            "菅原咲月",
            "冨里奈央",
            "中西アルノ"
        ];
        static readonly List<string> Nogizaka46_4ki_Members =
        [
            "遠藤さくら",
            "賀喜遥香",
            "掛橋沙耶香",
            "金川紗耶",
            "北川悠理",
            "柴田柚菜",
            "清宮レイ",
            "田村真佑",
            "筒井あやめ",
            "早川聖来",
            "矢久保美緒",
        ];
        static readonly List<string> Nogizaka46_new_4ki_Members =
        [
            "黒見明香",
            "佐藤璃果",
            "林瑠奈",
            "松尾美佑",
            "弓木奈於",
        ];
        static readonly List<string> Nogizaka46_3ki_Members =
        [
            "伊藤理々杏",
            "岩本蓮加",
            "梅澤美波",
            "大園桃子",
            "久保史緒里",
            "阪口珠美",
            "佐藤楓",
            "中村麗乃",
            "向井葉月",
            "山下美月",
            "吉田綾乃クリスティー",
            "与田祐希"
        ];
        static readonly List<string> AcceptedMemberList =
        [
            "久保史緒里",
            "池田瑛紗",
            "一ノ瀬美空",
            "井上和",
            "小川彩",
            "川﨑桜",
            "菅原咲月",
            "冨里奈央",
            "柴田柚菜",
            "田村真佑",
            "早川聖来",
            "松尾美佑"
        ];

        public static readonly Dictionary<string, List<string>> Nogizaka46_Members = new()
        {
            {"３期生",Nogizaka46_3ki_Members },
            {"４期生",Nogizaka46_4ki_Members },
            {"新4期生",Nogizaka46_new_4ki_Members },
            {"5期生",Nogizaka46_5ki_Members },
        };

        private static void GetBlogsInfo(int threadId)
        {
            Uri uri = new($"https://www.nogizaka46.com/s/n46/api/list/blog?rw=1024&st={threadId * 1024}&callback=res");
            Nogizaka46_BlogList blogList = GetHttpGetResponse(uri);
            if (blogList.data == null)
            {
                Console.WriteLine("end");
            }
            else
            {
                foreach (Nogizaka46_BlogData blogData in blogList.data)
                {
                    DateTime start = DateTime.Now;
                    HtmlDocument htmlDocument = new();
                    htmlDocument.LoadHtml(blogData.text);
                    HtmlNode htmlNode = htmlDocument.DocumentNode;
                    List<string> ImageList = [];
                    if (htmlNode != null)
                    {
                        ImageList = htmlNode.Descendants("img").Select(e => e.GetAttributeValue("src", null)).Where(s => !string.IsNullOrEmpty(s)).ToList();
                    }

                    if (Nogizaka46_Members.TryGetValue(blogData.name, out List<string> members))
                    {
                        int index = members.FindIndex(blogData.title.Contains);
                        if (index < 0)
                        {
                            int strIndex = blogData.text.Length - 1;
                            foreach (string name in members)
                            {
                                strIndex = blogData.text.IndexOf(name) >= strIndex ? strIndex : blogData.text.IndexOf(name);
                            }
                        }
                    }
                    Blog blog = new()
                    {
                        ID = blogData.code,
                        Title = blogData.title,
                        Name = blogData.name.Replace(" ", ""),
                        DateTime = ParseDateTime(blogData.date, DateFormats[2], japanTime: true),
                        ImageList = ImageList,
                    };

                    if (Nogizaka46_Blogs.TryAdd(blog.ID, blog))
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
            }
        }

        public static void Nogizaka46_Crawler_Ver_2()
        {
            List<Thread> mainThreads = [];
            List<Thread> articleThreads = [];
            int ThreadNumber = Environment.ProcessorCount;
            for (int threadId = 0; threadId < ThreadNumber; threadId++)
            {
                GetBlogsInfo(threadId);
            }
            foreach (Thread articleThread in articleThreads)
            {
                articleThread.Start();
            }
            foreach (Thread articleThread in articleThreads)
            {
                articleThread.Join();
            }

            Nogizaka46_Blogs = Nogizaka46_Blogs.OrderBy(kv => kv.Value.DateTime).ToDictionary(x => x.Key, x => x.Value);
            List<Member> new_Nogizaka46_Members = GetGroupedMembers();
            List<Blog> bloglist = new_Nogizaka46_Members.Where(m => AcceptedMemberList.Contains(m.Name)).SelectMany(m => m.BlogList).ToList();
            int blogPerThread = bloglist.Count / ThreadNumber;
            for (int i = 0; i <= ThreadNumber; i++)
            {
                List<Blog> threadBlogList = bloglist.Skip(i * blogPerThread).Take((i == (ThreadNumber)) ? bloglist.Count % blogPerThread : blogPerThread).ToList();
                Thread mainThread = SaveBlogAllImage(threadBlogList, Nogizaka46_Images_FilePath, Nogizaka46_HomePage);
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

            string JsonString = JsonSerializer.Serialize(new_Nogizaka46_Members, jsonSerializerOptions);
            File.WriteAllText(Nogizaka46_BlogStatus_FilePath, JsonString);
        }

        public static List<Member> GetGroupedMembers()
        {
            List<Member> members = [];
            foreach (IGrouping<string, Blog> BlogList_Groupby_Members in Nogizaka46_Blogs.Select(kv => kv.Value).GroupBy(blog => blog.Name))
            {
                Member collectedMember = new();
                if (Nogizaka46_Members.TryGetValue(BlogList_Groupby_Members.Key, out List<string> selected_ki_MemberNames))
                {

                    foreach (IGrouping<int, Blog> BlogList_Groupby_selected_ki_Members in BlogList_Groupby_Members.GroupBy(blog => BlogList_Groupby_Members.ToList().IndexOf(blog) % selected_ki_MemberNames.Count))
                    {
                        collectedMember = new Member
                        {
                            Name = selected_ki_MemberNames[BlogList_Groupby_selected_ki_Members.Key],
                            Group = IdolGroup.Nogizaka46.ToString(),
                            BlogList = BlogList_Groupby_selected_ki_Members.ToList().Select(blog =>
                            {
                                blog.Name = selected_ki_MemberNames[BlogList_Groupby_selected_ki_Members.Key];
                                return blog;
                            }).ToList()
                        };
                        members.AddMember(collectedMember);
                    }
                }
                else
                {

                    collectedMember = new Member
                    {
                        Name = BlogList_Groupby_Members.Key,
                        Group = IdolGroup.Nogizaka46.ToString(),
                        BlogList = [.. BlogList_Groupby_Members]
                    };
                    members.AddMember(collectedMember);
                }

            }
            return members;
        }


        public static void AddMember(this List<Member> members, Member member)
        {
            int selectedMemberIndex = members.FindIndex(m => m.Name == member.Name);
            if (selectedMemberIndex < 0)
            {
                members.Add(member);
            }
            else
            {
                members[selectedMemberIndex].BlogList.AddRange(member.BlogList);
            }
        }
    }
}
