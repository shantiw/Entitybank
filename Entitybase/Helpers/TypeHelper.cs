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

        public static bool IsInteger(Type type)
        {
            return (type == typeof(SByte) || type == typeof(Int16) || type == typeof(Int32) || type == typeof(Int64) ||
                 type == typeof(Byte) || type == typeof(UInt16) || type == typeof(UInt32) || type == typeof(UInt64));
        }

        public static bool IsNumeric(Type type)
        {
            if (IsInteger(type)) return true;
            return type == typeof(Decimal) || type == typeof(Single) || type == typeof(Double);
        }

        public static bool IsIntegerArray(Type type)
        {
            return (type == typeof(SByte[]) || type == typeof(Int16[]) || type == typeof(Int32[]) || type == typeof(Int64[]) ||
                type == typeof(Byte[]) || type == typeof(UInt16[]) || type == typeof(UInt32[]) || type == typeof(UInt64[]));
        }

        public static bool IsNumericArray(Type type)
        {
            if (IsIntegerArray(type)) return true;
            return type == typeof(Decimal[]) || type == typeof(Single[]) || type == typeof(Double[]);
        }

        // precision: float:7, double:15-16, decimal:28-29, long:19
        public static object ToFloatNumber(string value)
        {
            string sValue = value.Replace(".", string.Empty);
            if (long.TryParse(sValue, out long lResult))
            {
                sValue = lResult.ToString();
                if (sValue.Length <= 7) return float.Parse(value);
                if (sValue.Length < 16) return double.Parse(value);
                if (sValue.Length == 16)
                {
                    double val = double.Parse(value);
                    string sVal = val.ToString().Replace(".", string.Empty);
                    if (long.Parse(sVal) == lResult) return double.Parse(value);
                }
            }

            // (-7.9 x 1028 - 7.9 x 1028) / (100 - 1028)
            if (decimal.TryParse(value, out decimal result)) return result;

            // ±5.0 × 10−324 to ±1.7 × 10308
            return double.Parse(value);
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
