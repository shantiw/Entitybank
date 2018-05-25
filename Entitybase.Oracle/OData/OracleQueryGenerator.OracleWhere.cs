using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.DataObjects;

namespace XData.Data.OData
{
    public partial class OracleQueryGenerator : QueryGenerator
    {
        // "mod"(11 mod 2) is not an Operator but a function: MOD(numer1,number2)
        // not supported: "has"
        protected class OracleWhere : Where
        {
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
                //"mod", "%"
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

            public OracleWhere(Query query, Table table, QueryGenerator generator) : base(query, table, generator)
            {
            }

            // "mod"(11 mod 2) is not an Operator but a function: MOD(numer1,number2)
            protected override string ToSqlString(ArraySegment segment)
            {
                for (int i = 0; i < segment.Segments.Length; i++)
                {
                    if (segment.Segments[i] is OperatorSegment)
                    {
                        OperatorSegment operatorSegment = segment.Segments[i] as OperatorSegment;
                        if (operatorSegment.Operator == "mod")
                        {
                            string left = ToSqlString(segment.Segments[i - 1]);
                            string right = ToSqlString(segment.Segments[i + 1]);

                            segment.Segments[i] = new StringSegment(string.Format("MOD({0},{1})", left, right));
                            segment.Segments[i - 1] = new StringSegment(string.Empty);
                            segment.Segments[i + 1] = new StringSegment(string.Empty);
                        }
                    }
                }

                return base.ToSqlString(segment);
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
                return string.Format("{0} LIKE '%' || {1} || '%'", ToSqlString(segment.Left), ToSqlString(segment.Right));
            }

            // endswith(CompanyName,'Futterkiste')
            protected override string StringifyEndswith(BinaryFuncSegment segment)
            {
                return string.Format("{0} LIKE '%' || {1}", ToSqlString(segment.Left), ToSqlString(segment.Right));
            }

            // startswith(CompanyName,'Alfr')
            protected override string StringifyStartswith(BinaryFuncSegment segment)
            {
                return string.Format("{0} LIKE {1} || '%'", ToSqlString(segment.Left), ToSqlString(segment.Right));
            }

            // indexof(CompanyName,'lfreds') eq 1 // indexof(CompanyName,'e', 4) eq 11
            protected override string StringifyIndexof(BinaryFuncSegment segment)
            {
                if (segment.Right is ArraySegment)
                {
                    string[] strings = ToSqlStrings(segment.Right as ArraySegment);
                    return string.Format("(INSTR({0}, {1}, {2} + 1) - 1)", strings[0], ToSqlString(segment.Left), strings[1]);
                }
                return string.Format("(INSTR({0}, {1}) - 1)", ToSqlString(segment.Right), ToSqlString(segment.Left));
            }

            // length(CompanyName) eq 19
            protected override string StringifyLength(UnaryFuncSegment segment)
            {
                return string.Format("LENGTH({0})", ToSqlString(segment.Operand));
            }

            // substring(CompanyName,1) eq 'lfreds Futterkiste' // substring(CompanyName,1,6) eq 'lfreds'
            protected override string StringifySubstring(BinaryFuncSegment segment)
            {
                if (segment.Right is ArraySegment)
                {
                    string[] strings = ToSqlStrings(segment.Right as ArraySegment);
                    return string.Format("SUBSTR({0}, {1} + 1, {2})", ToSqlString(segment.Left), strings[0], strings[1]);
                }
                return string.Format("SUBSTR({0}, {1} + 1, {2})", ToSqlString(segment.Left), ToSqlString(segment.Right), int.MaxValue.ToString());
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
                return string.Format("TRIM({0})", ToSqlString(segment.Operand));
            }

            // concat(concat(City,', '), Country) eq 'Berlin, Germany' // concat(City,', ', Country) eq 'Berlin, Germany'
            protected override string StringifyConcat(BinaryFuncSegment segment)
            {
                if (segment.Right is ArraySegment)
                {
                    string[] strings = ToSqlStrings(segment.Right as ArraySegment);
                    return string.Format("({0} || {1})", ToSqlString(segment.Left), string.Join(" || ", strings));
                }
                return string.Format("({0} || {1})", ToSqlString(segment.Left), ToSqlString(segment.Right));
            }

            // year(BirthDate) eq 0
            protected override string StringifyYear(UnaryFuncSegment segment)
            {
                return string.Format("TO_NUMBER(TO_CHAR({0},'YYYY'))", ToSqlString(segment.Operand));
            }

            // month(BirthDate) eq 12
            protected override string StringifyMonth(UnaryFuncSegment segment)
            {
                return string.Format("TO_NUMBER(TO_CHAR({0},'MM'))", ToSqlString(segment.Operand));
            }

            // day(StartTime) eq 8
            protected override string StringifyDay(UnaryFuncSegment segment)
            {
                return string.Format("TO_NUMBER(TO_CHAR({0},'DD'))", ToSqlString(segment.Operand));
            }

            // hour(StartTime) eq 1
            protected override string StringifyHour(UnaryFuncSegment segment)
            {
                return string.Format("TO_NUMBER(TO_CHAR({0},'HH24'))", ToSqlString(segment.Operand));
            }

            // minute(StartTime) eq 0
            protected override string StringifyMinute(UnaryFuncSegment segment)
            {
                return string.Format("TO_NUMBER(TO_CHAR({0},'MI'))", ToSqlString(segment.Operand));
            }

            // second(StartTime) eq 0
            protected override string StringifySecond(UnaryFuncSegment segment)
            {
                return string.Format("TO_NUMBER(TO_CHAR({0},'SS'))", ToSqlString(segment.Operand));
            }

            // totaloffsetminutes(StartTime) eq 60
            // SELECT TO_NUMBER(TO_CHAR(SYSDATE,'FF9')) FROM DUAL; ORA-01821: "date format not recognized"
            // SELECT TO_NUMBER(TO_CHAR(CURRENT_TIMESTAMP,'FF9')) FROM DUAL; OK
            protected override string StringifyFractionalseconds(UnaryFuncSegment segment)
            {
                return string.Format("TO_NUMBER(TO_CHAR({0},'FF9'))", ToSqlString(segment.Operand));
            }

            // date(StartTime) ne date(EndTime)
            protected override string StringifyDate(UnaryFuncSegment segment)
            {
                return string.Format("TO_DATE(TO_CHAR({0}, 'YYYY-MM-DD'),'YYYY-MM-DD')", ToSqlString(segment.Operand));
            }

            // time(StartTime) le StartOfDay
            protected override string StringifyTime(UnaryFuncSegment segment)
            {
                return string.Format("TO_DATE('0001-01-01 '|| TO_CHAR({0},'HH24:MI:SS'), 'YYYY-MM-DD HH24:MI:SS')", ToSqlString(segment.Operand));
            }

            // StartTime ge now()
            protected override string StringifyNow(ParamlessFuncSegment segment)
            {
                return "SYSDATE";
            }

            // totaloffsetminutes(StartTime) eq 60 // timestamp only
            protected override string StringifyTotaloffsetminutes(UnaryFuncSegment segment)
            {
                string format = @"
DECODE(
SIGN(TO_CHAR({0},'TZH')),
-1, TO_NUMBER(TO_CHAR({0},'TZH')) * 60 - TO_NUMBER(TO_CHAR({0},'TZM')), 
TO_NUMBER(TO_CHAR({0},'TZH')) * 60 + TO_NUMBER(TO_CHAR({0},'TZM')))";

                return string.Format(format, ToSqlString(segment.Operand));
            }

            // EndTime eq maxdatetime()
            protected override string StringifyMaxdatetime(ParamlessFuncSegment segment)
            {
                return "TO_DATE('9999-12-31 23:59:59', 'YYYY-MM-DD HH24:MI:SS')";
            }

            // StartTime eq mindatetime()
            protected override string StringifyMindatetime(ParamlessFuncSegment segment)
            {
                return "TO_DATE('-4713-01-01 00:00:00', '-YYYY-MM-DD HH24:MI:SS')";
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
                return string.Format("CEIL({0})", ToSqlString(segment.Operand));
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
