using System.Text;
using Zakamichi_BlogCrawler.Helper;
using Zakamichi_BlogCrawler.Model;
using static Zakamichi_BlogCrawler.Global;
using static Zakamichi_BlogCrawler.Zakamichi.Sakurazaka;
using static Zakamichi_BlogCrawler.Zakamichi.Hinatazaka;
using static Zakamichi_BlogCrawler.Zakamichi.Nogizaka;
using static Zakamichi_BlogCrawler.Zakamichi.Bokuao;
using static Zakamichi_BlogCrawler.Helper.TestTableBuilder;

internal class Program
{

    private static void Main_test()
    {
        Console.OutputEncoding = Encoding.UTF8;
        bool exit = false;

        while (!exit)
        {
            DisplayMainMenu();
            var key = Console.ReadKey(intercept: true).Key;

            switch (key)
            {
                case ConsoleKey.H:
                    Hinatazaka46_Crawler_Ver_2();
                    break;
                case ConsoleKey.S:
                    Sakurazaka46_Crawler_Ver_2();
                    break;
                case ConsoleKey.N:
                    Nogizaka46_Crawler_Ver_2();
                    break;
                case ConsoleKey.B:
                    Bokuao_Crawler_Ver_2();
                    break;
                case ConsoleKey.E:
                    ExportSingleMemberImages();
                    break;
                case ConsoleKey.A:
                    ExportDesiredMembersImages();
                    break;
                default:
                    Console.WriteLine("Unknown MainPage Command:");
                    break;
            }
        }
    }

    private static void DisplayMainMenu()
    {
        Console.Clear();
        Console.WriteLine("Welcome. Please Select Function:");
        Console.WriteLine("h: load all Hinatazaka46 blog");
        Console.WriteLine("s: load all Sakurazaka46 blog");
        Console.WriteLine("n: load all Nogizaka46 blog");
        Console.WriteLine("b: load all Bokuao blog");
        Console.WriteLine("e: Export all blog image of single Member");
        Console.WriteLine("a: Export all blog image of desired Members");
        Console.WriteLine("================================================================================");
    }

    private static void ExportSingleMemberImages()
    {
        Console.Clear();
        Console.WriteLine("Select Group:");
        Console.WriteLine("h: Hinatazaka46");
        Console.WriteLine("s: Sakurazaka46");
        Console.WriteLine("n: Nogizaka46");
        Console.WriteLine("b: Bokuao");
        Console.WriteLine("================================================================================");

        var key = Console.ReadKey(intercept: true).Key;
        var (memberList, imagesFilePath) = GetGroupData(key);

        if (memberList == null)
        {
            Console.WriteLine("Unknown Command.");
            return;
        }

        Console.Clear();
        Console.WriteLine("Select Member:");
        for (int i = 0; i < memberList.Count; i++)
        {
            Console.WriteLine($"{i + 1} : {memberList[i].Name}");
        }
        Console.WriteLine("================================================================================");

        if (int.TryParse(Console.ReadLine(), out int num) && num > 0 && num <= memberList.Count)
        {
            var selectedMember = memberList[num - 1];
            //ExecuteWithSpinner(() => Export_SingleMember_BlogImages(selectedMember));
            Console.WriteLine($"Export Result: {selectedMember.Name} Success");
        }
        else
        {
            Console.WriteLine("Invalid selection.");
        }
    }

    private static void ExportDesiredMembersImages()
    {
        bool desiredPageExit = false;
        List<Member> fullMemberList = GetAllMembers();
        while (!desiredPageExit)
        {

            List<string> selectedDesiredMembers = Load_Desired_MemberList();
            DisplayMemberList(fullMemberList, selectedDesiredMembers);

            Console.WriteLine("Select Function:");
            Console.WriteLine("a: add desired member");
            Console.WriteLine("r: remove desired member");
            Console.WriteLine("e: Export");
            Console.WriteLine("d: Export before Date");
            Console.WriteLine("x: Exit");
            Console.WriteLine("================================================================================");

            var key = Console.ReadKey(intercept: true).Key;
            switch (key)
            {
                case ConsoleKey.A:
                    AddOrRemoveMember(fullMemberList, selectedDesiredMembers, true);
                    break;
                case ConsoleKey.R:
                    AddOrRemoveMember(fullMemberList, selectedDesiredMembers, false);
                    break;
                case ConsoleKey.E:
                    AddOrRemoveMember(fullMemberList, selectedDesiredMembers, true);
                    break;

            }
        }
    }

    private static void AddOrRemoveMember(List<Member> Full_MemberList, List<string> Selected_Desired_Member,bool Add)
    {
        string action = Add ? "Add" : "Remove";
        Console.WriteLine($"Select Member to {action}:");
        try
        {
            string InputCmd = Console.ReadLine();
            int Num = Convert.ToInt32(InputCmd);
            if (Num > 0 && Num <= Full_MemberList.Count)
            {
                Member SelectedMember = Full_MemberList[Num - 1];
                string result = (Add ? Add_Desired_MemberList(SelectedMember.Name) : Remove_Desired_MemberList(SelectedMember.Name)) ? "Success" : "Fail";
                Console.WriteLine($"Add {SelectedMember.Name} Result: {result}"); ;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unknown Command : {ex.Message}");
        }
    }

    private static void DisplayMemberList(List<Member> Full_MemberList, List<string> Selected_Desired_Member)
    {
        List<string> MemberList_View = Full_MemberList.Select(selector: (member, Index) => $"{Index + 1} : {(Selected_Desired_Member.Any(m => m == member.Name) ? $"[{member.Name}]" : member.Name)}").ToList();
        int columnCount = 5;
        int rowCount = (int)Math.Ceiling((double)MemberList_View.Count / columnCount);
        TableBuilder tb = new();
        for (int index = 0; index < rowCount; index++)
        {
            List<string> columns = MemberList_View.Skip(index * columnCount).Take(Math.Min(MemberList_View.Count - index * columnCount, columnCount)).ToList();
            while (columns.Count < columnCount)
            {
                columns.Add("");
            }
            tb.AddRow(columns.ToArray());
        }
        Console.Write(tb.Output());
    }

    private static (List<Member> memberList, string imagesFilePath) GetGroupData(ConsoleKey key)
    {
        return key switch
        {
            ConsoleKey.H => (GetMembers(Hinatazaka46_BlogStatus_FilePath), Hinatazaka46_Images_FilePath),
            ConsoleKey.S => (GetMembers(Sakurazaka46_BlogStatus_FilePath), Sakurazaka46_Images_FilePath),
            ConsoleKey.N => (GetMembers(Nogizaka46_BlogStatus_FilePath), Nogizaka46_Images_FilePath),
            ConsoleKey.B => (GetMembers(Bokuao_BlogStatus_FilePath), Bokuao_Images_FilePath),
            _ => (null, null),
        };
    }

    private static List<Member> GetAllMembers()
    {
        List<Member> Full_MemberList = [
                               .. GetMembers(Sakurazaka46_BlogStatus_FilePath),
                                    .. GetMembers(Hinatazaka46_BlogStatus_FilePath),
                                    .. GetMembers(Nogizaka46_BlogStatus_FilePath),
                                    .. GetMembers(Bokuao_BlogStatus_FilePath)];
        return Full_MemberList;
    }


    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        char chinput;
        bool exit = false;

        while (!exit)
        {
            Console.WriteLine("Welcome.Please Select Function:");
            Console.WriteLine("h: load all Hinatazaka46 blog");
            Console.WriteLine("s: load all Sakurazaka46 blog");
            Console.WriteLine("n: load all Nogizaka46 blog");
            Console.WriteLine("b: load all Bokuao blog");
            Console.WriteLine("e: Export all blog image of single Member");
            Console.WriteLine("a: Export all blog image of desired Members");
            Console.WriteLine("================================================================================");
            try
            {
                chinput = Convert.ToChar(Console.ReadLine()[0]);
                switch (chinput)
                {
                    case 'h':
                        {
                            Hinatazaka46_Crawler_Ver_2();
                            break;
                        }
                    case 's':
                        {
                            Sakurazaka46_Crawler_Ver_2();
                            break;
                        }
                    case 'n':
                        {
                            Nogizaka46_Crawler_Ver_2();
                            break;
                        }
                    case 'b':
                        {
                            Bokuao_Crawler_Ver_2();
                            break;
                        }
                    case 'e':
                        {

                            List<Member> MemberNameList = [];
                            string Images_FilePath = "";
                            Console.WriteLine("Select Group:");
                            Console.WriteLine("h: Hinatazaka46");
                            Console.WriteLine("s: Sakurazaka46");
                            Console.WriteLine("n: Nogizaka46");
                            Console.WriteLine("b: Bokuao");
                            Console.WriteLine("================================================================================");
                            chinput = Convert.ToChar(Console.ReadLine()[0]);

                            if (chinput == 'h')
                            {
                                MemberNameList = GetMembers(Hinatazaka46_BlogStatus_FilePath);
                                Images_FilePath = Hinatazaka46_Images_FilePath;

                            }
                            else if (chinput == 's')
                            {
                                MemberNameList = GetMembers(Sakurazaka46_BlogStatus_FilePath);
                                Images_FilePath = Sakurazaka46_Images_FilePath;

                            }
                            else if (chinput == 'n')
                            {
                                MemberNameList = GetMembers(Nogizaka46_BlogStatus_FilePath);
                                Images_FilePath = Nogizaka46_Images_FilePath;
                            }
                            else if (chinput == 'b')
                            {
                                MemberNameList = GetMembers(Bokuao_BlogStatus_FilePath);
                                Images_FilePath = Bokuao_Images_FilePath;
                            }
                            else
                            {
                                Console.WriteLine("Unknown Command.");
                                break;
                            }


                            Console.WriteLine("Select Member:");
                            foreach (var member in MemberNameList.Select(member => member.Name).Select((str, i) => new { Value = str, Index = i }))
                            {
                                Console.WriteLine($"{member.Index + 1} : {member.Value}");
                            }
                            Console.WriteLine("================================================================================");
                            try
                            {
                                string InputCmd = Console.ReadLine();
                                int Num = Convert.ToInt32(InputCmd);
                                if (Num > 0 && Num <= MemberNameList.Count)
                                {
                                    ConsoleSpinner spinner = new()
                                    {
                                        Delay = 300
                                    };
                                    bool end = false;
                                    Thread thread = new(() =>
                                    {
                                        Member SelectedMember = MemberNameList.Find(member => member.Name == MemberNameList[Num - 1].Name);
                                        Export_SingleMember_BlogImages(SelectedMember);
                                        Console.WriteLine($"Export Result:{SelectedMember} Success"); ;
                                        //Console.WriteLine($"Export Result:{(Export_Images_File(MemberNameList[Num - 1].Name, MemberNameList, Images_FilePath) ? "Success" : "Fail")}"); ;
                                        end = true;
                                    });
                                    thread.Start();
                                    while (!end)
                                    {
                                        spinner.Turn(displayMsg: "Working ", sequenceCode: 5);
                                    }

                                    thread.Join();

                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Unknown Command :　{ex.Message}");
                            }
                            break;
                        }
                    case 'a':
                        {
                            bool desiredPageExit = false;
                            while (!desiredPageExit)
                            {
                                List<Member> Sakurazaka46_MemberList = GetMembers(Sakurazaka46_BlogStatus_FilePath);
                                List<Member> Hinatazaka46_MemberList = GetMembers(Hinatazaka46_BlogStatus_FilePath);
                                List<Member> Nogizaka46_MemberList = GetMembers(Nogizaka46_BlogStatus_FilePath);
                                List<Member> Bokuao_MemberList = GetMembers(Bokuao_BlogStatus_FilePath);
                                List<Member> Full_MemberList = [
                                    .. GetMembers(Sakurazaka46_BlogStatus_FilePath),
                                    .. GetMembers(Hinatazaka46_BlogStatus_FilePath),
                                    .. GetMembers(Nogizaka46_BlogStatus_FilePath),
                                    .. GetMembers(Bokuao_BlogStatus_FilePath)];

                                List<string> Selected_Desired_Member = Load_Desired_MemberList();
                                List<string> MemberList_View = Full_MemberList.Select(selector: (member, Index) => $"{Index + 1} : {(Selected_Desired_Member.Any(m => m == member.Name) ? $"[{member.Name}]" : member.Name)}").ToList();
                                int columnCount = 5;
                                int rowCount = (int)Math.Ceiling((double)MemberList_View.Count / columnCount);
                                TableBuilder tb = new();
                                for (int index = 0; index < rowCount; index++)
                                {
                                    List<string> columns = MemberList_View.Skip(index * columnCount).Take(Math.Min(MemberList_View.Count - index * columnCount, columnCount)).ToList();
                                    while (columns.Count < columnCount)
                                    {
                                        columns.Add("");
                                    }
                                    tb.AddRow(columns.ToArray());
                                }
                                Console.Write(tb.Output());
                                Console.WriteLine("Select Function:");
                                Console.WriteLine("a: add desired member");
                                Console.WriteLine("r: remove desired member");
                                Console.WriteLine("e: Export");
                                Console.WriteLine("d: Export before Date");

                                Console.WriteLine("x: Exit");
                                Console.WriteLine("================================================================================");


                                chinput = Convert.ToChar(Console.ReadLine()[0]);
                                switch (chinput)
                                {
                                    case 'a':
                                        Console.WriteLine("Select Member to Add:");
                                        try
                                        {
                                            string InputCmd = Console.ReadLine();
                                            int Num = Convert.ToInt32(InputCmd);
                                            if (Num > 0 && Num <= Full_MemberList.Count)
                                            {
                                                Member SelectedMember = Full_MemberList[Num - 1];
                                                Console.WriteLine($"Add {SelectedMember.Name} Result: {(Add_Desired_MemberList(SelectedMember.Name) ? "Success" : "Fail")}"); ;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Unknown Command: {ex.Message}");
                                        }
                                        break;
                                    case 'r':
                                        Console.WriteLine("Select Member to Remove:");
                                        try
                                        {
                                            string InputCmd = Console.ReadLine();
                                            int Num = Convert.ToInt32(InputCmd);
                                            if (Num > 0 && Num <= Full_MemberList.Count)
                                            {
                                                Member SelectedMember = Full_MemberList[Num - 1];
                                                Console.WriteLine($"Add {SelectedMember.Name} Result: {(Remove_Desired_MemberList(SelectedMember.Name) ? "Success" : "Fail")}"); ;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Unknown Command : {ex.Message}");
                                        }
                                        break;
                                    case 'e':
                                        {
                                            bool end = false;
                                            ConsoleSpinner spinner = new()
                                            {
                                                Delay = 300
                                            };
                                            IEnumerable<Member> SelectedMembers = Full_MemberList.Where(member => Selected_Desired_Member.Contains(member.Name));
                                            List<Thread> Threads = [];

                                            foreach (Member SelectedMember in SelectedMembers)
                                            {
                                                Thread thread = new(() =>
                                                {
                                                    Export_SingleMember_BlogImages(SelectedMember);
                                                    Console.WriteLine($"Export Result: {SelectedMember.Name} Success");
                                                });
                                                Threads.Add(thread);
                                            }

                                            foreach (Thread thread1 in Threads)
                                            {
                                                thread1.Start();
                                            }

                                            foreach (Thread thread1 in Threads)
                                            {
                                                thread1.Join();
                                            }
                                            end = true;
                                            while (!end)
                                            {
                                                spinner.Turn(displayMsg: "Working ", sequenceCode: 5);
                                            }
                                            break;
                                        }

                                    case 'd':
                                        {
                                            Console.WriteLine("Enter the Date:");
                                            string str = Console.ReadLine();
                                            DateTime lastupdate = ParseDateTime(str, "yyyyMMdd");
                                            if (lastupdate == DateTime.MinValue)
                                            {
                                                lastupdate = DateTime.Now.AddDays(-8);
                                            }
                                            bool end = false;
                                            ConsoleSpinner spinner = new()
                                            {
                                                Delay = 300
                                            };
                                            IEnumerable<Member> SelectedMembers = Full_MemberList.Where(member => Selected_Desired_Member.Contains(member.Name));
                                            List<Thread> Threads = [];

                                            foreach (Member SelectedMember in SelectedMembers)
                                            {
                                                Thread thread = new(() =>
                                                {
                                                    Export_SingleMember_BlogImages(SelectedMember, lastupdate);
                                                    Console.WriteLine($"Export Result: {SelectedMember.Name} Success");
                                                });
                                                Threads.Add(thread);
                                            }

                                            foreach (Thread thread1 in Threads)
                                            {
                                                thread1.Start();
                                            }

                                            foreach (Thread thread1 in Threads)
                                            {
                                                thread1.Join();
                                            }
                                            end = true;
                                            while (!end)
                                            {
                                                spinner.Turn(displayMsg: "Working ", sequenceCode: 5);
                                            }
                                            break;
                                        }
                                    case 'p':
                                        {

                                            break;
                                        }
                                    case 'x':
                                        {
                                            desiredPageExit = true;
                                            break;
                                        }

                                }

                            }
                            break;


                        }
                    default:
                        Console.WriteLine("Unknown MainPage Command:");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unknown MainPage error:{ex}");
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}