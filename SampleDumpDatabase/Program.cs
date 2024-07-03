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
using System.Collections.Generic;

namespace SampleDumpDatabase
{
    class Program
    {
        private const string SectionSeparator = "****************************************";
        private const string EntrySeparator = "----------------------------------------";

        static void Main(string[] args)
        {
            var masterPassword = "master_password";
            var dbPath = ".\\SampleDatabase.kdbx";

            bool nextArgIsDbPath = false;
            bool nextArgIsMasterPassword = false;
            bool createWordDoc = false;
            foreach (var arg in args)
            {
                if (nextArgIsDbPath)
                {
                    dbPath = arg;
                    nextArgIsDbPath = false;
                }
                else if (nextArgIsMasterPassword)
                {
                    masterPassword = arg;
                    nextArgIsMasterPassword = false;
                }
                else if (string.Equals(arg, "-db", StringComparison.Ordinal))
                    nextArgIsDbPath = true;
                else if (string.Equals(arg, "-pw", StringComparison.Ordinal))
                    nextArgIsMasterPassword = true;
                else if (string.Equals(arg, "-w", StringComparison.Ordinal))
                    createWordDoc = true;
            }

            if (!File.Exists(dbPath))
            {
                Console.WriteLine($"Can't find KeePass database '{dbPath}'");
                return;
            }
            Console.WriteLine($"Opening '{dbPath}'");

            var db = KeePassUtilities.OpenPasswordDatabase(dbPath, masterPassword);

            Console.WriteLine("Enumerating groups and entries:");

            var lines = new List<string>();

            WriteGroupEntries(lines, db.RootGroup.Entries, "Root entries");

            foreach (var group in db.RootGroup.Groups)
                WriteGroup(lines, group, "");

            foreach (var line in lines)
                Console.WriteLine(line[1..]);

            if (createWordDoc)
                CreateWordDocument(dbPath, lines);

            Console.WriteLine();
            Console.WriteLine("Done!");
        }

        private static void WriteGroup(List<string> lines, PwGroup group, string baseGroupName)
        {
            if (group == null || group.Name == "Old" || group.Name == "Recycle Bin")
                return;
            var entries = group.Entries;
            var groupName = string.IsNullOrWhiteSpace(baseGroupName) ? group.Name : $"{baseGroupName}/{group.Name}";

            WriteGroupEntries(lines, entries, groupName);

            foreach (var subgroup in group.Groups)
            {
                WriteGroup(lines, subgroup, groupName);
            }
        }
        
        private static void WriteGroupEntries(List<string> lines, PwObjectList<PwEntry> entries, string groupName)
        {
            if (entries.Any())
            {
                lines.Add($"-{SectionSeparator}");
                lines.Add($"+Group: {groupName}");
                
                var groupingChar = '+';
                var separator = SectionSeparator;
                foreach (var entry in entries.OrderBy(x => x.Strings.ReadSafe("Title")))
                {
                    
                    lines.Add($"{groupingChar}{separator}");
                    WriteEntry(lines, entry, groupingChar);
                    groupingChar = '-';
                    separator = EntrySeparator;
                }
            }
        }

        private static void WriteEntry(List<string> lines, PwEntry entry, char initialGroupingChar)
        {
            var title = entry.Strings.ReadSafe("Title");
            var user = entry.Strings.ReadSafe("UserName");
            var password = entry.Strings.ReadSafe("Password");
            var url = entry.Strings.ReadSafe("URL");
            var notes = entry.Strings.ReadSafe("Notes")?.Trim();
            lines.Add($"{initialGroupingChar}{title}");
            lines.Add($"+user: {user}, pass: {password}");
            if (!string.IsNullOrWhiteSpace(url))
                lines.Add($"+url: {url}");
            if (!string.IsNullOrWhiteSpace(notes))
            {
                var noteLines = notes.Split('\n');
                foreach (var line in noteLines)
                   lines.Add($"+{line.Trim()}");
            }
        }

        private static void CreateWordDocument(string dbPath, List<string> lines)
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd");
            var extension = $".{now}.docx";
            var docPath = Path.ChangeExtension(dbPath, extension);
            if (File.Exists(docPath))
                File.Delete(docPath);
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(docPath, WordprocessingDocumentType.Document))
            {
                // Add a main document part. 
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();

                // Create the document structure and add some text.
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                var paragraphLines = new List<string>
                {
                    $"Passwords {now}"
                };

                foreach (var line in lines)
                {
                    var groupingChar = line[0];
                    var lineText = line[1..];
                    if (groupingChar == '-' && paragraphLines.Count > 0)
                    {
                        WriteParagraph(body, paragraphLines);
                        paragraphLines.Clear();
                    }
                    paragraphLines.Add(lineText);
                }

                if (paragraphLines.Count > 0)
                    WriteParagraph(body, paragraphLines);

                    //AppendParagraph(body, line);

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

            static void WriteParagraph(Body body, List<string> lines)
            {
                Paragraph para = body.AppendChild(new Paragraph());

                var spacing = new SpacingBetweenLines() { Line = "240", LineRule = LineSpacingRuleValues.Auto, Before = "0", After = "0" };
                var paragraphProperties = new ParagraphProperties
                {
                    KeepLines = new()
                };
                paragraphProperties.Append(spacing);
                para.Append(paragraphProperties);

                Run run = para.AppendChild(new Run());

                var runProp = new RunProperties();
                var runFont = new RunFonts { Ascii = "Arial" };
                var size = new FontSize { Val = new StringValue("14") }; // size in half-points
                runProp.Append(runFont);
                runProp.Append(size);
                run.PrependChild<RunProperties>(runProp);

                var addBreak = false;
                foreach (var line in lines)
                {
                    if (addBreak)
                        run.AppendChild(new Break());
                    addBreak = true;
                    if (line.Length > 0)
                        run.AppendChild(new Text(line));
                }
            }
        }
    }
}
