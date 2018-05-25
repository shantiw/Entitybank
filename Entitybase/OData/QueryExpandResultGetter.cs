using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.DataObjects;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.OData
{
    public abstract class QueryExpandResultGetter
    {
        protected readonly Database Database;

        public QueryExpandResultGetter(Database database)
        {
            Database = database;
        }

        public abstract ResultNode GetCollection(QueryExpand queryExpand);

        protected SelectClauseCollection GenerateSelectClauseCollection(Query query, out DbParameter[] dbParameters)
        {
            SelectClauseCollection clauses = GenerateSelectClauseCollection(query, out IReadOnlyDictionary<string, object> dbParameterValues);
            dbParameters = CreateParameters(dbParameterValues);
            return clauses;
        }

        private SelectClauseCollection GenerateSelectClauseCollection(Query query, out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            return Database.QueryGenerator.GenerateSelectClauseCollection(query, out dbParameterValues); ;
        }

        protected PagingClauseCollection GeneratePagingClauseCollection(Query query, out DbParameter[] dbParameters)
        {
            PagingClauseCollection clauses = Database.QueryGenerator.GeneratePagingClauseCollection(query, out IReadOnlyDictionary<string, object> dbParameterValues);
            dbParameters = CreateParameters(dbParameterValues);
            return clauses;
        }

        private PagingClauseCollection GeneratePagingClauseCollection(Query query, out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            return Database.QueryGenerator.GeneratePagingClauseCollection(query, out dbParameterValues);
        }

        protected DbParameter[] CreateParameters(IReadOnlyDictionary<string, object> dbParameterValues)
        {
            return Database.CreateParameters(dbParameterValues);
        }

        protected static ForeignKey[] CreateUndirectedForeignKeys(Relationship relationship, XElement schema)
        {
            return relationship.CreateUndirectedForeignKeys(schema);
        }

        protected static IEnumerable<string> GetKeyProperties(Query query)
        {
            return query.Schema.GetKeySchema(query.Entity).Elements(SchemaVocab.Property).Select(x => x.Attribute(SchemaVocab.Name).Value);
        }

        protected string DecorateTableName(string table)
        {
            return Database.QueryGenerator.DecorateTableName(table);
        }

        protected string DecorateTableAlias(string tableAlias)
        {
            return Database.QueryGenerator.DecorateTableAlias(tableAlias);
        }

        protected string DecorateColumnName(string column)
        {
            return Database.QueryGenerator.DecorateColumnName(column);
        }

        protected string DecorateColumnAlias(string columnAlias)
        {
            return Database.QueryGenerator.DecorateColumnAlias(columnAlias);
        }

        protected string DecorateColumnName(string column, string tableAlias)
        {
            return string.Format("{0}.{1}", DecorateTableAlias(tableAlias), DecorateColumnName(column));
        }


    }
}
