using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zakamichi_BlogCrawler.Model
{
    public class ImageContent
    {
        public string FileUrl { get; set; }
        public string FilePath { get; set; }
        public DateTime DateTime { get; set; }
        public string BlogId { get; set; }
        public string MemberName { get; set; }
        public string BlogTitle { get; set; }
        public int BlogImageCount { get; set; }
    }

}
