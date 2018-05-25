using System;
using System.Collections.Generic;
using System.Linq;
using XData.Data.Helpers;

namespace XData.Data.OData
{
    public class ParameterCollection
    {
        // case sensitive
        // ParamName, UpperParamName
        private Dictionary<string, string> _upperNameMapping = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> UpperNameMapping { get => _upperNameMapping; }

        public void UnionParameters(IEnumerable<string> parameters)
        {
            foreach (string parameter in parameters.Distinct())
            {
                if (!_upperNameMapping.ContainsKey(parameter))
                {
                    string upperName = parameter.ToUpper();
                    while (_upperNameMapping.Values.Contains(upperName))
                    {
                        upperName += "L";
                    }
                    _upperNameMapping.Add(parameter, upperName);
                }
            }
        }

        private Dictionary<string, object> _parameterValues = new Dictionary<string, object>();
        public IReadOnlyDictionary<string, object> ParameterValues { get => _parameterValues; }

        public void SetParameterValues(IEnumerable<KeyValuePair<string, string>> parameterValues)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (KeyValuePair<string, string> pv in parameterValues)
            {
                object value = Parse(pv.Value);
                if (dict.ContainsKey(pv.Key))
                {
                    dict[pv.Key] = value;
                }
                else
                {
                    dict[pv.Key] = value;
                }
            }

            SetParameterValues(dict);
        }

        public void SetParameterValues(IReadOnlyDictionary<string, object> parameterValues)
        {
            foreach (KeyValuePair<string, object> pv in parameterValues)
            {
                if (_upperNameMapping.ContainsKey(pv.Key))
                {
                    if (_parameterValues.ContainsKey(pv.Key))
                    {
                        _parameterValues[pv.Key] = pv.Value;
                    }
                    else
                    {
                        _parameterValues.Add(pv.Key, pv.Value);
                    }
                }
            }
        }

        private static object Parse(string value)
        {
            object result;

            if (value == "null") return null;
            if (value == "true") return true;
            if (value == "false") return false;

            if (value.StartsWith("datetime'") && value.EndsWith("'"))
            {
                result = DateTime.Parse(value.Substring(9, value.Length - 10));
            }
            else if (value.StartsWith("'") && value.EndsWith("'"))
            {
                result = value.Substring(1, value.Length - 2).Replace("''", "'");
            }
            else
            {
                if (value.Contains("."))
                {
                    result = ToFloatNumber(value);
                }
                else
                {
                    result = ToIntegerNumber(value);
                }
            }

            return result;
        }

        private static object ToIntegerNumber(string value)
        {
            return TypeHelper.ToIntegerNumber(value);
        }

        private static object ToFloatNumber(string value)
        {
            return TypeHelper.ToFloatNumber(value);
        }

        //
        private const string ParameterPrefix = "P";  // Upper Case
        private int _parameterIndex = 1;

        public string GenerateNextParamName()
        {
            string paramName = string.Format("@{0}{1}", ParameterPrefix, _parameterIndex);
            _parameterIndex++;

            while (_upperNameMapping.Values.Contains(paramName))
            {
                paramName = string.Format("@{0}{1}", ParameterPrefix, _parameterIndex);
                _parameterIndex++;
            }

            _parameterValues.Add(paramName, null);

            _upperNameMapping.Add(paramName, paramName);

            return paramName;
        }


    }
}
