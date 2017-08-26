using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XData.Data.Helpers
{
    public static class CaseFormat
    {
        // PascalCase
        public static string UnderscoreToUpperCamel(this string name)
        {
            string value = "_" + name;
            string pattern = @"_[^_]+";
            string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
            {
                string s = m.Value;
                s = s.Substring(1).ToLower();
                s = s[0].ToString().ToUpper() + s.Substring(1);
                return s;
            }));

            return result;
        }

        // camelCase
        public static string UnderscoreToLowerCamel(this string name)
        {
            string value = UnderscoreToUpperCamel(name);
            string s = value.TrimStart('_');
            s = s[0].ToString().ToLower() + s.Substring(1);
            s = new string('_', value.Length - s.Length) + s;

            return s;
        }


    }
}
