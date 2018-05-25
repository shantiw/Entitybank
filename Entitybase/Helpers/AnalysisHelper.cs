using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XData.Data.Helpers
{
    internal static class AnalysisHelper
    {
        // {e1e0601c-7183-482f-955d-dfb8e36a194c}
        public const string GuidPattern = @"\{[a-fA-F0-9]{8}(-[a-fA-F0-9]{4}){3}-[a-fA-F0-9]{12}\}";

        // outermost (...)
        public const string ParenthesesPairPattern = @"\([^\(\)]*(((?'Open'\()[^\(\)]*)+((?'-Open'\))[^\(\)]*)+)*(?(Open)(?!))\)";

        // single quote "''" // 'I''m  fine' // '''Im fine' // 'Im fine'''
        public static string EncodeString(string value, out Dictionary<string, string> placeholders)
        {
            placeholders = new Dictionary<string, string>();

            string val = value + ((char)32).ToString();
            while (val.IndexOf('\'') != -1)
            {
                val = EncodeString(val, placeholders);
            }
            val = val.Substring(0, val.Length - 1);

            return val;
        }

        private static string EncodeString(string val, Dictionary<string, string> placeholders)
        {
            int start = val.IndexOf('\'');
            if (start == -1) return val;

            int index = val.IndexOf('\'', start + 1);
            while (index != -1)
            {
                if (val[index + 1] == '\'')
                {
                    index = val.IndexOf('\'', index + 2);
                }
                else
                {
                    break;
                }
            }
            if (index == -1) throw new SyntaxErrorException(HelpersMessages.UnclosedQuotation);

            string constant = val.Substring(start, index - start + 1);
            string guid = GetGuid();
            placeholders.Add(guid, constant);

            val = val.Substring(0, start) + guid + val.Substring(index + 1);
            return val;
        }

        public static string DecodeString(string value, Dictionary<string, string> placeholders)
        {
            string result = Regex.Replace(value, GuidPattern, m =>
            {
                if (placeholders.ContainsKey(m.Value))
                {
                    return placeholders[m.Value];
                }

                return m.Value;
            });

            return result;
        }

        public static string GetGuid()
        {
            return Guid.NewGuid().ToString("B");
        }


    }
}
