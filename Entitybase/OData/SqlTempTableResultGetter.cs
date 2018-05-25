using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Helpers;
using XData.Data.DataObjects;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.OData
{
    public class SqlTempTableResultGetter : TempTableResultGetter
    {
        public SqlTempTableResultGetter(Database database) : base(database)
        {
        }

        private int _tempTableCount = 0;
        private string GetTempTableName()
        {
            _tempTableCount++;
            string tempTableName = string.Format("#V{0}", _tempTableCount);
            return DecorateTableAlias(tempTableName);
        }

        protected override void SetRootSelectStatments(TempTableNode root, QueryExpand queryExpand)
        {
            Query query = queryExpand.Query;

            root.TempTableName = GetTempTableName();
            SelectClauseCollection clauses = GenerateSelectClauseCollection(query, out DbParameter[] dbParameters);
            List<string> list = new List<string>
            {
                clauses.Select,
                "INTO " + root.TempTableName,
                clauses.From,
                string.Join(" ", clauses.LeftJoins),
                clauses.Where ?? string.Empty,
                //clauses.OrderBy ?? string.Empty
            };

            string sql = string.Join(" ", list);
            root.BeforeExecuteStatments.Add(new SQLStatment(sql, dbParameters));

            //
            string fetchSql = string.Format("SELECT * FROM {0}", root.TempTableName);
            if (query.Orderby != null)
            {
                IEnumerable<string> orderby = query.Orderby.Orders.Select(order => order.Property + ((order is DescendingOrder) ? " DESC" : " ASC"));
                fetchSql += " ORDER BY " + string.Join(",", orderby);
            }
            root.FetchTableStatment = new SQLStatment(fetchSql);
        }

        protected override void SetRootPagingStatments(TempTableNode root, QueryExpand queryExpand)
        {
            Query query = queryExpand.Query;

            root.TempTableName = GetTempTableName();
            PagingClauseCollection clauses = GeneratePagingClauseCollection(query, out DbParameter[] dbParameters);
            List<string> list = new List<string>
            {
                clauses.Select,
                "INTO " + root.TempTableName,
                clauses.From,
                string.Join(" ", clauses.LeftJoins),
                clauses.Where ?? string.Empty,
                clauses.OrderBy
            };
            list.AddRange(clauses.Clauses);

            string sql = string.Join(" ", list);
            root.BeforeExecuteStatments.Add(new SQLStatment(sql, dbParameters));

            //
            string fetchSql = string.Format("SELECT * FROM {0}", root.TempTableName);
            root.FetchTableStatment = new SQLStatment(fetchSql);
        }

        protected override void SetNodeStatments(TempTableNode node, ExpandNode queryNode)
        {
            string primarySql = GeneratePrimarySql(node, queryNode.Relationship, queryNode.Query, out DbParameter[] dbParameters,
                out string tempTableName1, out string rowNumberAlias);
            node.BeforeExecuteStatments.Add(new SQLStatment(primarySql, dbParameters));

            string distinctSql = GenerateDistinctSql(node, queryNode.Query, tempTableName1, rowNumberAlias);
            node.BeforeExecuteStatments.Add(new SQLStatment(distinctSql));

            string fetchTableSql = string.Format("SELECT * FROM {0}", node.TempTableName);
            node.FetchTableStatment = new SQLStatment(fetchTableSql);

            node.AfterExecuteStatments.AddRange(GenerateDropTempTableStatements(tempTableName1));
        }

        //SELECT [T].[Id], [T].[RoleName], [S1].[EmployeeId] [EmployeeId], ROW_NUMBER() OVER(ORDER BY [S1].[EmployeeId], [T].Id) [RowNumber]
        //INTO [#V2]
        //FROM [Roles] [T]
        //INNER JOIN [UsersRoles] [S] ON [T].[Id] = [S].[RoleId]
        //INNER JOIN [Users] [S1] ON [S1].[Id] = [S].[UserId]
        //INNER JOIN [#V1] S2 ON S2.Id = [S1].[EmployeeId]
        //LEFT JOIN...
        //WHERE ...
        private string GeneratePrimarySql(TempTableNode node, Relationship relationship, Query query, out DbParameter[] dbParameters,
            out string tempTableName1, out string rowNumberAlias)
        {
            ForeignKey[] foreignKeys = CreateUndirectedForeignKeys(relationship, query.Schema);
            IEnumerable<string> innerJoins = GenerateInnerJoins(foreignKeys, node.ParentTempTableName);

            IEnumerable<string> relatedKeyColProps = GetRelatedKeyColProps(node, foreignKeys[0], out IEnumerable<string> relatedKeyCols);
            IEnumerable<string> overOrderByCols = GetOverOrderByCols(relatedKeyCols, query);

            rowNumberAlias = GetRowNumberAlias(query.Select, node.RelatedKey);

            //ROW_NUMBER() OVER(ORDER BY [S1].[EmployeeId], [T].[Id]) [RowNumber]
            string rowNumberSelect = string.Format("ROW_NUMBER() OVER(ORDER BY {0}) {1}", string.Join(", ", overOrderByCols), rowNumberAlias);

            tempTableName1 = GetTempTableName();
            SelectClauseCollection clauses = GenerateSelectClauseCollection(query, out dbParameters);
            List<string> list = new List<string>
            {
                string.Join(", ", clauses.Select, string.Join(", ", relatedKeyColProps), rowNumberSelect),
                string.Format("INTO {0}", tempTableName1),
                clauses.From,
                string.Join(" ", innerJoins),
                string.Join(" ", clauses.LeftJoins),
                clauses.Where ?? string.Empty,
            };

            return string.Join(" ", list);
        }

        private IEnumerable<string> GetOverOrderByCols(IEnumerable<string> relatedKeyCols, Query query)
        {
            List<string> overOrder = new List<string>(relatedKeyCols);
            IEnumerable<string> keyColumns = query.Schema.GetKeySchema(query.Entity).Elements(SchemaVocab.Property).Select(x => x.Attribute(SchemaVocab.Column).Value);
            foreach (string column in keyColumns)
            {
                overOrder.Add(DecorateColumnName(column, Table.Default_Table_Alias_Prefix));
            }
            return overOrder;
        }

        private string GetRowNumberAlias(Select select, Dictionary<string, string> relatedKey)
        {
            return GetRowNumberAlias("RowNumber", select, relatedKey);
        }

        //SELECT [Id], [RoleName], [EmployeeId]
        //INTO [#V3]
        //FROM [#V2]
        //WHERE [RowNumber] IN
        //(SELECT MIN([RowNumber]) [RowNumber] FROM [#V2] GROUP BY [EmployeeId], [Id])
        private string GenerateDistinctSql(TempTableNode node, Query query, string tempTableName1, string rowNumberAlias)
        {
            IEnumerable<string> groupbyProps = GetDistinctGroupByProps(node.DistinctKey);
            string groupby = string.Join(", ", groupbyProps);

            //SELECT MIN([RowNumber]) [RowNumber] FROM [#V2] GROUP BY [EmployeeId], [Id]
            string groupbySql = string.Format("SELECT MIN({0}) {0} FROM {1} GROUP BY {2}", rowNumberAlias, tempTableName1, groupby);

            //
            node.TempTableName = GetTempTableName();
            IEnumerable<string> selectProps = GetDistinctSelectProps(query.Select.Properties, node.RelatedKey);
            List<string> list = new List<string>()
            {
                string.Format("SELECT {0}", string.Join(", ", selectProps)),
                string.Format("INTO {0}", node.TempTableName),
                string.Format("FROM {0}", tempTableName1),
                string.Format("WHERE {0} IN ({1})", rowNumberAlias, groupbySql)
            };

            return string.Join(" ", list);
        }

        protected override int DatabaseExecuteNonQuery(string sql, params object[] parameters)
        {
            return base.DatabaseExecuteNonQuery(ExcludeParameters(sql, parameters));
        }

        private string ExcludeParameters(string sql, object[] parameters)
        {
            string val = EncodeString(sql, out Dictionary<string, string> placeholders);
            foreach (object parameter in parameters)
            {
                DbParameter dbParameter = parameter as DbParameter;
                string ParameterName = dbParameter.ParameterName;
                object value = dbParameter.Value;
                string sqlString = ToSqlString(value, value.GetType());
                val = val.Replace(ParameterName, sqlString);
            }
            return DecodeConstant(val, placeholders);
        }

        // single quote "''" // 'I''m  fine' // '''Im fine' // 'Im fine'''
        private static string EncodeString(string value, out Dictionary<string, string> placeholders)
        {
            return AnalysisHelper.EncodeString(value, out placeholders);
        }

        private static string DecodeConstant(string value, Dictionary<string, string> placeholders)
        {
            return AnalysisHelper.DecodeString(value, placeholders);
        }

        private static string ToSqlString(object value, Type type)
        {
            if (type == typeof(string)) return ToSqlString((string)value);
            if (type == typeof(DateTime)) return ToSqlString((DateTime)value);
            if (type == typeof(bool)) return ToSqlString((bool)value);
            if (type == typeof(Guid)) return ToSqlString((Guid)value);
            if (type == typeof(byte[])) return ToSqlString((byte[])value);
            if (IsNumeric(type)) return value.ToString();

            throw new NotSupportedException(type.ToString());
        }

        private static bool IsNumeric(Type type)
        {
            return TypeHelper.IsNumeric(type);
        }

        private static string ToSqlString(string value)
        {
            string val = value.Replace("'", "''");
            return "'" + val + "'";
        }

        private static string ToSqlString(DateTime value)
        {
            if (value.Millisecond == 0) return "'" + value.ToString("yyyy-MM-dd HH:mm:ss") + "'";
            return "'" + value.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF") + "'";
        }

        // 9B9F5400-04FC-4EE6-B5C1-B1DD19361DC5
        private static string ToSqlString(Guid value)
        {
            return "'" + value.ToString("D").ToUpper() + "'";
        }

        private static string ToSqlString(bool value)
        {
            return value ? "1" : "0";
        }

        private static string ToSqlString(byte[] value)
        {
            return "0x" + BitConverter.ToString(value).Replace("-", string.Empty);
        }


    }
}
