

namespace Zakamichi_BlogCrawler.Model
{
    public class Member
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public List<Blog> BlogList { get; set; }
    }

    public enum IdolGroup
    {
        Nogizaka46,
        Sakurazaka46,
        Hinatazaka46,
        Bokuao
    }
}
