/*
 * This file only is:
 * 
 * MIT License
 * 
 * Copyright 2021 Timothy Byrd
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
 * is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies
 * or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
 * FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT
 * OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
 * OR OTHER DEALINGS IN THE SOFTWARE.
 */

using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Serialization;

namespace KeePassNetStandard
{
    /// <summary>
    /// Static class of utility methods to ease accessing a KeePass database
    /// </summary>
    public static class KeePassUtilities
    {
        /// <summary>
        /// Given the path to a KeePass password database file
        /// and a password in an UFT-8 encoded byte array,
        /// open the database.
        /// Only works for databases that are only keyed by a master password.
        /// </summary>
        /// <param name="databasePath">The path to the KeePass password database file</param>
        /// <param name="masterBytes">The master password for the database</param>
        /// <returns></returns>
        public static PwDatabase OpenPasswordDatabase(string databasePath, byte[] masterBytes)
        {
            return OpenPasswordDatabase(databasePath, System.Text.Encoding.UTF8.GetString(masterBytes));
        }

        /// <summary>
        /// Given the path to a KeePass password database file and a password, open the database.
        /// Only works for databases that are only keyed by a master password.
        /// </summary>
        /// <param name="databasePath">The path to the KeePass password database file</param>
        /// <param name="masterPassword">The master password for the database</param>
        /// <returns></returns>
        public static PwDatabase OpenPasswordDatabase(string databasePath, string masterPassword)
        {
            var ioConnInfo = new IOConnectionInfo { Path = databasePath };
            var compKey = new CompositeKey();
            compKey.AddUserKey(new KcpPassword(masterPassword));

            var db = new KeePassLib.PwDatabase();
            db.Open(ioConnInfo, compKey, null);
            return db;
        }

        /// <summary>
        /// Given the path to a KeePass password database file,
        /// a password in an UFT-8 encoded byte array,
        /// a group name, an entry name and a user name, get the password.
        /// This method is for when you have multiple entries with the same title, but different users.
        /// </summary>
        /// <param name="databasePath">The path to the KeePass password database file</param>
        /// <param name="masterBytes">The master password for the database</param>
        /// <param name="groupName">The name of the group to look in. If null/empty the rood group is used.</param>
        /// <param name="entryName">The title of the entry to find.</param>
        /// <param name="userName">The user name to match.</param>
        /// <returns>The password or null if not found.</returns>
        public static string GetPassword(string databasePath, byte[] masterBytes, string groupName, string entryName, string userName)
        {
            var db = OpenPasswordDatabase(databasePath, masterBytes);
            return db.GetPassword(groupName, entryName, userName);
        }


        /// <summary>
        /// Given a password database, an entry name and a user name, get the password from the root group.
        /// This method is for when you have multiple entries with the same title, but different users.
        /// </summary>
        /// <param name="passwordDb">The password database to look in.</param>
        /// <param name="entryName">The title of the entry to find.</param>
        /// <param name="userName">The user name to match.</param>
        /// <returns>The password or null if not found.</returns>
        public static string GetPassword(this PwDatabase passwordDb, string entryName, string userName)
        {
            return passwordDb.GetPassword(null, entryName, userName);
        }

        /// <summary>
        /// Given a password database, a group name, an entry name and a user name, get the password.
        /// This method is for when you have multiple entries with the same title, but different users.
        /// </summary>
        /// <param name="passwordDb">The password database to look in.</param>
        /// <param name="groupName">The name of the group to look in. If null/empty the rood group is used.</param>
        /// <param name="entryName">The title of the entry to find.</param>
        /// <param name="userName">The user name to match.</param>
        /// <returns>The password or null if not found.</returns>
        public static string GetPassword(this PwDatabase passwordDb, string groupName, string entryName, string userName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                var group = passwordDb.RootGroup;
                return group.GetPassword(entryName, userName);
            }

            foreach (var group in passwordDb.RootGroup.Groups)
            {
                if (group.Name != groupName)
                    continue;
                return group.GetPassword(entryName, userName);
            }
            return null;
        }

        /// <summary>
        /// Given a password group, an entry name and a user name, get the password.
        /// This method is for when you have multiple entries with the same title, but different users.
        /// </summary>
        /// <param name="group">The password group to look in.</param>
        /// <param name="entryName">The title of the entry to find.</param>
        /// <param name="userName">The user name to match.</param>
        /// <returns>The password or null if not found.</returns>
        public static string GetPassword(this PwGroup group, string entryName, string userName)
        {
            foreach (var entry in group.Entries)
            {
                var title = entry.Strings.ReadSafe("Title");
                var user = entry.Strings.ReadSafe("UserName");
                if (title == entryName && user == userName)
                    return entry.Strings.ReadSafe("Password");
            }
            return null;
        }

        /// <summary>
        /// Given a password database and an entry name,
        /// get the user and password for the first matching entry from the root group.
        /// </summary>
        /// <param name="passwordDb">The password database to look in.</param>
        /// <param name="entryName">The title of the entry to find.</param>
        /// <returns>The user and password or nulls if not found.</returns>
        public static (string user, string password) GetUserAndPassword(this PwDatabase passwordDb, string entryName)
        {
            return passwordDb.GetUserAndPassword(null, entryName);
        }

        /// <summary>
        /// Given a password database, a group name and an entry name,
        /// get the user and password for the first matching entry from the named group.
        /// </summary>
        /// <param name="passwordDb">The password database to look in.</param>
        /// <param name="groupName">The name of the group to look in. If null/empty the rood group is used.</param>
        /// <param name="entryName">The title of the entry to find.</param>
        /// <returns>The user and password or nulls if not found.</returns>
        public static (string user, string password) GetUserAndPassword(this PwDatabase passwordDb, string groupName, string entryName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                var group = passwordDb.RootGroup;
                return group.GetUserAndPassword(entryName);
            }

            foreach (var group in passwordDb.RootGroup.Groups)
            {
                if (group.Name != groupName)
                    continue;
                return group.GetUserAndPassword(entryName);
            }
            return (null, null);
        }

        /// <summary>
        /// Given a password group and an entry name, get the user and password for the first matching entry.
        /// </summary>
        /// <param name="group">The password group to look in.</param>
        /// <param name="entryName">The title of the entry to find.</param>
        /// <returns>The user and password or nulls if not found.</returns>
        public static (string user, string password) GetUserAndPassword(this PwGroup group, string entryName)
        {
            foreach (var entry in group.Entries)
            {
                var title = entry.Strings.ReadSafe("Title");
                if (title == entryName)
                    return (entry.Strings.ReadSafe("UserName"), entry.Strings.ReadSafe("Password"));
            }
            return (null, null);
        }
    }
}
