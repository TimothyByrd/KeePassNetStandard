using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using KeePassLib;
using KeePassLib.Collections;
using KeePassNetStandard;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace SampleDumpDatabase
{
    class Program
    {
        private const string SectionSeparator = "****************************************";
        private const string EntrySeparator = "----------------------------------------";

        static void Main(string[] args)
        {
            var masterPassword = "master_password";
            var dbpath = ".\\SampleDatabase.kdbx";

            bool nextArgIsDbPath = false;
            bool nextArgIsMastetPassword = false;
            bool createWordDoc = false;
            foreach (var arg in args)
            {
                if (nextArgIsDbPath)
                {
                    dbpath = arg;
                    nextArgIsDbPath = false;
                }
                else if (nextArgIsMastetPassword)
                {
                    masterPassword = arg;
                    nextArgIsMastetPassword = false;
                }
                else if (string.Equals(arg, "-db", StringComparison.Ordinal))
                    nextArgIsDbPath = true;
                else if (string.Equals(arg, "-pw", StringComparison.Ordinal))
                    nextArgIsMastetPassword = true;
                else if (string.Equals(arg, "-w", StringComparison.Ordinal))
                    createWordDoc = true;
            }

            if (!File.Exists(dbpath))
            {
                Console.WriteLine($"Can't find KeePass database '{dbpath}'");
                return;
            }
            Console.WriteLine($"Opening '{dbpath}'");

            var db = KeePassUtilities.OpenPasswordDatabase(dbpath, masterPassword);

            Console.WriteLine("Enumerating groups and entries:");

            var sb = new StringBuilder();

            WriteGroupEntries(sb, db.RootGroup.Entries, "Root entries");

            foreach (var group in db.RootGroup.Groups)
                WriteGroup(sb, group, "");

            Console.Write(sb);

            if (createWordDoc)
                CreateWordDocument(dbpath, sb);

            Console.WriteLine("Done!");
        }

        //private static void WriteGroup(PwGroup group, string baseGroupName)
        //{
        //    var entries = group.Entries;
        //    var groupName = string.IsNullOrWhiteSpace(baseGroupName) ? group.Name : $"{baseGroupName}/{group.Name}";

        //    WriteGroupEntries(entries, groupName);

        //    foreach (var subgroup in group.Groups)
        //    {
        //        WriteGroup(subgroup, groupName);
        //    }
        //}

        //private static void WriteGroupEntries(PwObjectList<PwEntry> entries, string groupName)
        //{
        //    if (entries.Any())
        //    {
        //        var separator = SectionSeparator;
        //        Console.WriteLine(separator);
        //        Console.WriteLine($"Group: {groupName}");

        //        foreach (var entry in entries.OrderBy(x => x.Strings.ReadSafe("Title")))
        //        {
        //            Console.WriteLine(separator);
        //            ConsoleWriteEntry(entry);
        //            separator = EntrySeparator;
        //        }
        //    }
        //}

        //private static void ConsoleWriteEntry(PwEntry entry)
        //{
        //    var title = entry.Strings.ReadSafe("Title");
        //    var user = entry.Strings.ReadSafe("UserName");
        //    var password = entry.Strings.ReadSafe("Password");
        //    var url = entry.Strings.ReadSafe("URL");
        //    var notes = entry.Strings.ReadSafe("Notes");
        //    Console.WriteLine($"{title}");
        //    Console.WriteLine($"user: '{user}', pass: '{password}'");
        //    if (!string.IsNullOrWhiteSpace(url))
        //        Console.WriteLine($"url: '{url}'");
        //    if (!string.IsNullOrWhiteSpace(notes))
        //        Console.WriteLine($"{notes.Trim()}");
        //}

        private static void WriteGroup(StringBuilder sb, PwGroup group, string baseGroupName)
        {
            if (group == null || group.Name == "Old" || group.Name == "Recycle Bin")
                return;
            var entries = group.Entries;
            var groupName = string.IsNullOrWhiteSpace(baseGroupName) ? group.Name : $"{baseGroupName}/{group.Name}";

            WriteGroupEntries(sb, entries, groupName);

            foreach (var subgroup in group.Groups)
            {
                WriteGroup(sb, subgroup, groupName);
            }
        }
        
        private static void WriteGroupEntries(StringBuilder sb, PwObjectList<PwEntry> entries, string groupName)
        {
            if (entries.Any())
            {
                var separator = SectionSeparator;
                sb.AppendLine(separator);
                sb.AppendLine($"Group: {groupName}");

                foreach (var entry in entries.OrderBy(x => x.Strings.ReadSafe("Title")))
                {
                    sb.AppendLine(separator);
                    WriteEntry(sb, entry);
                    separator = EntrySeparator;
                }
            }
        }
        
        private static void WriteEntry(StringBuilder sb, PwEntry entry)
        {
            var title = entry.Strings.ReadSafe("Title");
            var user = entry.Strings.ReadSafe("UserName");
            var password = entry.Strings.ReadSafe("Password");
            var url = entry.Strings.ReadSafe("URL");
            var notes = entry.Strings.ReadSafe("Notes")?.Trim();
            sb.AppendLine($"{title}");
            sb.AppendLine($"user: {user}, pass: {password}");
            if (!string.IsNullOrWhiteSpace(url))
                sb.AppendLine($"url: {url}");
            if (!string.IsNullOrWhiteSpace(notes))
                sb.AppendLine($"{notes.Trim()}");
        }

        private static void CreateWordDocument(string dbpath, StringBuilder sb)
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd");
            var extension = $".{now}.docx";
            var docPath = Path.ChangeExtension(dbpath, extension);
            if (File.Exists(docPath))
                File.Delete(docPath);
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(docPath, WordprocessingDocumentType.Document))
            {
                // Add a main document part. 
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();

                // Create the document structure and add some text.
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                AppendParagraph(body, $"Passwords {now}");

                var lines = sb.ToString().Trim().Split('\n');
                foreach (var line in lines)
                    AppendParagraph(body, line);

                var sectionProps = new SectionProperties();
                mainPart.Document.Body.Append(sectionProps);

                var pageMargin = new PageMargin
                {
                    Top = 720, // 1/20ths of a point so 720 = 1/2 inch
                    Right = (UInt32Value)720U, 
                    Bottom = 720, 
                    Left = (UInt32Value)720U, 
                    Header = (UInt32Value)360U, 
                    Footer = (UInt32Value)360U, 
                    Gutter = (UInt32Value)0U 
                };
                sectionProps.Append(pageMargin);

                var paragraphColumns = new Columns
                {
                    EqualWidth = true,
                    ColumnCount = 2
                };
                sectionProps.Append(paragraphColumns);
            }

            static void AppendParagraph(Body body, string str)
            {
                Paragraph para = body.AppendChild(new Paragraph());

                var spacing = new SpacingBetweenLines() { Line = "240", LineRule = LineSpacingRuleValues.Auto, Before = "0", After = "0" };
                var paragraphProperties = new ParagraphProperties();
                paragraphProperties.Append(spacing);
                para.Append(paragraphProperties);

                Run run = para.AppendChild(new Run());

                var runProp = new RunProperties();
                var runFont = new RunFonts { Ascii = "Arial" };
                var size = new FontSize { Val = new StringValue("14") }; // size in half-points
                runProp.Append(runFont);
                runProp.Append(size);
                run.PrependChild<RunProperties>(runProp);

                if (string.IsNullOrEmpty(str)) str = ".";
                run.AppendChild(new Text(str));

            }
        }
    }
}
