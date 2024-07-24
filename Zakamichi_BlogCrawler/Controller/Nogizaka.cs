using HtmlAgilityPack;
using System.Text.Json;
using static Zakamichi_BlogCrawler.Global;
using Zakamichi_BlogCrawler.Model;
namespace Zakamichi_BlogCrawler.Zakamichi
{
    public static class Nogizaka
    {
        private static Dictionary<string, Blog> Nogizaka46_Blogs = [];
        private static readonly Dictionary<string, List<string>> Nogizaka46_Members = new()
        {
            {"３期生", new List<string> {"伊藤理々杏", "岩本蓮加", "梅澤美波", "大園桃子", "久保史緒里", "阪口珠美", "佐藤楓", "中村麗乃", "向井葉月", "山下美月", "吉田綾乃クリスティー", "与田祐希"}},
            {"４期生", new List<string> {"遠藤さくら", "賀喜遥香", "掛橋沙耶香", "金川紗耶", "北川悠理", "柴田柚菜", "清宮レイ", "田村真佑", "筒井あやめ", "早川聖来", "矢久保美緒"}},
            {"新4期生", new List<string> {"黒見明香", "佐藤璃果", "林瑠奈", "松尾美佑", "弓木奈於"}},
            {"5期生", new List<string> {"五百城茉央", "池田瑛紗", "一ノ瀬美空", "井上和", "岡本姫奈", "小川彩", "奥田いろは", "川﨑桜", "菅原咲月", "冨里奈央", "中西アルノ"}}
        };
        private static readonly HashSet<string> AcceptedMemberList = new(
        [
            "久保史緒里", "池田瑛紗", "一ノ瀬美空", "井上和", "小川彩", "川﨑桜", "菅原咲月", "冨里奈央", "柴田柚菜", "田村真佑", "早川聖来", "松尾美佑"
        ]);
        private static void GetBlogsInfo(int threadId)
        {
            Uri uri = new($"https://www.nogizaka46.com/s/n46/api/list/blog?rw=1024&st={threadId * 1024}&callback=res");
            Nogizaka46_BlogList blogList = GetHttpGetResponse(uri);
            if (blogList.data == null)
            {
                Console.WriteLine("end");
                return;
            }

            foreach (Nogizaka46_BlogData blogData in blogList.data)
            {
                DateTime start = DateTime.Now;
                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(blogData.text);

                Blog blog = new()
                {
                    ID = blogData.code,
                    Title = blogData.title,
                    Name = blogData.name.Replace(" ", ""),
                    DateTime = ParseDateTime(blogData.date, DateFormats[2], japanTime: true),
                    ImageList = htmlDocument.DocumentNode?.Descendants("img")
                    .Select(e => e.GetAttributeValue("src", null))
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList() ?? [],
                };

                if (Nogizaka46_Blogs.TryAdd(blog.ID, blog))
                {
                    TimeSpan diff = DateTime.Now - start;
                    Console.WriteLine($"Total processing time for {blog.Name} Blog ID {blog.ID} : {diff:hh\\:mm\\:ss\\.fff}");
                }
                else
                {
                    Console.WriteLine($"Duplicate Blog Id {blog.ID} for Member {blog.Name} found on Page {threadId}");
                    break;
                }
            }
        }

        public static void Nogizaka46_Crawler()
        {
            int threadNumber = Environment.ProcessorCount;

            for (int threadId = 0; threadId < threadNumber; threadId++)
            {
                GetBlogsInfo(threadId);
            }

            Nogizaka46_Blogs = Nogizaka46_Blogs.OrderBy(kv => kv.Value.DateTime).ToDictionary(x => x.Key, x => x.Value);

            List<Member> newNogizaka46Members = GetGroupedMembers();
            List<Blog> bloglist = newNogizaka46Members
                .Where(m => AcceptedMemberList.Contains(m.Name))
                .SelectMany(m => m.BlogList)
                .ToList();

            int blogPerThread = bloglist.Count / threadNumber;

            List<Thread> mainThreads = Enumerable.Range(0, threadNumber).Select(threadId =>
            {
                List<Blog> threadBlogList = bloglist.Skip(threadId * blogPerThread).Take(blogPerThread).ToList();
                Thread mainThread = SaveBlogAllImage(threadBlogList, Nogizaka46_Images_FilePath, Nogizaka46_HomePage);
                return mainThread;
            }) .ToList();

            mainThreads.ForEach(t => t.Start());
            mainThreads.ForEach(t => t.Join());

            string jsonString = JsonSerializer.Serialize(newNogizaka46Members, jsonSerializerOptions);
            File.WriteAllText(Nogizaka46_BlogStatus_FilePath, jsonString);
        }

        public static List<Member> GetGroupedMembers()
        {
            List<Member> groupedMembers = Nogizaka46_Blogs.Values
                .GroupBy(blog => blog.Name)
                .Select(group =>
                {
                    Member member = new()
                    {
                        Name = group.Key,
                        Group = IdolGroup.Nogizaka46.ToString(),
                        BlogList = [.. group]
                    };

                    if (Nogizaka46_Members.TryGetValue(group.Key, out var selectedKiMemberNames))
                    {
                        member.BlogList = group.Select((blog, index) =>
                        {
                            blog.Name = selectedKiMemberNames[index % selectedKiMemberNames.Count];
                            return blog;
                        }).ToList();
                    }

                    return member;
                })
                .ToList();

            return groupedMembers;
        }


    }
}