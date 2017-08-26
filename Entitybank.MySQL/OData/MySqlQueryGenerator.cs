using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.DataObjects;

namespace XData.Data.OData
{
    public partial class MySqlQueryGenerator : QueryGenerator
    {
        protected override Where CreateWhere(Query query, Table table)
        {
            return new MySqlWhere(query, table, this);
        }

        protected override string DecorateColumnAlias(string columnAlias)
        {
            return string.Format("`{0}`", columnAlias);
        }

        protected override string DecorateColumnName(string column)
        {
            return string.Format("`{0}`", column);
        }

        protected override string DecorateParameterName(string parameter, IReadOnlyDictionary<string, string> upperParamNameMapping)
        {
            return "?" + base.DecorateParameterName(parameter, upperParamNameMapping).Substring(1);
        }

        protected override string DecorateTableAlias(string tableAlias)
        {
            return tableAlias;
        }

        protected override string DecorateTableName(string table)
        {
            return string.Format("`{0}`", table);
        }

        protected override PagingClauseCollection GeneratePagingClauseCollection(Query query, out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            long top = (query.Top == 0) ? long.MaxValue : query.Top;

            SelectClauseCollection selectClauses = GenerateSelectClauseCollection(query, out dbParameterValues);
            PagingClauseCollection PagingClauses = new PagingClauseCollection(selectClauses)
            {
                Clauses = new string[1]
            };
            PagingClauses.Clauses[0] = string.Format("LIMIT {0},{1}", query.Skip, top);

            return PagingClauses;
        }


    }
}
