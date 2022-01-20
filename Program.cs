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

            var changes = new List<Change>();
            changes.Add(new Change { AttributeName = "AssemblyInformationalVersion" });
            changes.Add(new Change { AttributeName = "AssemblyFileVersion" });

            var encoding = Extensions.DetectFileEncoding(path, Encoding.UTF8);
            var lines = File.ReadAllLines(path, encoding);
            var changed = false;
            var newLines = new List<string>();
            foreach (var line in lines)
            {
                var added = false;
                foreach (var change in changes.Where(c => c.NewLine == null))
                {
                    var newLine = change.UpdateAttribute(line);
                    if (newLine != null)
                    {
                        newLines.Add(newLine);
                        changed = true;
                        added = true;
                    }
                }

                if (!added)
                {
                    newLines.Add(line);
                }
            }

            foreach (var change in changes.Where(c => c.NewLine == null))
            {
                var newLine = change.CreateAttribute();
                if (newLine != null)
                {
                    newLines.Add(newLine);
                    changed = true;
                }
            }

            if (changed)
            {
                File.WriteAllLines(path, newLines.ToArray(), encoding);
            }
        }

        static void Help()
        {
            Console.WriteLine(Assembly.GetEntryAssembly().GetName().Name.ToUpperInvariant() + " <file path>");
            Console.WriteLine();
            Console.WriteLine("Description:");
            Console.WriteLine("    This tool is used to update assembly versions of a file.");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine();
            Console.WriteLine("    " + Assembly.GetEntryAssembly().GetName().Name.ToUpperInvariant() + " AssemblyInfo.cs");
            Console.WriteLine();
            Console.WriteLine("    Updates the AssemblyInformationalVersion and AssemblyFileVersion attributes in the AssemblyInfo.cs file.");
            Console.WriteLine();
        }

        private class Change
        {
            public string AttributeName { get; set; }
            public string NewLine { get; private set; }

            public virtual string CreateAttribute() => "[assembly: " + AttributeName + "(\"1.0.0.0\")]";

            public virtual string UpdateAttribute(string line)
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

            public override string ToString() => AttributeName + " => '" + NewLine + "'";
        }
    }
}
