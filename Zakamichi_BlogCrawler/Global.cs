using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using HtmlAgilityPack;
using Zakamichi_BlogCrawler.Model;

namespace Zakamichi_BlogCrawler
{
    public class Global
    {
        public static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.Create(new TextEncoderSettings(UnicodeRanges.All)),
            WriteIndented = true
        };
        public const string Sakurazaka46_HomePage = @"https://sakurazaka46.com";
        public const string Hinatazaka46_HomePage = @"https://hinatazaka46.com";
        public const string Nogizaka46_HomePage = @"https://nogizaka46.com";
        public const string Bokuao_HomePage = @"https://bokuao.com";
        public const string PicturesFolderPath = $@"C:\Users\mojss\Pictures";
        public const string BlogStatus_FilePath = "BlogStatus.JSON";

        public static readonly string Hinatazaka46_Images_FilePath = $@"{PicturesFolderPath}\Hinatazaka46_Images";
        public static readonly string Sakurazaka46_Images_FilePath = $@"{PicturesFolderPath}\Sakurazaka46_Images";
        public static readonly string Nogizaka46_Images_FilePath = $@"{PicturesFolderPath}\Nogizaka46_Images";
        public static readonly string Bokuao_Images_FilePath = $@"{PicturesFolderPath}\Bokuao_Images";

        public static readonly string Hinatazaka46_BlogStatus_FilePath = $@"{Hinatazaka46_Images_FilePath}\{BlogStatus_FilePath}";
        public static readonly string Sakurazaka46_BlogStatus_FilePath = $@"{Sakurazaka46_Images_FilePath}\{BlogStatus_FilePath}";
        public static readonly string Nogizaka46_BlogStatus_FilePath = $@"{Nogizaka46_Images_FilePath}\{BlogStatus_FilePath}";
        public static readonly string Bokuao_BlogStatus_FilePath = $@"{Bokuao_Images_FilePath}\{BlogStatus_FilePath}";

        public static readonly string ExportFilePath = $@"{PicturesFolderPath}\Export";
        public static readonly string ForPhonePath = $@"{PicturesFolderPath}\ForPhone";
        public static readonly string Desired_MemberList_FilePath = $@"{ExportFilePath}\Desired_Member_List.JSON";
        public static List<Member> GetMembers(string BlogStatus_FilePath)
        {
            return File.Exists(BlogStatus_FilePath)
                ? JsonSerializer.Deserialize<List<Member>>(File.ReadAllText(BlogStatus_FilePath), jsonSerializerOptions)
                : [];
        }

        public static Nogizaka46_BlogList GetHttpGetResponse(Uri uri)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
            using HttpRequestMessage request = new(HttpMethod.Get, uri);
            request.Headers.Add("Accept", "application/json");

            using HttpResponseMessage response = client.SendAsync(request).Result;
            if (response.StatusCode != HttpStatusCode.OK) return new Nogizaka46_BlogList();

            string responseString = Encoding.UTF8.GetString(response.Content.ReadAsByteArrayAsync().Result);
            string json = responseString[4..^2];

            return JsonSerializer.Deserialize<Nogizaka46_BlogList>(json, jsonSerializerOptions) ?? new Nogizaka46_BlogList();
        }

        public static Thread SaveBlogImage(List<Blog> BlogList, string HomePage_Url, string ImgFolderPath)
        {
            return new Thread(() =>
            {
                foreach (Blog blog in BlogList)
                {
                    if (!Directory.Exists(ImgFolderPath))
                    {
                        Directory.CreateDirectory(ImgFolderPath);
                    }

                    bool result = blog.ImageList.Count > 0;
                    foreach (string remoteFileUrl in blog.ImageList)
                    {
                        result &= SaveImage($"{HomePage_Url}{remoteFileUrl}", ImgFolderPath, blog.DateTime);
                    }

                    if (result)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Saved {blog.Name} blog [{blog.Title}] update on {blog.DateTime:yyyy-MM-dd} ImageCount:{blog.ImageList.Count}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            });
        }

        public static void Export_SingleMember_BlogImages(Member member, DateTime? lastupdate = null)
        {

            IEnumerable<Blog> bloglist = lastupdate == null ? member.BlogList : member.BlogList.Where(blog => blog.DateTime >= lastupdate);
            string ExportFolder = lastupdate == null ? ExportFilePath : ForPhonePath;

            string homePage = member.Group switch
            {
                nameof(IdolGroup.Nogizaka46) => Nogizaka46_HomePage,
                nameof(IdolGroup.Sakurazaka46) => Sakurazaka46_HomePage,
                _ => ""
            };

            string folderName = member.Group switch
            {
                nameof(IdolGroup.Nogizaka46) => "◢乃木坂46",
                nameof(IdolGroup.Sakurazaka46) => "◢櫻坂46",
                nameof(IdolGroup.Hinatazaka46) => "◢日向坂46",
                nameof(IdolGroup.Bokuao) => "僕青",
                _ => "Unknown"
            };

            string ImgFolderPath = $@"{ExportFolder}\{folderName}\{member.Name}\";
            Console.WriteLine($"member Name:{member.Name} ImgFolderPath:{ImgFolderPath}");

            int ThreadNumber = Environment.ProcessorCount;
            int blogPerThread = (int)Math.Ceiling((decimal)bloglist.Count() / ThreadNumber);
            List<Thread> mainThreads = Enumerable.Range(0, ThreadNumber).Select(threadId =>
            {
                List<Blog> threadBlogList = bloglist.Skip(threadId * blogPerThread).Take((threadId == ThreadNumber) ? bloglist.Count() % blogPerThread : blogPerThread).ToList();
                return SaveBlogImage(threadBlogList, homePage, ImgFolderPath);
            }).ToList();

            mainThreads.ForEach(t => t.Start());
            mainThreads.ForEach(t => t.Join());
        }

        public static string GetElementInnerText(HtmlNode element, string tag, string className, string attributeValue = null)
        {
            return element.Descendants(tag)
                .Where(n => n.HasClass(className) || (attributeValue != null && n.GetAttributeValue(className, null) == attributeValue))
                .Select(e => e.InnerText.Trim())
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "Unknown";
        }

        public static string GetBlogID(string articleUrl)
        {
            return articleUrl[(articleUrl.LastIndexOf('/') + 1)..];
        }

        public static readonly string[] DateFormats =
            [
            "yyyy.M.d HH:mm",
            "yyyy/M/d",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy.MM.dd",
            ];

        public static DateTime ParseDateTime(string dateString, string dateFormat, bool japanTime = false)
        {
            try
            {
                DateTime dateValue = DateTime.ParseExact(dateString, dateFormat, CultureInfo.GetCultureInfo("ja"), DateTimeStyles.AssumeLocal);
                return japanTime ? dateValue.AddHours(-1) : dateValue;
            }
            catch (FormatException)
            {
                Console.WriteLine($"Unable to convert '{dateString}'.");
                return DateTime.MinValue;
            }
        }

        public static HtmlDocument GetHtmlDocument(string urlAddress, List<Cookie> cookies = null)
        {
            HtmlDocument htmlDocument = new();
            using HttpClient client = new();
            HttpRequestMessage message = new(HttpMethod.Get, urlAddress);
            if (cookies != null && cookies.Count > 0)
            {
                message.Headers.Add("Cookie", string.Join(';', cookies.Select(cookie => $"{cookie.Name}={cookie.Value}")));
            }

            using HttpResponseMessage httpResponseMessage = client.SendAsync(message).Result;
            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
            {
                string resultHtmlCode = new StreamReader(httpResponseMessage.Content.ReadAsStream(), Encoding.UTF8).ReadToEnd();
                htmlDocument.LoadHtml(resultHtmlCode);
            }
            return htmlDocument;
        }

        public static Thread SaveBlogAllImage(List<Blog> BlogList, string Images_FilePath, string HomePage_Url)
        {
            return new Thread(() =>
            {
                foreach (Blog blog in BlogList)
                {
                    string ImgFolderPath = $@"{Images_FilePath}\{blog.Name}\{blog.ID}\";
                    if (!Directory.Exists(ImgFolderPath)) Directory.CreateDirectory(ImgFolderPath);

                    bool result = blog.ImageList.Count > 0;
                    foreach (string remoteFileUrl in blog.ImageList)
                    {
                        result &= SaveImage($"{HomePage_Url}{remoteFileUrl}", ImgFolderPath, blog.DateTime);
                    }

                    if (result)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Saved {blog.Name} blog [{blog.Title}] update on {blog.DateTime:yyyy-MM-dd} ImageCount:{blog.ImageList.Count}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            });
        }

        public static bool SaveImage(string imgFileUrl, string imgFilePath, DateTime dateTime)
        {
            string extension = Path.GetExtension(imgFileUrl).ToLower();
            if (!extension.Contains(".jpeg") && !extension.Contains(".jpg") && !extension.Contains(".png") && !extension.Contains(".gif")) return false;

            string fileName = Path.GetFileNameWithoutExtension(imgFileUrl);
            fileName = fileName.Length > 52 ? fileName[..52] : fileName + extension;
            string imgFileName = $"{imgFilePath}{fileName}";

            if (File.Exists(imgFileName))
            {
                if (File.GetCreationTime(imgFileName) != dateTime) File.SetCreationTime(imgFileName, dateTime);
                if (File.GetLastWriteTime(imgFileName) != dateTime) File.SetLastWriteTime(imgFileName, dateTime);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        using HttpClient client = new();
                        using HttpResponseMessage response = client.GetAsync(imgFileUrl).Result;
                        if (response.StatusCode != HttpStatusCode.OK) continue;

                        using Stream stream = response.Content.ReadAsStream();
                        using FileStream fileStream = new(imgFileName, FileMode.Create);
                        stream.CopyTo(fileStream);

                        File.SetCreationTime(imgFileName, dateTime);
                        File.SetLastWriteTime(imgFileName, dateTime);
                        File.SetLastAccessTime(imgFileName, dateTime);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Connect to {imgFileUrl} Fail: {ex.Message} Retry:{i}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            }
            return false;
        }

        public static List<string> Load_Desired_MemberList()
        {
            try
            {
                if (File.Exists(Desired_MemberList_FilePath))
                {
                    return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(Desired_MemberList_FilePath), jsonSerializerOptions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Load_Desired_MemberList error: {ex.Message}");
            }
            return [];
        }

        public static bool Add_Desired_MemberList(string MemberName)
        {
            try
            {
                List<string> Desired_MemberList = [];
                if (File.Exists(Desired_MemberList_FilePath))
                {
                    Desired_MemberList = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(Desired_MemberList_FilePath), jsonSerializerOptions);
                }
                Desired_MemberList.Add(MemberName);
                File.WriteAllText(Desired_MemberList_FilePath, JsonSerializer.Serialize(Desired_MemberList, jsonSerializerOptions));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Add_Desired_MemberList error: {ex.Message}");
                return false;
            }
        }

        public static bool Remove_Desired_MemberList(string MemberName)
        {
            try
            {
                if (!File.Exists(Desired_MemberList_FilePath)) return false;

                List<string> Desired_MemberList = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(Desired_MemberList_FilePath), jsonSerializerOptions);
                if (Desired_MemberList.Remove(MemberName))
                {
                    File.WriteAllText(Desired_MemberList_FilePath, JsonSerializer.Serialize(Desired_MemberList, jsonSerializerOptions));
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Remove_Desired_MemberList error: {ex.Message}");
            }
            return false;
        }

    }
}
