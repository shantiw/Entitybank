using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.DataObjects;
using XData.Data.OData;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    // Database.OData.cs
    public abstract partial class Database
    {
        private QueryGenerator _queryGenerator = null;
        internal protected QueryGenerator QueryGenerator
        {
            get
            {
                if (_queryGenerator == null)
                {
                    _queryGenerator = CreateQueryGenerator();
                }
                return _queryGenerator;
            }
        }

        private QueryExpandResultGetter _queryExpandResultGetter = null;
        protected QueryExpandResultGetter QueryExpResultGetter
        {
            get
            {
                if (_queryExpandResultGetter == null)
                {
                    _queryExpandResultGetter = CreateQueryExpandResultGetter();
                }
                return _queryExpandResultGetter;
            }
        }

        protected abstract QueryGenerator CreateQueryGenerator();

        protected abstract QueryExpandResultGetter CreateQueryExpandResultGetter();

        internal protected DataTable GetDefault(Query query)
        {
            string sql = QueryGenerator.GenerateDefaultStatement(query, out IReadOnlyDictionary<string, object> constants);
            DataTable table = ExecuteDataTable(sql, CreateParameters(constants));
            table.TableName = query.Entity;
            //RecoverColumnNamesCaseSensitivity(table, query.Select.Properties);
            return table;
        }

        internal protected int Count(Query query)
        {
            string sql = QueryGenerator.GenerateCountStatement(query, out IReadOnlyDictionary<string, object> dbParameterValues);
            DbParameter[] dbParameters = CreateParameters(dbParameterValues);
            object value = ExecuteScalar(sql, dbParameters);
            return (int)Convert.ChangeType(value, TypeCode.Int32);
        }

        internal protected DataTable GetCollection(Query query)
        {
            string sql;
            IReadOnlyDictionary<string, object> dbParameterValues;
            if (query.Top != 0 || query.Skip != 0)
            {
                sql = QueryGenerator.GeneratePagingStatement(query, out dbParameterValues);
            }
            else
            {
                sql = QueryGenerator.GenerateSelectStatement(query, out dbParameterValues);
            }
            DbParameter[] dbParameters = CreateParameters(dbParameterValues);
            DataTable table = ExecuteDataTable(sql, dbParameters);
            table.TableName = query.Schema.GetEntitySchema(query.Entity).Attribute(SchemaVocab.Collection).Value;
            //RecoverColumnNamesCaseSensitivity(table, query.Select.Properties);
            return table;
        }

        internal protected static void RecoverColumnNamesCaseSensitivity(DataTable table, string[] selectProperties)
        {
            // NAME,Name
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (string property in selectProperties)
            {
                dict.Add(property.ToUpper(), property);
            }

            foreach (DataColumn column in table.Columns)
            {
                column.ColumnName = dict[column.ColumnName.ToUpper()];
            }
        }

        internal protected ResultNode GetCollection(QueryExpand queryExpand)
        {
            return QueryExpResultGetter.GetCollection(queryExpand);
        }


    }
}
