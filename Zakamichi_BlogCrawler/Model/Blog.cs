using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zakamichi_BlogCrawler.Model
{
    public class Blog
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public DateTime DateTime { get; set; }
        public List<string> ImageList { get; set; }
    }

    public class Nogizaka46_BlogList
    {
        public string count { get; set; }

        public Nogizaka46_BlogData[] data { get; set; }
    }

    public class Nogizaka46_BlogData
    {
        public string code { get; set; }
        public string title { get; set; }
        public string date { get; set; }
        public string link { get; set; }
        public string name { get; set; }
        public string text { get; set; }

    }
}
