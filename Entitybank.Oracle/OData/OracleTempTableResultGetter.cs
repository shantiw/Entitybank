using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.DataObjects;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.OData
{
    public class OracleTempTableResultGetter : TempTableResultGetter
    {
        public OracleTempTableResultGetter(Database database) : base(database)
        {
        }

        public override ResultNode GetCollection(QueryExpand queryExpand)
        {
            _tableNames = queryExpand.Schema.Elements(SchemaVocab.Entity).Select(x => x.Attribute(SchemaVocab.Table).Value);
            return base.GetCollection(queryExpand);
        }

        private IEnumerable<string> _tableNames;
        private int _tempTableCount = 0;
        private string GetTempTableName()
        {
            _tempTableCount++;
            string tempTableName = string.Format("TEMP{0}", _tempTableCount);
            while (_tableNames.Contains(tempTableName))
            {
                _tempTableCount++;
                tempTableName = string.Format("TEMP{0}", _tempTableCount);
            }
            return tempTableName;
        }

        protected override SQLStatment[] GenerateDropTempTableStatements(string tempTableName)
        {
            SQLStatment[] statments = new SQLStatment[2];
            statments[0] = new SQLStatment(string.Format("TRUNCATE TABLE {0}", tempTableName));
            statments[1] = new SQLStatment(string.Format("DROP TABLE {0} PURGE", tempTableName));
            return statments;
        }

        protected override Order[] GetOrders(Query query)
        {
            List<Order> orders = new List<Order>();
            if (query.Orderby == null)
            {
                IEnumerable<string> keyProperties = GetKeyProperties(query);
                foreach (string prop in keyProperties)
                {
                    orders.Add(new AscendingOrder(prop));
                }
                return orders.ToArray();
            }
            else
            {
                return query.Orderby.Orders; ;
            }
        }

        protected override void SetRootSelectStatments(TempTableNode root, QueryExpand queryExpand)
        {
            Query query = queryExpand.Query;

            root.TempTableName = GetTempTableName();
            SelectClauseCollection clauses = GenerateSelectClauseCollection(query, out DbParameter[] dbParameters);
            List<string> list = new List<string>
            {
                string.Format("CREATE GLOBAL TEMPORARY TABLE {0} ON COMMIT PRESERVE ROWS AS", root.TempTableName),
                clauses.Select,
                clauses.From,
                string.Join(" ", clauses.LeftJoins),
                clauses.Where ?? string.Empty,
                clauses.OrderBy ?? string.Empty
            };

            string sql = string.Join(" ", list);
            root.BeforeExecuteStatments.Add(new SQLStatment(sql, dbParameters));

            //
            string fetchSql = string.Format("SELECT * FROM {0}", root.TempTableName);
            root.FetchTableStatment = new SQLStatment(fetchSql);
        }

        protected override void SetRootPagingStatments(TempTableNode root, QueryExpand queryExpand)
        {
            Query query = queryExpand.Query;

            root.TempTableName = GetTempTableName();
            PagingClauseCollection clauses = GeneratePagingClauseCollection(query, out DbParameter[] dbParameters);
            List<string> list = new List<string>
            {
                string.Format("CREATE GLOBAL TEMPORARY TABLE {0} ON COMMIT PRESERVE ROWS AS", root.TempTableName),
                clauses.Select,
                clauses.From,
                string.Join(" ", clauses.LeftJoins),
                clauses.Where ?? string.Empty,
                clauses.OrderBy ?? string.Empty
            };
            list.AddRange(clauses.Clauses);

            string sql = string.Join(" ", list);
            root.BeforeExecuteStatments.Add(new SQLStatment(sql, dbParameters));

            //
            string fetchSql = string.Format("SELECT * FROM {0}", root.TempTableName);
            root.FetchTableStatment = new SQLStatment(fetchSql);
        }

        protected override void SetNodeStatments(TempTableNode node, QueryNode queryNode)
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

        //CREATE GLOBAL TEMPORARY TABLE TEMP2 ON COMMIT PRESERVE ROWS AS
        //SELECT T.Id, T.RoleName, S1.EmployeeId EmployeeId, ROWNUM ROW_NUM
        //FROM Roles T
        //INNER JOIN UsersRoles S ON T.Id = S.RoleId
        //INNER JOIN Users S1 ON S1.Id = S.UserId
        //INNER JOIN #V1 S2 ON S2.Id = S1.EmployeeId
        //LEFT JOIN...
        //WHERE ...
        private string GeneratePrimarySql(TempTableNode node, Relationship relationship, Query query, out DbParameter[] dbParameters,
             out string tempTableName1, out string rowNumberAlias)
        {
            ForeignKey[] foreignKeys = CreateUndirectedForeignKeys(relationship, query.Schema);
            IEnumerable<string> innerJoins = GenerateInnerJoins(foreignKeys, node.ParentTempTableName);

            IEnumerable<string> relatedKeyColProps = GetRelatedKeyColProps(node, foreignKeys[0], out IEnumerable<string> relatedKeyCols);

            rowNumberAlias = GetRowNumberAlias(query.Select, node.RelatedKey);

            tempTableName1 = GetTempTableName();
            SelectClauseCollection clauses = GenerateSelectClauseCollection(query, out dbParameters);
            List<string> list = new List<string>
            {
                string.Format("CREATE GLOBAL TEMPORARY TABLE {0} ON COMMIT PRESERVE ROWS AS", tempTableName1),
                string.Join(", ", clauses.Select, string.Join(", ", relatedKeyColProps), string.Format("ROWNUM {0}", rowNumberAlias)),
                clauses.From,
                string.Join(" ", innerJoins),
                string.Join(" ", clauses.LeftJoins),
                clauses.Where ?? string.Empty,
            };

            return string.Join(" ", list);
        }

        private string GetRowNumberAlias(Select select, Dictionary<string, string> relatedKey)
        {
            return GetRowNumberAlias("ROW_NUM", select, relatedKey);
        }

        //CREATE GLOBAL TEMPORARY TABLE TEMP3 ON COMMIT PRESERVE ROWS AS
        //SELECT Id, RoleName, EmployeeId
        //FROM TEMP2
        //WHERE ROW_NUM IN
        //(SELECT MIN(ROW_NUM) ROW_NUM FROM TEMP2 GROUP BY EmployeeId, Id)
        private string GenerateDistinctSql(TempTableNode node, Query query, string tempTableName1, string rowNumberAlias)
        {
            IEnumerable<string> groupbyProps = GetDistinctGroupByProps(node.DistinctKey);
            string groupby = string.Join(", ", groupbyProps);

            //SELECT MIN(ROW_NUM) ROW_NUM FROM TEMP2 GROUP BY EmployeeId, Id
            string groupbySql = string.Format("SELECT MIN({0}) {0} FROM {1} GROUP BY {2}", rowNumberAlias, tempTableName1, groupby);

            //
            node.TempTableName = GetTempTableName();
            IEnumerable<string> selectProps = GetDistinctSelectProps(query.Select.Properties, node.RelatedKey);
            List<string> list = new List<string>()
            {
                string.Format("CREATE GLOBAL TEMPORARY TABLE {0} ON COMMIT PRESERVE ROWS AS", node.TempTableName),
                string.Format("SELECT {0}", string.Join(", ", selectProps)),
                string.Format("FROM {0}", tempTableName1),
                string.Format("WHERE {0} IN ({1})", rowNumberAlias, groupbySql)
            };

            return string.Join(" ", list);
        }


    }
}
