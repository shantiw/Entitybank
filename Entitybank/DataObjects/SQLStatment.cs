using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.DataObjects
{
    public class SQLStatment
    {
        public string Sql { get; private set; }
        public object[] Parameters { get; private set; } = new object[0];

        public SQLStatment(string sql, params object[] parameters)
        {
            Sql = sql;
            Parameters = parameters;
        }
    }
}
