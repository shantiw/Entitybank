using System;
using System.Collections.Generic;
using System.Linq;
using XData.Data.Helpers;

namespace XData.Data.OData
{
    public class ParameterCollection
    {
        private Dictionary<string, object> _parameterValues = new Dictionary<string, object>();
        public IReadOnlyDictionary<string, object> ParameterValues { get => _parameterValues; }

        // ParamName, UpperParamName
        private Dictionary<string, string> _upperNameMapping = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> UpperNameMapping { get => _upperNameMapping; }

        private List<string> _upperNames = new List<string>();

        private readonly IEnumerable<KeyValuePair<string, string>> _stringValues;

        public ParameterCollection(IEnumerable<KeyValuePair<string, string>> parameterValues)
        {
            _stringValues = parameterValues;
        }

        public void AddRange(IEnumerable<string> parameters)
        {
            foreach (string parameter in parameters.Distinct())
            {
                if (!_parameterValues.ContainsKey(parameter))
                {
                    _parameterValues.Add(parameter, GetParameterValue(parameter));
                }

                if (!_upperNameMapping.ContainsKey(parameter))
                {
                    string upperName = parameter.ToUpper();
                    while (_upperNames.Contains(upperName))
                    {
                        upperName += "L";
                    }
                    _upperNames.Add(upperName);
                    _upperNameMapping.Add(parameter, upperName);
                }
            }
        }

        private object GetParameterValue(string parameter)
        {
            return GetParameterValue(_stringValues, parameter);
        }

        private static object GetParameterValue(IEnumerable<KeyValuePair<string, string>> parameterValues, string parameter)
        {
            object result;

            string value = GetValue(parameterValues, parameter);
            if (string.IsNullOrWhiteSpace(value)) return null;

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

        private static string GetValue(IEnumerable<KeyValuePair<string, string>> nameValues, string key)
        {
            return TypeHelper.GetValue(nameValues, key);
        }

        private static object ToIntegerNumber(string value)
        {
            return TypeHelper.ToIntegerNumber(value);
        }

        private static object ToFloatNumber(string value)
        {
            return TypeHelper.ToFloatNumber(value);
        }

        // Upper Case
        private const string ParameterPrefix = "P";
        private int _parameterIndex = 1;

        public string GenerateNextParamName()
        {
            string paramName = string.Format("@{0}{1}", ParameterPrefix, _parameterIndex);
            _parameterIndex++;

            while (_upperNames.Contains(paramName))
            {
                paramName = string.Format("@{0}{1}", ParameterPrefix, _parameterIndex);
                _parameterIndex++;
            }

            _upperNames.Add(paramName);

            _parameterValues.Add(paramName, null);

            _upperNameMapping.Add(paramName, paramName);

            return paramName;
        }

        public void ResetParameterValues(IReadOnlyDictionary<string, object> source)
        {
            foreach (string key in _parameterValues.Keys.ToList())
            {
                object value = null;
                if (source.ContainsKey(key))
                {
                    value = source[key];
                }
                _parameterValues[key] = value;
            }
        }


    }
}
