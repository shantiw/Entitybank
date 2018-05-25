using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.DataObjects;
using XData.Data.Objects;

namespace XData.Data.OData
{
    public partial class OracleQueryGenerator : QueryGenerator
    {
        protected override Where CreateWhere(Query query, Table table)
        {
            return new OracleWhere(query, table, this);
        }

        protected override string DecorateColumnAlias(string columnAlias)
        {
            return "\"" + columnAlias + "\"";
        }

        protected override string DecorateColumnName(string column)
        {
            return "\"" + column + "\"";
        }

        protected override string DecorateParameterName(string parameter, IReadOnlyDictionary<string, string> upperParamNameMapping)
        {
            return ":" + base.DecorateParameterName(parameter, upperParamNameMapping).Substring(1);
        }

        protected override string DecorateTableAlias(string tableAlias)
        {
            return tableAlias;
        }

        protected override string DecorateTableName(string table)
        {
            return "\"" + table + "\"";
        }

        //SELECT *
        //FROM (SELECT SELECT_TABLE_ALIAS.*, ROWNUM ROW_NUM
        //      FROM ({0}) SELECT_TABLE_ALIAS
        //      WHERE ROWNUM <= {1}) PAGING_TABLE_ALIAS
        //WHERE PAGING_TABLE_ALIAS.ROW_NUM >= {2}"
        protected override PagingClauseCollection GeneratePagingClauseCollection(Query query, out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            long top = (query.Top == 0) ? long.MaxValue : query.Top;

            string selectSql = GenerateSelectStatement(query, out dbParameterValues);

            string select = string.Format("SELECT {0}", string.Join(",", query.Select.Properties));

            string rowNumberAlias = GetRowNumberAlias(query.Select);

            string from = @"
FROM (SELECT SELECT_TABLE_ALIAS.*, ROWNUM {0}
      FROM ({1}) SELECT_TABLE_ALIAS
      WHERE ROWNUM <= {2}) PAGING_TABLE_ALIAS";
            from = string.Format(from, rowNumberAlias, selectSql, query.Skip + top);

            string where = string.Format("WHERE PAGING_TABLE_ALIAS.{0} >= {1}", rowNumberAlias, query.Skip + 1);

            return new PagingClauseCollection(null, select, from, null, where, null);
        }

        private static string GetRowNumberAlias(Select select)
        {
            string row_num = "ROW_NUM";
            int i = 0;
            while (select.Properties.Contains(row_num))
            {
                row_num = "ROW_NUM" + i.ToString();
                i++;
            }
            return row_num;
        }


    }
}
