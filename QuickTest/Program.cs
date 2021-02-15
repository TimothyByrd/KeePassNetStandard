using System;
using System.IO;

using KeePassNetStandard;

namespace QuickTest
{
    class Program
    {
        static void Main()
        {
            var masterPassword = "master_password";
            var dbpath = ".\\SampleDatabase.kdbx";
            if (!File.Exists(dbpath))
            {
                Console.WriteLine($"Can't find KeePass database '{dbpath}'");
                return;
            }
            Console.WriteLine($"Opening '{dbpath}'");

            var db = KeePassUtilities.OpenPasswordDatabase(dbpath, masterPassword);

            Console.WriteLine();
            Console.WriteLine("Getting a couple known passwords:");

            var rootEntryPassword = db.GetPassword("RootEntry", "RootUser");
            if (rootEntryPassword == "root_password")
                Console.WriteLine("Successfully got password for RootEntry");
            else
                Console.WriteLine($"*** Failed to get password for RootEntry - got '{rootEntryPassword}'");

            var internetEntryPassword = db.GetPassword("Internet", "InternetEntry", "InternetUser");
            if (internetEntryPassword == "internet_password")
                Console.WriteLine("Successfully got password for InternetEntry");
            else
                Console.WriteLine($"*** Failed to get password for InternetEntry - got '{internetEntryPassword}'");

            Console.WriteLine();
            Console.WriteLine("Enumerating groups and entries:");

            foreach (var entry in db.RootGroup.Entries)
            {
                ConsoleWriteEntry(entry, "Root entry");
            }

            foreach (var group in db.RootGroup.Groups)
            {
                var prefix = $"Entry in '{group.Name}'";
                foreach (var entry in group.Entries)
                {
                    ConsoleWriteEntry(entry, prefix);
                }
            }
            Console.WriteLine("Done!");
        }

        private static void ConsoleWriteEntry(KeePassLib.PwEntry entry, string prefixText)
        {
            var title = entry.Strings.ReadSafe("Title");
            var user = entry.Strings.ReadSafe("UserName");
            var password = entry.Strings.ReadSafe("Password");
            Console.WriteLine($"{prefixText}: '{title}', user: '{user}', pass: '{password}'");
        }
    }
}
