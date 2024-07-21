using System.Text;
using Zakamichi_BlogCrawler.Helper;
using Zakamichi_BlogCrawler.Model;
using static Zakamichi_BlogCrawler.Global;
using static Zakamichi_BlogCrawler.Zakamichi.Sakurazaka;
using static Zakamichi_BlogCrawler.Zakamichi.Hinatazaka;
using static Zakamichi_BlogCrawler.Zakamichi.Nogizaka;
using static Zakamichi_BlogCrawler.Zakamichi.Bokuao;
using static Zakamichi_BlogCrawler.Helper.TestTableBuilder;

class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        bool exit = false;

        while (!exit)
        {
            DisplayMainMenu();
            char chinput = GetUserInput();

            switch (chinput)
            {
                case 'h':
                    Hinatazaka46_Crawler();
                    break;
                case 's':
                    Sakurazaka46_Crawler();
                    break;
                case 'n':
                    Nogizaka46_Crawler();
                    break;
                case 'b':
                    Bokuao_Crawler_Ver_2();
                    break;
                case 'e':
                    ExportSingleMemberImages();
                    break;
                case 'a':
                    ManageDesiredMembers();
                    break;
                default:
                    Console.WriteLine("Unknown MainPage Command:");
                    break;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    private static void DisplayMainMenu()
    {
        Console.WriteLine("Welcome. Please Select Function:");
        Console.WriteLine("h: load all Hinatazaka46 blog");
        Console.WriteLine("s: load all Sakurazaka46 blog");
        Console.WriteLine("n: load all Nogizaka46 blog");
        Console.WriteLine("b: load all Bokuao blog");
        Console.WriteLine("e: Export all blog image of single Member");
        Console.WriteLine("a: Export all blog image of desired Members");
        Console.WriteLine("================================================================================");
    }

    private static char GetUserInput()
    {
        try
        {
            return Convert.ToChar(Console.ReadLine()[0]);
        }
        catch
        {
            return '\0';
        }
    }

    private static void ExportSingleMemberImages()
    {
        List<Member> MemberNameList;
        Console.WriteLine("Select Group:");
        Console.WriteLine("h: Hinatazaka46");
        Console.WriteLine("s: Sakurazaka46");
        Console.WriteLine("n: Nogizaka46");
        Console.WriteLine("b: Bokuao");
        Console.WriteLine("================================================================================");

        char chinput = GetUserInput();

        switch (chinput)
        {
            case 'h':
                MemberNameList = GetMembers(Hinatazaka46_BlogStatus_FilePath);
                break;
            case 's':
                MemberNameList = GetMembers(Sakurazaka46_BlogStatus_FilePath);
                break;
            case 'n':
                MemberNameList = GetMembers(Nogizaka46_BlogStatus_FilePath);
                break;
            case 'b':
                MemberNameList = GetMembers(Bokuao_BlogStatus_FilePath);
                break;
            default:
                Console.WriteLine("Unknown Command.");
                return;
        }

        SelectAndExportMemberImages(MemberNameList);
    }

    private static void SelectAndExportMemberImages(List<Member> memberNameList)
    {
        Console.WriteLine("Select Member:");
        foreach (var member in memberNameList.Select((m, i) => new { m.Name, Index = i }))
        {
            Console.WriteLine($"{member.Index + 1} : {member.Name}");
        }
        Console.WriteLine("================================================================================");

        try
        {
            int num = Convert.ToInt32(Console.ReadLine());
            if (num > 0 && num <= memberNameList.Count)
            {
                Member selectedMember = memberNameList[num - 1];
                ConsoleSpinner spinner = new() { Delay = 300 };
                bool end = false;

                Thread thread = new(() =>
                {
                    Export_SingleMember_BlogImages(selectedMember);
                    Console.WriteLine($"Export Result: {selectedMember.Name} Success");
                    end = true;
                });

                thread.Start();
                while (!end) spinner.Turn(displayMsg: "Working ", sequenceCode: 5);
                thread.Join();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unknown Command: {ex.Message}");
        }
    }

    private static void ManageDesiredMembers()
    {
        bool desiredPageExit = false;

        while (!desiredPageExit)
        {
            List<Member> fullMemberList = GetFullMemberList();
            List<string> selectedDesiredMembers = Load_Desired_MemberList();
            List<string> memberListView = CreateMemberListView(fullMemberList, selectedDesiredMembers);

            DisplayTable(memberListView);
            DisplayDesiredMemberMenu();

            char chinput = GetUserInput();

            switch (chinput)
            {
                case 'a':
                    AddDesiredMember(fullMemberList);
                    break;
                case 'r':
                    RemoveDesiredMember(fullMemberList);
                    break;
                case 'e':
                    ExportDesiredMembers(fullMemberList, selectedDesiredMembers);
                    break;
                case 'd':
                    ExportDesiredMembersBeforeDate(fullMemberList, selectedDesiredMembers);
                    break;
                case 'x':
                    desiredPageExit = true;
                    break;
                default:
                    Console.WriteLine("Unknown Command.");
                    break;
            }
        }
    }

    private static List<Member> GetFullMemberList()
    {
        return
        [
            .. GetMembers(Sakurazaka46_BlogStatus_FilePath),
            .. GetMembers(Hinatazaka46_BlogStatus_FilePath),
            .. GetMembers(Nogizaka46_BlogStatus_FilePath),
            .. GetMembers(Bokuao_BlogStatus_FilePath),
        ];
    }

    private static List<string> CreateMemberListView(List<Member> fullMemberList, List<string> selectedDesiredMembers)
    {
        return fullMemberList.Select((member, index) =>
            $"{index + 1} : {(selectedDesiredMembers.Contains(member.Name) ? $"[{member.Name}]" : member.Name)}").ToList();
    }

    private static void DisplayTable(List<string> memberListView)
    {
        int columnCount = 5;
        int rowCount = (int)Math.Ceiling((double)memberListView.Count / columnCount);
        TableBuilder tb = new();

        for (int i = 0; i < rowCount; i++)
        {
            List<string> columns = memberListView.Skip(i * columnCount).Take(columnCount).ToList();
            while (columns.Count < columnCount) columns.Add("");
            tb.AddRow([.. columns]);
        }

        Console.Write(tb.Output());
    }

    private static void DisplayDesiredMemberMenu()
    {
        Console.WriteLine("Select Function:");
        Console.WriteLine("a: add desired member");
        Console.WriteLine("r: remove desired member");
        Console.WriteLine("e: Export");
        Console.WriteLine("d: Export before Date");
        Console.WriteLine("x: Exit");
        Console.WriteLine("================================================================================");
    }

    private static void AddDesiredMember(List<Member> fullMemberList)
    {
        Console.WriteLine("Select Member to Add:");
        try
        {
            int num = Convert.ToInt32(Console.ReadLine());
            if (num > 0 && num <= fullMemberList.Count)
            {
                Member selectedMember = fullMemberList[num - 1];
                Console.WriteLine($"Add {selectedMember.Name} Result: {(Add_Desired_MemberList(selectedMember.Name) ? "Success" : "Fail")}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unknown Command: {ex.Message}");
        }
    }

    private static void RemoveDesiredMember(List<Member> fullMemberList)
    {
        Console.WriteLine("Select Member to Remove:");
        try
        {
            int num = Convert.ToInt32(Console.ReadLine());
            if (num > 0 && num <= fullMemberList.Count)
            {
                Member selectedMember = fullMemberList[num - 1];
                Console.WriteLine($"Remove {selectedMember.Name} Result: {(Remove_Desired_MemberList(selectedMember.Name) ? "Success" : "Fail")}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unknown Command: {ex.Message}");
        }
    }

    private static void ExportDesiredMembers(List<Member> fullMemberList, List<string> selectedDesiredMembers)
    {
        ConsoleSpinner spinner = new() { Delay = 300 };
        bool end = false;

        List<Thread> threads = fullMemberList
            .Where(m => selectedDesiredMembers.Contains(m.Name))
            .Select(selectedMember => new Thread(() =>
            {
                Export_SingleMember_BlogImages(selectedMember);
                Console.WriteLine($"Export Result: {selectedMember.Name} Success");
            }))
            .ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        end = true;
        while (!end) spinner.Turn(displayMsg: "Working ", sequenceCode: 5);
    }

    private static void ExportDesiredMembersBeforeDate(List<Member> fullMemberList, List<string> selectedDesiredMembers)
    {
        Console.WriteLine("Enter the Date:");
        string dateInput = Console.ReadLine();
        DateTime lastUpdate = ParseDateTime(dateInput, "yyyyMMdd");
        if (lastUpdate == DateTime.MinValue) lastUpdate = DateTime.Now.AddDays(-8);

        ConsoleSpinner spinner = new() { Delay = 300 };
        bool end = false;

        List<Thread> threads = fullMemberList
            .Where(m => selectedDesiredMembers.Contains(m.Name))
            .Select(selectedMember => new Thread(() =>
            {
                Export_SingleMember_BlogImages(selectedMember, lastUpdate);
                Console.WriteLine($"Export Result: {selectedMember.Name} Success");
            }))
            .ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        end = true;
        while (!end) spinner.Turn(displayMsg: "Working ", sequenceCode: 5);
    }


}

