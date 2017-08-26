using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.DataObjects;

namespace XData.Data.OData
{
    public partial class SqlQueryGenerator : QueryGenerator
    {
        protected class SqlWhere : Where
        {
            // fix: 11 div 2 is 5
            // not supported: "has"
            private static readonly string[] OperatorMappingArray = new string[]
            {
                "eq", "=",
                "ne", "!=",
                "gt", ">",
                "ge", ">=",
                "lt", "<",
                "le", "<=",
                //"has", ?,
                "and", "AND",
                "or", "OR",
                "not", "NOT",
                "add", "+",
                "sub", "-",
                "mul", "*",
                "div", "/",
                "mod", "%"
            };

            private static IReadOnlyDictionary<string, string> Operator_Mapping = null;

            protected override IReadOnlyDictionary<string, string> GetOperatorMapping()
            {
                if (Operator_Mapping == null)
                {
                    Operator_Mapping = ToOperatorMapping(OperatorMappingArray);
                }

                return Operator_Mapping;
            }

            public SqlWhere(Query query, Table table, QueryGenerator generator)
                : base(query, table, generator)
            {
            }

            // fix: 11 div 2 is 5
            protected override string ToSqlString(ArraySegment segment)
            {
                List<Segment> segments = new List<Segment>();
                for (int i = 0; i < segment.Segments.Length; i++)
                {
                    if (segment.Segments[i] is OperatorSegment)
                    {
                        OperatorSegment operatorSegment = segment.Segments[i] as OperatorSegment;
                        if (operatorSegment.Operator == "div")
                        {
                            segments.Add(new OperatorSegment("mul"));
                            segments.Add(new ConstantSegment("1.0", 1.0));
                            segments.Add(segment.Segments[i]);
                            continue;
                        }
                    }

                    segments.Add(segment.Segments[i]);
                }

                return base.ToSqlString(new ArraySegment(segment.Value, segments));
            }

            // contains((1,3,5), Id) // contains(CompanyName,'freds')
            protected override string StringifyContains(BinaryFuncSegment segment)
            {
                if (segment.Left is ParenthesesPairSegment)
                {
                    ParenthesesPairSegment left = segment.Left as ParenthesesPairSegment;
                    if (left.Inner is ArraySegment)
                    {
                        ArraySegment arraySegment = left.Inner as ArraySegment;
                        List<string> list = new List<string>();
                        for (int i = 0; i < arraySegment.Segments.Length; i++)
                        {
                            list.Add(ToSqlString(arraySegment.Segments[i]));
                        }
                        return string.Format("{0} IN ({1})", ToSqlString(segment.Right), string.Join(",", list));
                    }
                }

                //
                return string.Format("{0} LIKE '%' + {1} + '%'", ToSqlString(segment.Left), ToSqlString(segment.Right));
            }

            // endswith(CompanyName,'Futterkiste')
            protected override string StringifyEndswith(BinaryFuncSegment segment)
            {
                return string.Format("{0} LIKE '%' + {1}", ToSqlString(segment.Left), ToSqlString(segment.Right));
            }

            // startswith(CompanyName,'Alfr')
            protected override string StringifyStartswith(BinaryFuncSegment segment)
            {
                return string.Format("{0} LIKE {1} + '%'", ToSqlString(segment.Left), ToSqlString(segment.Right));
            }

            // indexof(CompanyName,'lfreds') eq 1 // indexof(CompanyName,'e', 4) eq 11
            protected override string StringifyIndexof(BinaryFuncSegment segment)
            {
                if (segment.Right is ArraySegment)
                {
                    string[] strings = ToSqlStrings(segment.Right as ArraySegment);
                    return string.Format("(CHARINDEX({0}, {1}, {2} + 1) - 1)", strings[0], ToSqlString(segment.Left), strings[1]);
                }
                return string.Format("(CHARINDEX({0}, {1}) - 1)", ToSqlString(segment.Right), ToSqlString(segment.Left));
            }

            // substring(CompanyName,1) eq 'lfreds Futterkiste' // substring(CompanyName,1,6) eq 'lfreds'
            protected override string StringifySubstring(BinaryFuncSegment segment)
            {
                if (segment.Right is ArraySegment)
                {
                    string[] strings = ToSqlStrings(segment.Right as ArraySegment);
                    return string.Format("SUBSTRING({0}, {1} + 1, {2})", ToSqlString(segment.Left), strings[0], strings[1]);
                }
                return string.Format("SUBSTRING({0}, {1} + 1, {2})", ToSqlString(segment.Left), ToSqlString(segment.Right), int.MaxValue.ToString());
            }

            // length(CompanyName) eq 19
            protected override string StringifyLength(UnaryFuncSegment segment)
            {
                return string.Format("LEN({0})", ToSqlString(segment.Operand));
            }

            // concat(concat(City,', '), Country) eq 'Berlin, Germany' // concat(City,', ', Country) eq 'Berlin, Germany'
            protected override string StringifyConcat(BinaryFuncSegment segment)
            {
                if (segment.Right is ArraySegment)
                {
                    string[] strings = ToSqlStrings(segment.Right as ArraySegment);
                    return string.Format("({0} + {1})", ToSqlString(segment.Left), string.Join(" + ", strings));
                }
                return string.Format("({0} + {1})", ToSqlString(segment.Left), ToSqlString(segment.Right));
            }

            // tolower(CompanyName) eq 'alfreds futterkiste'
            protected override string StringifyTolower(UnaryFuncSegment segment)
            {
                return string.Format("LOWER({0})", ToSqlString(segment.Operand));
            }

            // toupper(CompanyName) eq 'ALFREDS FUTTERKISTE'
            protected override string StringifyToupper(UnaryFuncSegment segment)
            {
                return string.Format("UPPER({0})", ToSqlString(segment.Operand));
            }

            // trim(CompanyName) eq 'Alfreds Futterkiste'
            protected override string StringifyTrim(UnaryFuncSegment segment)
            {
                return string.Format("LTRIM(RTRIM({0}))", ToSqlString(segment.Operand));
            }

            // round(Freight) eq 32
            protected override string StringifyRound(UnaryFuncSegment segment)
            {
                return string.Format("ROUND({0}, 0)", ToSqlString(segment.Operand));
            }

            // floor(Freight) eq 32
            protected override string StringifyFloor(UnaryFuncSegment segment)
            {
                return string.Format("FLOOR({0})", ToSqlString(segment.Operand));
            }

            // ceiling(Freight) eq 33
            protected override string StringifyCeiling(UnaryFuncSegment segment)
            {
                return string.Format("CEILING({0})", ToSqlString(segment.Operand));
            }

            // year(BirthDate) eq 0
            protected override string StringifyYear(UnaryFuncSegment segment)
            {
                return string.Format("DATEPART(YEAR, {0})", ToSqlString(segment.Operand));
            }

            // month(BirthDate) eq 12
            protected override string StringifyMonth(UnaryFuncSegment segment)
            {
                return string.Format("DATEPART(MONTH, {0})", ToSqlString(segment.Operand));
            }

            // day(StartTime) eq 8
            protected override string StringifyDay(UnaryFuncSegment segment)
            {
                return string.Format("DATEPART(DAY, {0})", ToSqlString(segment.Operand));
            }

            // hour(StartTime) eq 1
            protected override string StringifyHour(UnaryFuncSegment segment)
            {
                return string.Format("DATEPART(HOUR, {0})", ToSqlString(segment.Operand));
            }

            // minute(StartTime) eq 0
            protected override string StringifyMinute(UnaryFuncSegment segment)
            {
                return string.Format("DATEPART(MINUTE, {0})", ToSqlString(segment.Operand));
            }

            // second(StartTime) eq 0
            protected override string StringifySecond(UnaryFuncSegment segment)
            {
                return string.Format("DATEPART(SECOND, {0})", ToSqlString(segment.Operand));
            }

            // fractionalseconds(StartTime) eq 0
            protected override string StringifyFractionalseconds(UnaryFuncSegment segment)
            {
                return string.Format("DATEPART(MILLISECOND, {0})", ToSqlString(segment.Operand));
            }

            // totaloffsetminutes(StartTime) eq 60 // datetimeoffset(7) only
            protected override string StringifyTotaloffsetminutes(UnaryFuncSegment segment)
            {
                // "DATEPART(TZOFFSET, SYSDATETIMEOFFSET())"
                return string.Format("DATEPART(TZOFFSET, {0})", ToSqlString(segment.Operand));
            }

            // date(StartTime) ne date(EndTime)
            protected override string StringifyDate(UnaryFuncSegment segment)
            {
                // "CONVERT(datetime, CONVERT(char(10), {0}, 120))"
                return string.Format("CONVERT(date, {0})", ToSqlString(segment.Operand));
            }

            // time(StartTime) le StartOfDay
            protected override string StringifyTime(UnaryFuncSegment segment)
            {
                return string.Format("CONVERT(time, {0})", ToSqlString(segment.Operand));
            }

            // StartTime ge now()
            protected override string StringifyNow(ParamlessFuncSegment segment)
            {
                return "GETDATE()";
            }

            protected override string StringifyMindatetime(ParamlessFuncSegment segment)
            {
                throw new NotSupportedException(segment.Func);
            }

            protected override string StringifyMaxdatetime(ParamlessFuncSegment segment)
            {
                throw new NotSupportedException(segment.Func);
            }

            protected override string StringifyCast(BinaryFuncSegment segment)
            {
                throw new NotSupportedException(segment.Func);
            }

            protected override string StringifyIsof(UnaryFuncSegment segment)
            {
                throw new NotSupportedException(segment.Func);
            }

            protected override string StringifyIsof(BinaryFuncSegment segment)
            {
                throw new NotSupportedException(segment.Func);
            }

            protected override string StringifyGeoDistance(BinaryFuncSegment segment)
            {
                throw new NotSupportedException(segment.Func);
            }

            protected override string StringifyGeoIntersects(BinaryFuncSegment segment)
            {
                throw new NotSupportedException(segment.Func);
            }

            protected override string StringifyGeoLength(UnaryFuncSegment segment)
            {
                throw new NotSupportedException(segment.Func);
            }


        }
    }
}
