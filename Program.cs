using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace UpdateAssemblyInfo
{
    class Program
    {
        static void Main()
        {
            if (Debugger.IsAttached)
            {
                RealMain();
            }
            else
            {
                try
                {
                    RealMain();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        static void RealMain()
        {
            Console.WriteLine("UpdateAssemblyInfo - Copyright (C) 2021-" + DateTime.Now.Year + " Simon Mourier. All rights reserved.");
            Console.WriteLine();

            var path = CommandLine.GetNullifiedArgument(0);
            if (CommandLine.HelpRequested || path == null)
            {
                Help();
                return;
            }

            var commitDate = CommandLine.GetArgument("commitDate", true);
            var fileVersion = CommandLine.GetArgument("fileVersion", true);
            var informationalVersion = CommandLine.GetArgument("informationalVersion", true);

            var changes = new List<Change>();
            if (fileVersion)
            {
                changes.Add(new VersionChange { AttributeName = "AssemblyFileVersion" });
            }

            if (informationalVersion)
            {
                changes.Add(new VersionChange { AttributeName = "AssemblyInformationalVersion" });
            }

            if (commitDate)
            {
                changes.Add(new CommitDateChange());
            }

            if (changes.Count == 0)
            {
                Help();
                return;
            }

            var encoding = Extensions.DetectFileEncoding(path, Encoding.UTF8);
            var lines = File.ReadAllLines(path, encoding);
            var changed = false;
            var newLines = new List<(string, bool)>();
            foreach (var line in lines)
            {
                var added = false;
                foreach (var change in changes.Where(c => c.NewLine == null))
                {
                    var newLine = change.UpdateAttribute(line);
                    if (newLine != null)
                    {
                        newLines.Add((newLine, true));
                        changed = true;
                        added = true;
                    }
                }

                if (!added)
                {
                    newLines.Add((line, false));
                }
            }

            foreach (var change in changes.Where(c => c.NewLine == null))
            {
                var newLine = change.CreateAttribute();
                if (newLine != null)
                {
                    newLines.Add((newLine, true));
                    changed = true;
                }
            }

            if (changed)
            {
                var changedLines = newLines.Where(n => n.Item2).ToArray();
                Console.WriteLine(changedLines.Length + " line(s) were added or changed:");
                Console.WriteLine();
                foreach (var newLine in changedLines)
                {
                    Console.WriteLine(newLine.Item1);
                }
                File.WriteAllLines(path, newLines.Select(n => n.Item1).ToArray(), encoding);
            }
        }

        static void Help()
        {
            Console.WriteLine(Assembly.GetEntryAssembly().GetName().Name.ToUpperInvariant() + " <file path> [options]");
            Console.WriteLine();
            Console.WriteLine("Description:");
            Console.WriteLine("    This tool is used to update assembly versions of a file.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine();
            Console.WriteLine("    /fileVersion:<bool>             Update or create AssemblyFileVersion assembly attribute. Default value is true.");
            Console.WriteLine("    /informationalVersion:<bool>    Update or create AssemblyInformationalVersion assembly attribute. Default value is true.");
            Console.WriteLine("    /commitDate:<bool>              Update or create AssemblyMetadata(\"Commit Date\") assembly attribute. Default value is true.");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine();
            Console.WriteLine("    " + Assembly.GetEntryAssembly().GetName().Name.ToUpperInvariant() + " AssemblyInfo.cs");
            Console.WriteLine();
            Console.WriteLine("    Updates AssemblyInformationalVersion, AssemblyFileVersion and AssemblyMetadata(\"Commit Date\") attributes in the AssemblyInfo.cs file.");
            Console.WriteLine();
        }

        private class CommitDateChange : Change
        {
            public override string AttributeName => "AssemblyMetadata";

            private const string _key = "Commit Date";
            public override string CreateAttribute() => "[assembly: " + AttributeName + "(\"" + _key + "\", \"" + DateTime.UtcNow.ToString("R") + "\")]";

            public override string UpdateAttribute(string line)
            {
                UpdateAttribute(line, AttributeName);
                if (NewLine != null)
                    return NewLine;

                UpdateAttribute(line, AttributeName + "Attribute");
                return NewLine;
            }

            private void UpdateAttribute(string line, string name)
            {
                var trim = line.TrimStart();
                if (!trim.StartsWith("[assembly: "))
                    return;

                var startToken = name + "(\"";
                var start = line.IndexOf(startToken);
                if (start < 0)
                    return;

                var endToken = "\")]";
                var end = line.IndexOf(endToken, start + startToken.Length);
                if (end < 0)
                    return;

                var content = line.Substring(start + startToken.Length, end - start - startToken.Length);
                if (!content.StartsWith(_key))
                    return;

                var newName = DateTime.UtcNow.ToString("R");
                NewLine = line.Substring(0, start) + name + "(\"" + _key + "\", \"" + newName + endToken + line.Substring(end + endToken.Length);
            }
        }

        private class VersionChange : Change
        {
            public override string CreateAttribute() => "[assembly: " + AttributeName + "(\"1.0.0.0\")]";
            public override string UpdateAttribute(string line)
            {
                UpdateAttribute(line, AttributeName);
                if (NewLine != null)
                    return NewLine;

                UpdateAttribute(line, AttributeName + "Attribute");
                return NewLine;
            }

            private void UpdateAttribute(string line, string name)
            {
                var trim = line.TrimStart();
                if (!trim.StartsWith("[assembly: "))
                    return;

                var startToken = name + "(\"";
                var start = line.IndexOf(startToken);
                if (start < 0)
                    return;

                var endToken = "\")]";
                var end = line.IndexOf(endToken, start + startToken.Length);
                if (end < 0)
                    return;

                var content = line.Substring(start + startToken.Length, end - start - startToken.Length);
                if (!Version.TryParse(content, out var version))
                {
                    version = new Version(1, 0, 0, 0);
                }
                else
                {
                    version = new Version(version.Major, version.Minor, version.Build, version.Revision + 1);
                }

                var newName = version.ToString();
                var newLine = line.Substring(0, start) + name + "(\"" + newName + endToken + line.Substring(end + endToken.Length);
                if (newLine == line)
                    return; // unchanged

                NewLine = newLine;
            }
        }

        private abstract class Change
        {
            public virtual string AttributeName { get; set; }
            public string NewLine { get; protected set; }

            public abstract string CreateAttribute();
            public abstract string UpdateAttribute(string line);
            public override string ToString() => AttributeName + " => '" + NewLine + "'";
        }
    }
}
