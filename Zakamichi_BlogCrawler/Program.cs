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

    // Determine if a character is a CJK character
    static bool IsDoubleWidth(char c)
    {

        // CJK Unified Ideographs
        if (c >= 0x4E00 && c <= 0x9FFF) return true;
        // CJK Unified Ideographs Extension A
        if (c >= 0x3400 && c <= 0x4DBF) return true;
        // CJK Unified Ideographs Extension B
        if (c >= 0x20000 && c <= 0x2A6DF) return true;
        // CJK Unified Ideographs Extension C
        if (c >= 0x2A700 && c <= 0x2B73F) return true;
        // CJK Unified Ideographs Extension D
        if (c >= 0x2B740 && c <= 0x2B81F) return true;
        // CJK Unified Ideographs Extension E
        if (c >= 0x2B820 && c <= 0x2CEAF) return true;
        // CJK Compatibility Ideographs
        if (c >= 0xF900 && c <= 0xFAFF) return true;
        // CJK Compatibility Ideographs Supplement
        if (c >= 0x2F800 && c <= 0x2FA1F) return true;
        // Enclosed CJK Letters and Months
        if (c >= 0x3200 && c <= 0x32FF) return true;
        // CJK Compatibility
        if (c >= 0x3300 && c <= 0x33FF) return true;
        // Full-width and half-width forms
        if (c >= 0xFF00 && c <= 0xFFEF) return true;
        // Hiragana and Katakana
        if (c >= 0x3040 && c <= 0x309F) return true;
        if (c >= 0x30A0 && c <= 0x30FF) return true;
        // Hangul Jamo
        if (c >= 0x1100 && c <= 0x11FF) return true;
        // Hangul Syllables
        if (c >= 0xAC00 && c <= 0xD7AF) return true;

        // Additional ranges for punctuation and symbols
        // General Punctuation
        if (c >= 0x2000 && c <= 0x206F) return true;
        // CJK Symbols and Punctuation
        if (c >= 0x3000 && c <= 0x303F) return true;
        // Half-width and Full-width Forms
        if (c >= 0xFF00 && c <= 0xFFEF) return true;

        return false;
    }

    // Pad a string to a specified width
    static string PadString(string input, int width)
    {
        int inputWidth = input.Sum(c => IsDoubleWidth(c) ? 2 : 1);
        return input + new string(' ', width - inputWidth);
    }
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
            $"{index + 1}.{(selectedDesiredMembers.Contains(member.Name) ? $"[{member.Name}]" : member.Name)}").ToList();
    }
    private static void DisplayTable(List<string> memberListView)
    {
        int columnCount = 5;
        int rowCount = (int)Math.Ceiling((double)memberListView.Count / columnCount);
        var table = new List<List<string>>();

        for (int i = 0; i < rowCount; i++)
        {
            table.Add(memberListView.Skip(i * columnCount).Take(columnCount).ToList());
        }

        // Calculate column widths
        int[] columnWidths = new int[columnCount];
        foreach (List<string> row in table)
        {
            for (int i = 0; i < row.Count; i++)
            {
                int columnWidth = row[i].Sum(c => IsDoubleWidth(c) ? 2 : 1); // Calculate width considering CJK characters
                if (columnWidths[i] < columnWidth)
                {
                    columnWidths[i] = columnWidth;
                }
            }
        }

        // Print the table
        foreach (var row in table)
        {
            for (int i = 0; i < row.Count; i++)
            {
                Console.Write(PadString(row[i], columnWidths[i] + 1)); // Pad each cell
            }
            Console.WriteLine();
        }
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

