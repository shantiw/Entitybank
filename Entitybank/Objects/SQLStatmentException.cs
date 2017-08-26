using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Objects
{
    public class SQLStatmentException : DbException
    {
        public string SQL { get; private set; }
        public object[] Parameters { get; private set; }

        public SQLStatmentException(string message, string sql, params object[] parameters)
            : base(message)
        {
            SQL = sql;
            Parameters = parameters;
        }

        public SQLStatmentException(Exception innerException, string sql, params object[] parameters)
            : base(innerException.Message, innerException)
        {
            SQL = sql;
            Parameters = parameters;
        }
    }

    public class OptimisticConcurrencyException : SQLStatmentException
    {
        public OptimisticConcurrencyException(string message, string sql, params object[] parameters)
            : base(message, sql, parameters)
        {
        }
    }


}
