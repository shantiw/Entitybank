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
    public class MySqlTempTableResultGetter : TempTableResultGetter
    {
        public MySqlTempTableResultGetter(Database database) : base(database)
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
            string tempTableName = string.Format("temp{0}", _tempTableCount);
            while (_tableNames.Contains(tempTableName))
            {
                _tempTableCount++;
                tempTableName = string.Format("temp{0}", _tempTableCount);
            }
            return DecorateTableAlias(tempTableName);
        }

        protected override void SetRootSelectStatments(TempTableNode root, QueryExpand queryExpand)
        {
            Query query = queryExpand.Query;

            root.TempTableName = GetTempTableName();
            SelectClauseCollection clauses = GenerateSelectClauseCollection(query, out DbParameter[] dbParameters);
            List<string> list = new List<string>
            {
                string.Format("Create TEMPORARY TABLE {0}", root.TempTableName),
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
                string.Format("Create TEMPORARY TABLE {0}", root.TempTableName),
                clauses.Select,
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

        protected override void SetNodeStatments(TempTableNode node, ExpandNode expandNode)
        {
            Query query = expandNode.Query;

            string primarySql = GeneratePrimarySql(node, expandNode.Relationship, query, out DbParameter[] dbParameters,
                out string tempTableName1);
            node.BeforeExecuteStatments.Add(new SQLStatment(primarySql, dbParameters));

            //
            string auto_id = GetAutoIdColumnName(query);

            //ALTER TABLE temp2 ADD auto_id int auto_increment key
            string auto_increment_sql = string.Format("ALTER TABLE temp2 ADD {0} int auto_increment key", auto_id);
            node.BeforeExecuteStatments.Add(new SQLStatment(auto_increment_sql));

            //
            IEnumerable<string> groupbyProps = GetDistinctGroupByProps(node.DistinctKey);
            string groupby = string.Join(", ", groupbyProps);

            //Create TEMPORARY TABLE temp3
            //SELECT MIN(auto_id) auto_id FROM temp2 GROUP BY EmployeeId, Id
            string tempTableName2 = GetTempTableName();
            string groupby_sql = string.Format("SELECT MIN({0}) {0} FROM {1} GROUP BY {2}", auto_id, tempTableName1, groupby);
            groupby_sql = string.Format("Create TEMPORARY TABLE {0} ", tempTableName2) + groupby_sql;
            node.BeforeExecuteStatments.Add(new SQLStatment(groupby_sql));

            //ALTER TABLE temp3 ADD PRIMARY KEY (auto_id)
            string add_key_sql = string.Format("ALTER TABLE {0} ADD PRIMARY KEY ({1})", tempTableName2, auto_id);
            node.BeforeExecuteStatments.Add(new SQLStatment(add_key_sql));

            //
            IEnumerable<string> selectProps = GetDistinctSelectProps(query.Select.Properties, node.RelatedKey);

            //Create TEMPORARY TABLE temp4
            //SELECT [Id], [RoleName], [EmployeeId]
            //FROM temp2
            //WHERE auto_id IN (SELECT auto_id FROM temp3)
            node.TempTableName = GetTempTableName();
            List<string> list = new List<string>()
            {
                string.Format("Create TEMPORARY TABLE {0}", node.TempTableName),
                string.Format("SELECT {0}", string.Join(", ", selectProps)),
                string.Format("FROM {0}", tempTableName1),
                string.Format("WHERE {0} IN (SELECT {0} FROM {1})", auto_id, tempTableName2)
            };
            string distinctSql = string.Join(" ", list);
            node.BeforeExecuteStatments.Add(new SQLStatment(distinctSql));

            //SELECT * FROM temp4
            string fetchSql = string.Format("SELECT * FROM {0}", node.TempTableName);
            node.FetchTableStatment = new SQLStatment(fetchSql);

            //
            node.AfterExecuteStatments.AddRange(GenerateDropTempTableStatements(tempTableName1));
            node.AfterExecuteStatments.AddRange(GenerateDropTempTableStatements(tempTableName2));
        }

        //Create TEMPORARY TABLE temp2
        //SELECT T.Id, T.RoleName, S1.EmployeeId EmployeeId
        //FROM Roles T
        //INNER JOIN UsersRoles S ON T.Id = S.RoleId
        //INNER JOIN Users S1 ON S1.Id = S.UserId
        //INNER JOIN #V1 S2 ON S2.Id = S1.EmployeeId
        //LEFT JOIN...
        //WHERE ...
        private string GeneratePrimarySql(TempTableNode node, Relationship relationship, Query query, out DbParameter[] dbParameters,
          out string tempTableName1)
        {
            ForeignKey[] foreignKeys = CreateUndirectedForeignKeys(relationship, query.Schema);
            IEnumerable<string> innerJoins = GenerateInnerJoins(foreignKeys, node.ParentTempTableName);

            IEnumerable<string> relatedKeyColProps = GetRelatedKeyColProps(node, foreignKeys[0], out IEnumerable<string> relatedKeyCols);

            tempTableName1 = GetTempTableName();
            SelectClauseCollection clauses = GenerateSelectClauseCollection(query, out dbParameters);
            List<string> list = new List<string>
            {
                string.Format("Create TEMPORARY TABLE {0}", tempTableName1),
                string.Join(", ", clauses.Select, string.Join(", ", relatedKeyColProps)),
                clauses.From,
                string.Join(" ", innerJoins),
                string.Join(" ", clauses.LeftJoins),
                clauses.Where ?? string.Empty
            };

            return string.Join(" ", list);
        }

        private static string GetAutoIdColumnName(Query query)
        {
            string auto_id = "auto_id";
            int i = 0;
            while (query.Select.Properties.Contains(auto_id))
            {
                auto_id = "auto_id" + i.ToString();
                i++;
            }
            return auto_id;
        }


    }
}
