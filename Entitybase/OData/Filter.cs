using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;
using XData.Data.Helpers;
using System.Data;

namespace XData.Data.OData
{
    public class Filter
    {
        public Segment Segment { get; private set; }

        private List<string> _parameters = new List<string>();
        public IEnumerable<string> Parameters { get => _parameters.Distinct(); }

        private List<string> _properties = new List<string>();
        public IEnumerable<string> Properties { get => _properties; }

        protected static readonly string[] Operators = new string[]
        {
            // Comparison Operators
            "eq", "ne", "gt", "ge", "lt", "le", "has",
            // Logical Operators
            "and", "or", "not",
            // Arithmetic Operators
            "add", "sub", "mul", "div", "mod",
            // Grouping Operators
            //"()"
        };

        protected static readonly string[] Functions = new string[]
        {
            // String Functions
            "contains", "endswith", "startswith", "length", "indexof", "substring", "tolower", "toupper", "trim", "concat",
            // Date Functions
            "year", "month", "day", "hour", "minute", "second", "fractionalseconds", "date", "time", "totaloffsetminutes", "now", "mindatetime", "maxdatetime",
            // Math Functions
            "round", "floor", "ceiling",
            // Type Functions
            "cast", "isof", //"isof",
            // Geo Functions
            "geo.distance", "geo.length", "geo.intersects"
        };

        protected static readonly string[] ParamlessFuncs = new string[]
        {
            "now", "mindatetime", "maxdatetime"
        };

        protected static readonly string[] UnaryFuncs = new string[]
        {
            "length", "tolower", "toupper", "trim",
            "year", "month", "day", "hour", "minute", "second", "fractionalseconds", "date", "time", "totaloffsetminutes",
            "round", "floor", "ceiling",
            //"isof",
            "geo.length"
        };

        protected static readonly string[] BinaryFuncs = new string[]
        {
            "contains", "endswith", "startswith", "indexof", "substring", "concat",
            "cast",
            //"isof",
            "geo.distance", "geo.intersects"
        };

        // {e1e0601c-7183-482f-955d-dfb8e36a194c}
        protected const string GuidPattern = AnalysisHelper.GuidPattern;

        // outermost (...)
        protected const string ParenthesesPairPattern = AnalysisHelper.ParenthesesPairPattern;

        protected static readonly string FuncPattern;

        static Filter()
        {
            FuncPattern = string.Format(@"\b({0})\s*", string.Join("|", Functions));
        }

        protected readonly string Value;
        protected readonly string Entity;
        protected readonly XElement Schema;

        // guid, datetime'2017-01-12 12:21:31' or 'I''m fine'
        protected readonly Dictionary<string, string> ConstantPlaceholders;

        public Filter(string value, string entity, XElement schema)
        {
            Value = value;
            Entity = entity;
            Schema = schema;

            //
            string val = Value;
            val = EncodeStringConstants(val, out ConstantPlaceholders);
            val = EncodeDateTimeConstants(val);
            Segment = Compose(val);
        }

        // single quote "''" // 'I''m  fine' // '''Im fine' // 'Im fine'''
        protected static string EncodeStringConstants(string value, out Dictionary<string, string> placeholders)
        {
            return AnalysisHelper.EncodeString(value, out placeholders);
        }

        protected string EncodeDateTimeConstants(string value)
        {
            string val = value;

            string pattern = @"\bdatetime\s*{\w{8}-\w{4}-\w{4}-\w{4}-\w{12}}";
            val = Regex.Replace(val, pattern, new MatchEvaluator(m =>
            {
                string guid = m.Value.Substring("datetime".Length).Trim();
                ConstantPlaceholders[guid] = "datetime" + ConstantPlaceholders[guid];
                return guid;
            }));

            return val;
        }

        protected string DecodeConstant(string value)
        {
            return DecodeConstant(value, ConstantPlaceholders);
        }

        protected static string DecodeConstant(string value, Dictionary<string, string> placeholders)
        {
            return AnalysisHelper.DecodeString(value, placeholders);
        }

        protected Segment Compose(string value)
        {
            List<Segment> list = new List<Segment>();

            Dictionary<int, int> dict = Group(value);
            if (dict.Count == 0)
            {
                list.AddRange(Split(value));
            }
            else
            {
                int index = 0;
                foreach (KeyValuePair<int, int> pair in dict)
                {
                    // plain
                    string val = value.Substring(index, pair.Key - index);
                    list.AddRange(Split(val));

                    // parentheses or func
                    Segment segment = null;
                    val = value.Substring(pair.Key, pair.Value);
                    if (val.StartsWith("("))
                    {
                        // parentheses
                        Segment inner = Compose(val.Substring(1, val.Length - 2).Trim());
                        segment = new ParenthesesPairSegment(DecodeConstant(val), inner);
                    }
                    else
                    {
                        // func
                        int idx = val.IndexOf('(');
                        string func = val.Substring(0, idx).Trim();
                        if (func == "contains")
                        {
                            segment = NewInFuncSegment(val, idx);

                            // SQL LIKE
                            // contains(Name, 'n')
                            if (segment == null)
                            {
                                segment = NewBinaryFuncSegment(val, idx, func);
                            }
                        }
                        else if (func == "isof")
                        {
                            if (val.Contains(','))
                            {
                                segment = NewBinaryFuncSegment(val, idx, func);
                            }
                            else
                            {
                                segment = NewUnaryFuncSegment(val, idx, func);
                            }
                        }
                        else if (ParamlessFuncs.Contains(func))
                        {
                            segment = new ParamlessFuncSegment(val, func);
                        }
                        else if (UnaryFuncs.Contains(func))
                        {
                            segment = NewUnaryFuncSegment(val, idx, func);
                        }
                        else if (BinaryFuncs.Contains(func))
                        {
                            segment = NewBinaryFuncSegment(val, idx, func);
                        }
                    }

                    //if (segment == null) throw // never
                    list.Add(segment);
                    index = pair.Key + pair.Value;
                }

                list.AddRange(Split(value.Substring(index)));
            }

            return (list.Count == 1) ? list[0] : new ArraySegment(DecodeConstant(value), list);
        }

        private UnaryFuncSegment NewUnaryFuncSegment(string value, int index, string func)
        {
            string val = value.Substring(index + 1, value.Length - index - 2).Trim();

            Segment operand = Compose(val);

            return new UnaryFuncSegment(DecodeConstant(value), func, operand);
        }

        private BinaryFuncSegment NewBinaryFuncSegment(string value, int index, string func)
        {
            string val = value.Substring(index + 1, value.Length - index - 2).Trim();

            Dictionary<string, string> placeholders = new Dictionary<string, string>();
            val = Regex.Replace(val, ParenthesesPairPattern, new MatchEvaluator(m =>
            {
                string guid = GetGuid();
                placeholders.Add(guid, m.Value);
                return guid;
            }));

            string[] ss = val.Split(',');
            for (int i = 0; i < ss.Length; i++)
            {
                ss[i] = DecodeConstant(ss[i], placeholders).Trim();
            }

            Segment left = Compose(ss[0]);
            Segment right = Compose(ss[1]);

            if (ss.Length > 2)
            {
                string segmentsValue = val.Substring(ss[0].Length + 1).Trim();
                segmentsValue = DecodeConstant(segmentsValue);
                Segment[] segments = new Segment[ss.Length - 1];
                for (int i = 1; i < ss.Length; i++)
                {
                    segments[i - 1] = Compose(ss[i]);
                }
                right = new ArraySegment(segmentsValue, segments);
            }

            return new BinaryFuncSegment(DecodeConstant(value), func, left, right);
        }

        // SQL IN // func: contains((1,2,...),Id) or contains(({e1e0601c-7183-482f-955d-dfb8e36a194c},{...},...),Name)
        private BinaryFuncSegment NewInFuncSegment(string value, int index)
        {
            string val = value.Substring(index + 1, value.Length - index - 2).Trim();
            string pattern = ParenthesesPairPattern + @"\s*,";
            Match match = Regex.Match(val, pattern);
            if (!match.Success) return null;
            if (match.Index != 0) return null;

            string rightStr = val.Substring(match.Length).Trim();
            string leftStr = match.Value.Substring(0, match.Value.Length - 1).Trim(); // trim ,
            string arrayStr = leftStr.Substring(1, leftStr.Length - 2); // trim ( and )
            string[] ss = arrayStr.Split(',');

            Segment[] segments = new Segment[ss.Length];
            for (int i = 0; i < ss.Length; i++)
            {
                segments[i] = Compose(ss[i].Trim());
                if (segments[i] is ConstantSegment) continue;
                return null;
            }
            ArraySegment arraySegment = new ArraySegment(DecodeConstant(arrayStr), segments);
            Segment left = new ParenthesesPairSegment(DecodeConstant(leftStr), arraySegment);

            string func = "contains";
            Segment right = Compose(rightStr);
            return new BinaryFuncSegment(DecodeConstant(value), func, left, right);
        }

        // outermost parentheses or func
        private static Dictionary<int, int> Group(string value)
        {
            Dictionary<int, int> dict = new Dictionary<int, int>(); // index, length
            MatchCollection matches = Regex.Matches(value, ParenthesesPairPattern);
            foreach (Match match in matches)
            {
                string s = value.Substring(0, match.Index);
                Match funcMatch = Regex.Match(s, FuncPattern, RegexOptions.RightToLeft);
                if (funcMatch.Success)
                {
                    if (funcMatch.Index + funcMatch.Length == s.Length)
                    {
                        dict.Add(match.Index - funcMatch.Length, match.Length + funcMatch.Length);
                        continue;
                    }
                }

                dict.Add(match.Index, match.Length);
            }
            return dict;
        }

        // plain // next Split_Guid
        private IEnumerable<Segment> Split(string value)
        {
            return Split_Guid(value);
        }

        // next Split_Parameter
        private IEnumerable<Segment> Split_Guid(string value)
        {
            List<Segment> list = new List<Segment>();
            if (string.IsNullOrWhiteSpace(value)) return list;

            // index, length
            Dictionary<int, int> dict = new Dictionary<int, int>();
            MatchCollection matches = Regex.Matches(value, GuidPattern);
            foreach (Match match in matches)
            {
                dict.Add(match.Index, match.Length);
            }

            //
            int index = 0;
            foreach (KeyValuePair<int, int> pair in dict)
            {
                // Parameter
                string val = value.Substring(index, pair.Key - index);
                list.AddRange(Split_Parameter(val));

                // ConstantPlaceholder
                val = value.Substring(pair.Key, pair.Value);
                if (!ConstantPlaceholders.ContainsKey(val))
                    throw new SyntaxErrorException(string.Format(ODataMessages.IncorrectSyntax, val));

                val = ConstantPlaceholders[val];
                Segment segment = new ConstantSegment(val);

                list.Add(segment);
                index = pair.Key + pair.Value;
            }

            // Parameter
            list.AddRange(Split_Parameter(value.Substring(index)));

            return list;
        }

        // next Split_ExtProperty
        private IEnumerable<Segment> Split_Parameter(string value)
        {
            const string pattern = @"(?<!\w)@[A-Za-z]\w*";

            List<Segment> list = new List<Segment>();
            if (string.IsNullOrWhiteSpace(value)) return list;

            // index, length
            Dictionary<int, int> dict = new Dictionary<int, int>();
            MatchCollection matches = Regex.Matches(value, pattern);
            foreach (Match match in matches)
            {
                dict.Add(match.Index, match.Length);
            }

            //
            int index = 0;
            foreach (KeyValuePair<int, int> pair in dict)
            {
                // extended Property
                string val = value.Substring(index, pair.Key - index);
                list.AddRange(Split_ExtProperty(val));

                // Parameter
                val = value.Substring(pair.Key, pair.Value);
                _parameters.Add(val);
                Segment segment = new ParameterSegment(val);

                list.Add(segment);
                index = pair.Key + pair.Value;
            }

            // extended Property
            list.AddRange(Split_ExtProperty(value.Substring(index)));

            return list;
        }

        // next Split_AllLettersWords
        private IEnumerable<Segment> Split_ExtProperty(string value)
        {
            const string pattern = @"[A-Za-z_]\w*(\.[A-Za-z_]\w*)+";

            List<Segment> list = new List<Segment>();
            if (string.IsNullOrWhiteSpace(value)) return list;

            // index, length
            Dictionary<int, int> dict = new Dictionary<int, int>();
            MatchCollection matches = Regex.Matches(value, pattern);
            foreach (Match match in matches)
            {
                dict.Add(match.Index, match.Length);
            }

            //
            int index = 0;
            foreach (KeyValuePair<int, int> pair in dict)
            {
                // Constant or Operator or Property
                string val = value.Substring(index, pair.Key - index);
                list.AddRange(Split_AllLettersWords(val));

                // extended Property
                val = value.Substring(pair.Key, pair.Value);
                _properties.Add(val);
                Segment segment = new PropertySegment(val);

                list.Add(segment);
                index = pair.Key + pair.Value;
            }

            // Constant or Operator or Property
            list.AddRange(Split_AllLettersWords(value.Substring(index)));

            return list;
        }

        // next Split_Property
        private IEnumerable<Segment> Split_AllLettersWords(string value)
        {
            const string pattern = @"\b[A-Za-z]+\b";

            List<Segment> list = new List<Segment>();
            if (string.IsNullOrWhiteSpace(value)) return list;

            // index, length
            Dictionary<int, int> dict = new Dictionary<int, int>();
            MatchCollection matches = Regex.Matches(value, pattern);
            foreach (Match match in matches)
            {
                dict.Add(match.Index, match.Length);
            }

            //
            int index = 0;
            foreach (KeyValuePair<int, int> pair in dict)
            {
                // Property
                string val = value.Substring(index, pair.Key - index);
                list.AddRange(Split_Property(val));

                // Constant or Operator or Property
                Segment segment;
                val = value.Substring(pair.Key, pair.Value);
                switch (val)
                {
                    case "null":
                        segment = new NullSegment();
                        break;
                    case "false":
                        segment = new FalseSegment();
                        break;
                    case "true":
                        segment = new TrueSegment();
                        break;
                    default:
                        if (Operators.Contains(val))
                        {
                            segment = new OperatorSegment(val);
                        }
                        else
                        {
                            _properties.Add(val);
                            segment = new PropertySegment(val);
                        }
                        break;
                }

                list.Add(segment);
                index = pair.Key + pair.Value;
            }

            // Property
            list.AddRange(Split_Property(value.Substring(index)));

            return list;
        }

        // next Split_PointNumber
        private IEnumerable<Segment> Split_Property(string value)
        {
            const string pattern = @"[A-Za-z_]\w*";

            List<Segment> list = new List<Segment>();
            if (string.IsNullOrWhiteSpace(value)) return list;

            // index, length
            Dictionary<int, int> dict = new Dictionary<int, int>();
            MatchCollection matches = Regex.Matches(value, pattern);
            foreach (Match match in matches)
            {
                dict.Add(match.Index, match.Length);
            }

            //
            int index = 0;
            foreach (KeyValuePair<int, int> pair in dict)
            {
                // decimal or double
                string val = value.Substring(index, pair.Key - index);
                list.AddRange(Split_PointNumber(val));

                // Property
                val = value.Substring(pair.Key, pair.Value);
                _properties.Add(val);
                Segment segment = new PropertySegment(val);

                list.Add(segment);
                index = pair.Key + pair.Value;
            }

            // decimal or double
            list.AddRange(Split_PointNumber(value.Substring(index)));

            return list;
        }

        // next Split_Integer
        private IEnumerable<Segment> Split_PointNumber(string value)
        {
            const string pattern = @"\d*\.\d+";

            List<Segment> list = new List<Segment>();
            if (string.IsNullOrWhiteSpace(value)) return list;

            // index, length
            Dictionary<int, int> dict = new Dictionary<int, int>();
            MatchCollection matches = Regex.Matches(value, pattern);
            foreach (Match match in matches)
            {
                int idx = match.Index;
                int len = match.Length;
                if (match.Index > 0)
                {
                    char c = value[match.Index - 1];
                    if (c == '+' || c == '-')
                    {
                        idx = idx - 1;
                        len = len + 1;
                    }
                }
                dict.Add(idx, len);
            }

            //
            int index = 0;
            foreach (KeyValuePair<int, int> pair in dict)
            {
                // int or long
                string val = value.Substring(index, pair.Key - index);
                list.AddRange(Split_Integer(val));

                // decimal or double
                Segment segment;
                try
                {
                    val = value.Substring(pair.Key, pair.Value);

                    object oVal = ToFloatNumber(val);
                    segment = new ConstantSegment(val, oVal);
                }
                catch (Exception ex)
                {
                    throw new SyntaxErrorException(ex.Message, ex);
                }

                list.Add(segment);
                index = pair.Key + pair.Value;
            }

            // int or long
            list.AddRange(Split_Integer(value.Substring(index)));

            return list;
        }

        // next Split_String
        private IEnumerable<Segment> Split_Integer(string value)
        {
            const string pattern = @"\d+";

            List<Segment> list = new List<Segment>();
            if (string.IsNullOrWhiteSpace(value)) return list;

            // index, length
            Dictionary<int, int> dict = new Dictionary<int, int>();
            MatchCollection matches = Regex.Matches(value, pattern);
            foreach (Match match in matches)
            {
                int idx = match.Index;
                int len = match.Length;
                if (match.Index > 0)
                {
                    char c = value[match.Index - 1];
                    if (c == '+' || c == '-')
                    {
                        idx = idx - 1;
                        len = len + 1;
                    }
                }
                dict.Add(idx, len);
            }

            //
            int index = 0;
            foreach (KeyValuePair<int, int> pair in dict)
            {
                // end
                string val = value.Substring(index, pair.Key - index);
                list.AddRange(Split_String(val));

                // int or long
                Segment segment;
                try
                {
                    val = value.Substring(pair.Key, pair.Value);

                    object oVal = ToIntegerNumber(val);
                    segment = new ConstantSegment(val, oVal);
                }
                catch (Exception ex)
                {
                    throw new SyntaxErrorException(ex.Message, ex);
                }

                list.Add(segment);
                index = pair.Key + pair.Value;
            }

            // end
            list.AddRange(Split_String(value.Substring(index)));

            return list;
        }

        // end
        private IEnumerable<Segment> Split_String(string value)
        {
            List<Segment> list = new List<Segment>();
            if (string.IsNullOrWhiteSpace(value)) return list;

            throw new SyntaxErrorException(string.Format(ODataMessages.IncorrectSyntax, value));

            //Segment segment = new StringSegment(value);
            //list.Add(segment);

            //return list;
        }

        protected static string GetGuid()
        {
            return AnalysisHelper.GetGuid();
        }

        protected static bool IsNumeric(Type type)
        {
            return TypeHelper.IsNumeric(type);
        }

        protected static object ToIntegerNumber(string value)
        {
            return TypeHelper.ToIntegerNumber(value);
        }

        protected static object ToFloatNumber(string value)
        {
            return TypeHelper.ToFloatNumber(value);
        }


    }
}
