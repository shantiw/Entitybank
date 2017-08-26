using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.DataObjects;
using XData.Data.Schema;

namespace XData.Data.OData
{
    public partial class SqlQueryGenerator : QueryGenerator
    {
        internal protected override string DecorateTableName(string table)
        {
            return string.Format("[{0}]", table);
        }

        internal protected override string DecorateTableAlias(string tableAlias)
        {
            return tableAlias;
        }

        internal protected override string DecorateColumnName(string column)
        {
            return string.Format("[{0}]", column);
        }

        internal protected override string DecorateColumnAlias(string columnAlias)
        {
            return string.Format("[{0}]", columnAlias);
        }

        protected override string DecorateParameterName(string parameter, IReadOnlyDictionary<string, string> upperParamNameMapping)
        {
            return base.DecorateParameterName(parameter, upperParamNameMapping);
        }

        protected override Where CreateWhere(Query query, Table table)
        {
            return new SqlWhere(query, table, this);
        }

        internal protected override PagingClauseCollection GeneratePagingClauseCollection(Query query, out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            SelectClauseCollection selectClauses = GenerateSelectClauseCollection(query, out dbParameterValues);
            PagingClauseCollection pagingClauses = new PagingClauseCollection(selectClauses);

            List<string> clauses = new List<string>
            {
                string.Format("OFFSET {0} ROWS", query.Skip)
            };
            if (query.Top != 0)
            {
                clauses.Add(string.Format("FETCH NEXT {0} ROWS ONLY", query.Top));
            }
            pagingClauses.Clauses = clauses.ToArray();

            return pagingClauses;
        }


    }
}
