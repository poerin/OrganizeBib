using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OrganizeBib
{
    class Program
    {
        // author,title,(booktitle|journal),(publisher|institution|school),year,volume,number,chapter,pages,edition
        static Dictionary<string, List<string>> Entries = new Dictionary<string, List<string>> {
            { "article", new List<string> {"author", "title", "journal", "year", "volume", "number", "pages"}},
            { "book", new List<string> {"author", "title", "publisher", "year", "volume", "number", "edition"}},
            { "booklet", new List<string> {"author", "title", "year"}},
            { "conference", new List<string> {"author", "title", "booktitle", "publisher", "year", "volume", "number", "pages"}},
            { "inbook", new List<string> {"author", "title", "publisher", "year", "volume", "number", "chapter", "pages", "edition"}},
            { "incollection", new List<string> {"author", "title", "booktitle", "publisher", "year", "volume", "number", "chapter", "pages", "edition"}},
            { "inproceedings", new List<string> {"author", "title", "booktitle", "publisher", "year", "volume", "number", "pages"}},
            { "manual", new List<string> {"author", "title", "year", "edition"}},
            { "mastersthesis", new List<string> {"author", "title", "school", "year"}},
            { "misc", new List<string> {"author", "title", "year"}},
            { "phdthesis", new List<string> {"author", "title", "school", "year"}},
            { "proceedings", new List<string> {"title", "publisher", "year", "volume", "number"}},
            { "techreport", new List<string> {"author", "title", "institution", "year", "number"}},
            { "unpublished", new List<string> {"author", "title", "year", "note"}}
        };

        static Dictionary<string, string> Accents = new Dictionary<string, string> {
            { "`", "\u0300"},
            { "'", "\u0301"},
            { "^", "\u0302"},
            { "~", "\u0303"},
            { "=", "\u0304"},
            { "u", "\u0306"},
            { "\\.", "\u0307"},
            { "\"", "\u0308"},
            { "r", "\u030A"},
            { "H", "\u030B"},
            { "v", "\u030C"},
            { "b", "\u0332"},
            { "d", "\u0323"},
            { "c", "\u0327"},
            { "k", "\u0328"},
        };


        [STAThread]
        static void Main(string[] arguments)
        {
            if (arguments.Length == 0) return;

            string Text = "";

            foreach (string argument in arguments)
            {
                using (StreamReader sr = new StreamReader(argument))
                {
                    Text += sr.ReadToEnd() + "\r\n";
                }
            }

            Dictionary<string, string> Results = new Dictionary<string, string>();

            Func<string, string, string> FieldText = delegate (string text, string field)
             {
                 string content = Regex.Match(text, @"\s+" + field + @"\s*=\s*({)?("")?([\s\S]*?)(?(2)"")(?(1)})(?=\s*,?\s*\n)", RegexOptions.IgnoreCase).Groups[3].Value.Trim();
                 if (content == "")
                 {
                     content = Regex.Match(text, field + @"\s*=\s*(\d+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                 }

                 string result = "";

                 if (field == "author")
                 {
                     List<string> authors = new List<string>();
                     foreach (Match matchAuthor in Regex.Matches(content, @"([\s\S]+?)(\sand\s|$)"))
                     {
                         string name = Regex.Replace(String.Join(" ", matchAuthor.Groups[1].Value.Split(',').Reverse()), @"\s+", " ").Trim();
                         name = Regex.Replace(Regex.Replace(name, @"({)\s+", "$1"), @"\s+(})", "$1");

                         foreach (var accent in Accents)
                         {
                             name = Regex.Replace(name, $"({{)?(\\\\{accent.Key})\\s*{{(\\w)}}(?(1)}})", $"$3{accent.Value}");
                             name = Regex.Replace(name, $"({{)?(\\\\{accent.Key})\\s{(Regex.IsMatch(accent.Key, @"\w") ? "+" : "*")}(\\w)(?(1)}})", $"$3{accent.Value}");
                         }

                         name = name.Normalize(System.Text.NormalizationForm.FormC);
                         authors.Add(name);
                     }
                     result = String.Join(" and ", authors);
                 }
                 else if (field == "pages")
                 {
                     result = Regex.Replace(Regex.Replace(content, @"[-‐‑‒–—―]+", "--"), @"\s+", " ").Trim();
                 }
                 else
                 {
                     result = Regex.Replace(content, @"\s+", " ").Trim();
                 }

                 return result;
             };

            foreach (Match text in Regex.Matches(Text, @"@(article|book|booklet|conference|inbook|incollection|inproceedings|manual|mastersthesis|misc|phdthesis|proceedings|techreport|unpublished)\s*{[^@]+}", RegexOptions.IgnoreCase))
            {
                List<string> fields = new List<string>();
                string type = "", author = "", year = "";
                foreach (var entry in Entries)
                {
                    if (text.Value.StartsWith("@" + entry.Key, true, System.Globalization.CultureInfo.InvariantCulture))
                    {
                        type = entry.Key;
                        foreach (var field in entry.Value)
                        {
                            string fieldText = FieldText(text.Value, field);

                            if (field == "author")
                            {
                                string name = Regex.Match(fieldText, @"([\s\S]+?)(\sand\s|$)").Groups[1].Value;
                                name = Regex.Replace(name, @"\\(i|j|ij|IJ|l|L|o|O|aa|AA|ae|AE|oe|OE|ss)\s+", "$1");
                                name = Regex.Replace(name, @"\\(i|j|ij|IJ|l|L|o|O|aa|AA|ae|AE|oe|OE|ss)(\W+)", "$1$2");
                                name = Regex.Replace(Regex.Replace(name, @"({)?\\(\w+|\W)\s*{(\S+)}(?(1)})", "$3"), @"({)?\\(\w+\s+|\W\s*)(\S+)(?(1)})", "$3");

                                author = Regex.Replace(Regex.Match(name, @"(^|\s)({([\s\S]+?)})$", RegexOptions.RightToLeft).Groups[3].Value, @"\s", "");
                                if (author == "")
                                {
                                    author = Regex.Match(name, @"\S+$", RegexOptions.RightToLeft).Value;
                                }
                            }

                            if (field == "year")
                            {
                                year = fieldText;
                            }

                            if (fieldText != "")
                            {
                                fields.Add($"  {field} = {{{fieldText}}}");
                            }
                        }
                        break;
                    }
                }

                string key = $"{author}{year}{type}";
                int index = 1;

                while (Results.ContainsKey(key))
                {
                    key = $"{author}{year}{type}.{index++}";
                }
                Results.Add(key, $"@{type}{{{author}{year},\n{String.Join(",\n", fields)}\n}}");
            }

            Clipboard.SetText(String.Join("\n\n", from result in Results orderby result.Key select result.Value) + "\n\n");
        }
    }
}
