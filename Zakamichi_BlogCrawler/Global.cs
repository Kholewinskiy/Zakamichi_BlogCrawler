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
            if (File.Exists(BlogStatus_FilePath))
            {
                return JsonSerializer.Deserialize<List<Member>>(File.ReadAllText(BlogStatus_FilePath), jsonSerializerOptions);
            }
            else
            {
                return [];
            }
        }

        public static Nogizaka46_BlogList GetHttpGetResponse(Uri uri)
        {
            Nogizaka46_BlogList blogList = new();
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
                using HttpRequestMessage request = new(HttpMethod.Get, uri);
                request.Headers.Add("Accept", "application/json");
                using HttpResponseMessage response = client.SendAsync(request).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    byte[] byteArray = [.. response.Content.ReadAsByteArrayAsync().Result];
                    string responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                    string Json = responseString[4..^2];
                    string end = responseString.Substring(responseString.Length - 2, 2);
                    string start = responseString[..4];
                    blogList = JsonSerializer.Deserialize<Nogizaka46_BlogList>(Json, jsonSerializerOptions) ?? new Nogizaka46_BlogList();
                }
            }
            return blogList;
        }

        public static Thread SaveBlogImage(List<Blog> BlogList, string HomePage_Url, string ImgFolderPath)
        {
            Thread mainThread = new(() =>
            {
                foreach (Blog blog in BlogList)
                {
                    string MemberName = blog.Name;

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
                        string logMessage = $"Saved {MemberName} blog [{blog.Title}] update on {blog.DateTime:yyyy-MM-dd} ImageCount:{blog.ImageList.Count}";
                        Console.WriteLine(logMessage);
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            });
            return mainThread;
        }

        public static void Export_SingleMember_BlogImages(Member member, DateTime? lastupdate = null)
        {
            List<Thread> mainThreads = [];
            int ThreadNumber = Environment.ProcessorCount;
            IEnumerable<Blog> bloglist = member.BlogList;
            string ExportFolder = ExportFilePath;
            if (lastupdate != null)
            {
                bloglist = bloglist.Where(blog => blog.DateTime >= lastupdate).ToList();
                ExportFolder = ForPhonePath;
            }
            string homePage = member switch
            {
                _ when member.Group == IdolGroup.Nogizaka46.ToString() => Nogizaka46_HomePage,
                _ when member.Group == IdolGroup.Sakurazaka46.ToString() => Sakurazaka46_HomePage,
                _ => ""
            };

            string folderName = member switch
            {
                _ when member.Group == IdolGroup.Nogizaka46.ToString() => "◢乃木坂46",
                _ when member.Group == IdolGroup.Sakurazaka46.ToString() => "◢櫻坂46",
                _ when member.Group == IdolGroup.Hinatazaka46.ToString() => "◢日向坂46",
                _ when member.Group == IdolGroup.Bokuao.ToString() => "僕青",
                _ => "Unknown"
            };
            string ImgFolderPath = $@"{ExportFolder}\{folderName}\{member.Name}\";
            Console.WriteLine($"member Name:{member.Name} ImgFolderPath:{ImgFolderPath}");

            int blogPerThread = (int)Math.Ceiling((decimal)bloglist.Count() / ThreadNumber);

            for (int i = 0; i <= ThreadNumber; i++)
            {
                List<Blog> threadBlogList = blogPerThread > 0 ? bloglist.Skip(i * blogPerThread)?.Take((i == ThreadNumber) ? bloglist.Count() % blogPerThread : blogPerThread)?.ToList() ?? [] : [];

                Thread mainThread = SaveBlogImage(threadBlogList, homePage, ImgFolderPath);
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


        public static bool Export_Images_File(string MemberName, List<Member> members, string Images_FilePath)
        {
            Member SelectedMember = members.Find(member => member.Name == MemberName);
            if (SelectedMember != null)
            {
                foreach (Blog blog in SelectedMember.BlogList)
                {
                    string ImgFolderPath = $@"{Images_FilePath}\{SelectedMember.Name}\{blog.ID}\";
                    string ExportPath = $@"{ExportFilePath}\{SelectedMember.Name}\";
                    if (!Directory.Exists(ExportPath))
                    {
                        Directory.CreateDirectory(ExportPath);
                    }
                    foreach (string sourceFile in Directory.GetFiles(ImgFolderPath, "*.*"))
                    {
                        string[] fileNames =
                        [
                            "0000",
                            "0001",
                            "0002",
                            "0003",
                            "0004",
                            "0005",
                            "0006",
                            "0007",
                            "0008",
                            "0009",
                            "0010"
                        ];

                        string destinationFile = fileNames.Contains(Path.GetFileNameWithoutExtension(sourceFile)) ?
                            $"{ExportPath}{blog.ID}_{Path.GetFileName(sourceFile)}" :
                            $"{ExportPath}{Path.GetFileName(sourceFile)}";

                        File.Copy(sourceFile, destinationFile, true);
                    }
                }
                return true;
            }
            return false;
        }

        public static string GetElementInnerText(HtmlNode element, string tag, string className, string attributeValue = null)
        {
            List<string> matchingNodes = element.Descendants(tag)
                .Where(n => n.HasClass(className) || (attributeValue != null && n.GetAttributeValue(className, null) == attributeValue))
                .Select(e => e.InnerText.Trim())
            .ToList();
            return matchingNodes.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "Unknown";
        }

        public static string GetBlogID(string articleUrl)
        {
            int index = articleUrl.LastIndexOf('/');
            return articleUrl[(index + 1)..];
        }


        public static string[] DateFormats =
        [
            "yyyy.M.d HH:mm",
            "yyyy/M/d",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy.MM.dd",
        ];

        public static DateTime ParseDateTime(string dateString, string dateFormat, bool japanTime = false)
        {
            DateTime dateValue = DateTime.MinValue;
            try
            {
                dateValue = DateTime.ParseExact(dateString, dateFormat, CultureInfo.GetCultureInfo("ja"), DateTimeStyles.AssumeLocal);
                if (japanTime)
                {
                    dateValue = dateValue.AddHours(-1);
                }
                return dateValue;
            }
            catch (FormatException)
            {
                Console.WriteLine("Unable to convert '{0}'.", dateString);
            }

            return dateValue;
        }

        public static HtmlDocument GetHtmlDocument(string urlAddress, List<Cookie> cookies)
        {
            HtmlDocument htmlDocument = new();
            using (HttpClient client = new())
            {
                HttpRequestMessage message = new(HttpMethod.Get, urlAddress);
                if (cookies.Count > 0)
                {
                    message.Headers.Add("Cookie", string.Join(';', cookies.Select(cookie => $"{cookie.Name}={cookie.Value}")));
                }

                using HttpResponseMessage httpResponseMessage = client.SendAsync(message).Result;
                if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {
                    using Stream receiveStream = httpResponseMessage.Content.ReadAsStream();
                    using StreamReader readStream = new(receiveStream, Encoding.UTF8);
                    string resultHtmlCode = readStream.ReadToEnd();
                    htmlDocument.LoadHtml(resultHtmlCode);
                }
            }
            return htmlDocument;
        }

        public static Thread SaveBlogAllImage(List<Blog> BlogList, string Images_FilePath, string HomePage_Url)
        {
            Thread mainThread = new(() =>
            {
                foreach (Blog blog in BlogList)
                {
                    string BlogID = blog.ID;
                    string MemberName = blog.Name;
                    string ImgFolderPath = $@"{Images_FilePath}\{MemberName}\{BlogID}\";
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
                        string logMessage = $"Saved {MemberName} blog [{blog.Title}] update on {blog.DateTime:yyyy-MM-dd} ImageCount:{blog.ImageList.Count}";
                        Console.WriteLine(logMessage);
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            });
            return mainThread;
        }

        public static bool SaveImage(string imgFileUrl, string imgFilePath, DateTime dateTime)
        {
            string Extension = Path.GetExtension(imgFileUrl).ToLower();
            if (Extension.Contains(".jpeg") || Extension.Contains(".jpg") || Extension.Contains(".png") || Extension.Contains(".gif"))
            {
                string FileName = Path.GetFileNameWithoutExtension(imgFileUrl).Length > 52 ?
                    Path.GetFileNameWithoutExtension(imgFileUrl)[..52] :
                    Path.GetFileNameWithoutExtension(imgFileUrl) + Extension;
                string imgFileName = $"{imgFilePath}{FileName}";

                if (File.Exists(imgFileName))
                {
                    if (File.GetCreationTime(imgFileName) != dateTime)
                    {
                        File.SetCreationTime(imgFileName, dateTime);
                    }
                    if (File.GetLastWriteTime(imgFileName) != dateTime)
                    {
                        File.SetLastWriteTime(imgFileName, dateTime);
                    }
                }
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            using HttpClient client = new();
                            using HttpResponseMessage httpResponseMessage = client.GetAsync(imgFileUrl).Result;
                            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                            {
                                try
                                {
                                    using Stream stream = httpResponseMessage.Content.ReadAsStream();
                                    using FileStream fileStream = new(imgFileName, FileMode.Create);
                                    stream.CopyTo(fileStream);

                                    fileStream.Dispose();
                                    stream.Dispose();
                                    File.SetCreationTime(imgFileName, dateTime);
                                    File.SetLastWriteTime(imgFileName, dateTime);
                                    File.SetLastAccessTime(imgFileName, dateTime);



                                    return true;
                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"stream of {imgFileUrl} is null {ex.Message}");
                                    Console.ForegroundColor = ConsoleColor.White;
                                }
                            }
                            httpResponseMessage.Dispose();
                            client.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Connect to {imgFileUrl} Fail: {ex.Message} Retry:{i}");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
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
            }
            return false;
        }

        public static bool Remove_Desired_MemberList(string MemberName)
        {
            try
            {
                List<string> Desired_MemberList = [];
                if (File.Exists(Desired_MemberList_FilePath))
                {
                    Desired_MemberList = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(Desired_MemberList_FilePath), jsonSerializerOptions);
                }
                int index = Desired_MemberList.FindIndex(name => name == MemberName);
                if (index != -1)
                {
                    Desired_MemberList.RemoveAt(index);
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

        public static void WriteLog(string message, ConsoleColor consoleColor = ConsoleColor.Yellow)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

    }
}
