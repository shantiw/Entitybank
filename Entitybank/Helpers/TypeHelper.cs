using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Helpers
{
    internal static class TypeHelper
    {
        public const string DEFAULT_DATE_FORMAT = "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";
        public const string XS = "http://www.w3.org/2001/XMLSchema";
        public const string XSI = "http://www.w3.org/2001/XMLSchema-instance";

        public static readonly XNamespace XSNamespace = XS;
        public static readonly XNamespace XSINamespace = XSI;

        public static Type GetType(string type)
        {
            if (type.Contains(","))
            {
                string[] ss = type.Split(',');
                string typeName = ss[0].Trim();
                string assemblyName = ss[1].Trim();
                Assembly assembly = Assembly.Load(assemblyName);
                return assembly.GetType(ss[0]);
            }

            return Type.GetType(type);
        }

        public static bool IsNumeric(Type type)
        {
            return (type == typeof(SByte) || type == typeof(Int16) || type == typeof(Int32) || type == typeof(Int64) ||
                type == typeof(Byte) || type == typeof(UInt16) || type == typeof(UInt32) || type == typeof(UInt64) ||
                type == typeof(Decimal) || type == typeof(Single) || type == typeof(Double));
        }

        public static bool IsNumericArray(Type type)
        {
            return (type == typeof(SByte[]) || type == typeof(Int16[]) || type == typeof(Int32[]) || type == typeof(Int64[]) ||
                type == typeof(Byte[]) || type == typeof(UInt16[]) || type == typeof(UInt32[]) || type == typeof(UInt64[]) ||
                type == typeof(Decimal[]) || type == typeof(Single[]) || type == typeof(Double[]));
        }

        public static object ToFloatNumber(string value)
        {
            object result;
            double d = double.Parse(value);
            result = d;
            if (d >= float.MinValue && d <= float.MaxValue)
            {
                float f = float.Parse(value);
                result = f;
                if (f >= (float)decimal.MinValue && f <= (float)decimal.MaxValue)
                {
                    if (decimal.TryParse(value, out decimal c))
                    {
                        result = c;
                    }
                }
            }
            return result;
        }

        public static object ToIntegerNumber(string value)
        {
            object result;
            long l = long.Parse(value);
            result = l;
            if (l >= int.MinValue && l <= int.MaxValue)
            {
                int i = (int)l;
                result = i;
                if (i >= short.MaxValue && i <= short.MaxValue)
                {
                    short s = (short)i;
                    result = s;
                    if (s >= byte.MinValue && s <= byte.MaxValue)
                    {
                        result = (byte)s;
                    }
                }
            }
            return result;
        }

        public static string GetValue(IEnumerable<KeyValuePair<string, string>> nameValues, string key)
        {
            string value = null;
            if (nameValues.Any(p => p.Key == key))
            {
                value = nameValues.First(p => p.Key == key).Value;
            }
            return value;
        }


    }
}
