using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Helpers;
using XData.Data.DataObjects;
using XData.Data.Schema;

namespace XData.Data.OData
{
    public abstract partial class QueryGenerator
    {
        protected abstract class Where
        {
            public string Clause { get; protected set; }

            // decoratedParameter, value // :P11,value
            private Dictionary<string, object> _constants = new Dictionary<string, object>();
            public IReadOnlyDictionary<string, object> Constants { get => _constants; }

            // parameter, decoratedParameter // @P1,:P1
            private Dictionary<string, string> _paramMapping = new Dictionary<string, string>();
            public IReadOnlyDictionary<string, string> ParamMapping { get => _paramMapping; }

            // eq,=
            protected IReadOnlyDictionary<string, string> OperatorMapping;
            protected abstract IReadOnlyDictionary<string, string> GetOperatorMapping();

            protected readonly XElement Schema;
            public readonly IEnumerable<Property> Properties;
            protected readonly Filter Filter;
            protected IReadOnlyDictionary<string, string> UpperParamNameMapping;
            protected readonly Table Table;
            protected readonly QueryGenerator Generator;

            protected Func<string> GenerateNextParamName;

            protected Where(Query query, Table table, QueryGenerator generator)
            {
                OperatorMapping = GetOperatorMapping();

                GenerateNextParamName = () => query.GenerateNextParamName();
                Schema = query.Schema;
                Properties = query.Properties;
                Filter = query.Filter;
                UpperParamNameMapping = query.UpperParamNameMapping;

                Table = table;
                Generator = generator;

                Clause = ToSqlString(Filter.Segment);
            }

            protected string ToSqlString(Segment segment)
            {
                if (segment is ArraySegment)
                {
                    return ToSqlString(segment as ArraySegment);
                }
                else if (segment is ParenthesesPairSegment)
                {
                    return ToSqlString(segment as ParenthesesPairSegment);
                }
                else if (segment is FuncSegment)
                {
                    if (segment is ParamlessFuncSegment)
                    {
                        return ToSqlString(segment as ParamlessFuncSegment);
                    }
                    else if (segment is UnaryFuncSegment)
                    {
                        return ToSqlString(segment as UnaryFuncSegment);
                    }
                    else if (segment is BinaryFuncSegment)
                    {
                        return ToSqlString(segment as BinaryFuncSegment);
                    }
                }
                else if (segment is NullSegment)
                {
                    return ToSqlString(segment as NullSegment);
                }
                else if (segment is ConstantSegment)
                {
                    if (segment is TrueSegment)
                    {
                        return ToSqlString(segment as TrueSegment);
                    }
                    else if (segment is FalseSegment)
                    {
                        return ToSqlString(segment as FalseSegment);
                    }
                    else
                    {
                        return ToSqlString(segment as ConstantSegment);
                    }
                }
                else if (segment is OperatorSegment)
                {
                    return ToSqlString(segment as OperatorSegment);
                }
                else if (segment is PropertySegment)
                {
                    return ToSqlString(segment as PropertySegment);
                }
                else if (segment is ParameterSegment)
                {
                    return ToSqlString(segment as ParameterSegment);
                }

                throw new NotSupportedException(segment.GetType().ToString());
            }

            protected virtual string ToSqlString(ArraySegment segment)
            {
                // "null eq Name"/"null ne Name" to "Name eq null"/"Name ne null"
                int len = segment.Segments.Length;
                if (segment.Segments[len - 1] is NullSegment) len--;

                int j = 0;
                while (j < len)
                {
                    for (int i = j; i < len; i++)
                    {
                        if (segment.Segments[i] is NullSegment)
                        {
                            if (segment.Segments[i + 1] is OperatorSegment)
                            {
                                OperatorSegment operatorSegment = segment.Segments[i + 1] as OperatorSegment;
                                if (operatorSegment.Operator == "eq" || operatorSegment.Operator == "ne")
                                {
                                    NullSegment segment_i = segment.Segments[i] as NullSegment;
                                    segment.Segments[i] = segment.Segments[i + 2];
                                    segment.Segments[i + 2] = segment_i;
                                    j = i + 2 + 1;
                                    break;
                                }
                            }
                        }
                        j = i + 1;
                    }
                }

                // "= NULL"/"!= NULL" to "IS NULL"/"IS NOT NULL"
                string eqOperator = ToSqlString(new OperatorSegment("eq"));
                string neOperator = ToSqlString(new OperatorSegment("ne"));
                string nullString = ToSqlString(new NullSegment());

                return string.Join(" ", ToSqlStrings(segment))
                    .Replace(string.Format("{0} {1}", eqOperator, nullString), "IS NULL")
                    .Replace(string.Format("{0} {1}", neOperator, nullString), "IS NOT NULL");
            }

            protected string[] ToSqlStrings(ArraySegment segment)
            {
                List<string> strings = new List<string>();
                for (int i = 0; i < segment.Segments.Length; i++)
                {
                    string sqlString = ToSqlString(segment.Segments[i]);
                    if (sqlString != string.Empty) strings.Add(sqlString);
                }
                return strings.ToArray();
            }

            protected virtual string ToSqlString(ParenthesesPairSegment segment)
            {
                return string.Format("()", ToSqlString(segment.Inner));
            }

            protected virtual string ToSqlString(ParamlessFuncSegment segment)
            {
                switch (segment.Func)
                {
                    case "now":
                        return StringifyNow(segment);
                    case "mindatetime":
                        return StringifyMindatetime(segment);
                    case "maxdatetime":
                        return StringifyMaxdatetime(segment);
                    default:
                        throw new NotSupportedException(segment.Func);
                }
            }

            protected virtual string ToSqlString(UnaryFuncSegment segment)
            {
                switch (segment.Func)
                {
                    case "length":
                        return StringifyLength(segment);
                    case "tolower":
                        return StringifyTolower(segment);
                    case "toupper":
                        return StringifyToupper(segment);
                    case "trim":
                        return StringifyTrim(segment);
                    case "year":
                        return StringifyYear(segment);
                    case "month":
                        return StringifyMonth(segment);
                    case "day":
                        return StringifyDay(segment);
                    case "hour":
                        return StringifyHour(segment);
                    case "minute":
                        return StringifyMinute(segment);
                    case "second":
                        return StringifySecond(segment);
                    case "fractionalseconds":
                        return StringifyFractionalseconds(segment);
                    case "date":
                        return StringifyDate(segment);
                    case "time":
                        return StringifyTime(segment);
                    case "totaloffsetminutes":
                        return StringifyTotaloffsetminutes(segment);
                    case "isof":
                        return StringifyIsof(segment);
                    case "geo.length":
                        return StringifyGeoLength(segment);
                    default:
                        throw new NotSupportedException(segment.Func);
                }
            }

            protected string ToSqlString(BinaryFuncSegment segment)
            {
                switch (segment.Func)
                {
                    case "contains":
                        return StringifyContains(segment);
                    case "endswith":
                        return StringifyEndswith(segment);
                    case "startswith":
                        return StringifyStartswith(segment);
                    case "indexof":
                        return StringifyIndexof(segment);
                    case "substring":
                        return StringifySubstring(segment);
                    case "concat":
                        return StringifyConcat(segment);
                    case "cast":
                        return StringifyCast(segment);
                    case "isof":
                        return StringifyIsof(segment);
                    case "geo.distance":
                        return StringifyGeoDistance(segment);
                    case "geo.intersects":
                        return StringifyGeoIntersects(segment);
                    default:
                        throw new NotSupportedException(segment.Func);
                }
            }

            protected virtual string ToSqlString(NullSegment segment)
            {
                return "NULL";
            }

            protected virtual string ToSqlString(TrueSegment segment)
            {
                return "1";
            }

            protected virtual string ToSqlString(FalseSegment segment)
            {
                return "0";
            }

            protected virtual string ToSqlString(ConstantSegment segment)
            {
                string parameter = GenerateNextParamName();
                string decoratedParameter = Generator.DecorateParameterName(parameter, UpperParamNameMapping);
                _constants.Add(decoratedParameter, segment.Constant);
                return decoratedParameter;
            }

            protected virtual string ToSqlString(StringSegment segment)
            {
                return segment.Value;
            }

            protected virtual string ToSqlString(OperatorSegment segment)
            {
                if (OperatorMapping.ContainsKey(segment.Operator)) return OperatorMapping[segment.Operator];

                throw new NotSupportedException(segment.Operator);
            }

            protected virtual string ToSqlString(PropertySegment segment)
            {
                Column column = Table.Columns[segment.Property];
                return Generator.ToSqlString(column, Table);
            }

            protected virtual string ToSqlString(ParameterSegment segment)
            {
                if (_paramMapping.ContainsKey(segment.Parameter)) return _paramMapping[segment.Parameter];

                string decorated = Generator.DecorateParameterName(segment.Parameter, UpperParamNameMapping);
                _paramMapping.Add(segment.Parameter, decorated);
                return decorated;
            }

            // funcs
            protected abstract string StringifyNow(ParamlessFuncSegment segment);
            protected abstract string StringifyMindatetime(ParamlessFuncSegment segment);
            protected abstract string StringifyMaxdatetime(ParamlessFuncSegment segment);

            protected abstract string StringifyLength(UnaryFuncSegment segment);
            protected abstract string StringifyTolower(UnaryFuncSegment segment);
            protected abstract string StringifyToupper(UnaryFuncSegment segment);
            protected abstract string StringifyTrim(UnaryFuncSegment segment);
            protected abstract string StringifyYear(UnaryFuncSegment segment);
            protected abstract string StringifyMonth(UnaryFuncSegment segment);
            protected abstract string StringifyDay(UnaryFuncSegment segment);
            protected abstract string StringifyHour(UnaryFuncSegment segment);
            protected abstract string StringifyMinute(UnaryFuncSegment segment);
            protected abstract string StringifySecond(UnaryFuncSegment segment);
            protected abstract string StringifyFractionalseconds(UnaryFuncSegment segment);
            protected abstract string StringifyDate(UnaryFuncSegment segment);
            protected abstract string StringifyTime(UnaryFuncSegment segment);
            protected abstract string StringifyTotaloffsetminutes(UnaryFuncSegment segment);
            protected abstract string StringifyRound(UnaryFuncSegment segment);
            protected abstract string StringifyFloor(UnaryFuncSegment segment);
            protected abstract string StringifyCeiling(UnaryFuncSegment segment);
            protected abstract string StringifyIsof(UnaryFuncSegment segment);
            protected abstract string StringifyGeoLength(UnaryFuncSegment segment);

            protected abstract string StringifyContains(BinaryFuncSegment segment);
            protected abstract string StringifyEndswith(BinaryFuncSegment segment);
            protected abstract string StringifyStartswith(BinaryFuncSegment segment);
            protected abstract string StringifyIndexof(BinaryFuncSegment segment);
            protected abstract string StringifySubstring(BinaryFuncSegment segment);
            protected abstract string StringifyConcat(BinaryFuncSegment segment);
            protected abstract string StringifyCast(BinaryFuncSegment segment);
            protected abstract string StringifyIsof(BinaryFuncSegment segment);
            protected abstract string StringifyGeoDistance(BinaryFuncSegment segment);
            protected abstract string StringifyGeoIntersects(BinaryFuncSegment segment);

            protected static IReadOnlyDictionary<string, string> ToOperatorMapping(string[] operatorMappingArray)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                for (int i = 0; i < operatorMappingArray.Length / 2; i++)
                {
                    dict.Add(operatorMappingArray[i * 2], operatorMappingArray[i * 2 + 1]);
                }
                return dict;
            }


        }
    }
}
