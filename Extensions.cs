﻿using System;
using System.IO;
using System.Text;

namespace UpdateAssemblyInfo
{
    public static class Extensions
    {
        public static bool EqualsIgnoreCase(this string str, string text, bool trim = false)
        {
            if (trim)
            {
                str = str.Nullify();
                text = text.Nullify();
            }

            if (str == null)
                return text == null;

            if (text == null)
                return false;

            if (str.Length != text.Length)
                return false;

            return string.Compare(str, text, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static string Nullify(this string str)
        {
            if (str == null)
                return null;

            if (string.IsNullOrWhiteSpace(str))
                return null;

            var t = str.Trim();
            return t.Length == 0 ? null : t;
        }

        public static Encoding DetectFileEncoding(string filePath, Encoding defaultEncodingIfNoBom = null)
        {
            defaultEncodingIfNoBom = defaultEncodingIfNoBom ?? Encoding.Default;
            using (var reader = new StreamReader(filePath, defaultEncodingIfNoBom, true))
            {
                reader.Peek();
                return reader.CurrentEncoding;
            }
        }
    }
}
