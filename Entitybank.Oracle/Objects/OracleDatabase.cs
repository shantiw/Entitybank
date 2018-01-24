using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Modification;

namespace XData.Data.Objects
{
    // Oracle 10.2.0.4.0
    // not supported: LONG, LONG VARCHAR, LONG RAW
    public partial class OracleDatabase : Database
    {
        public override string ParameterPrefix => ":";

        public override DateTime GetNow()
        {
            return (DateTime)ExecuteScalar("SELECT SYSDATE FROM DUAL");
        }

        public override DateTime GetUtcNow()
        {
            return GetNow().ToUniversalTime();
        }

        public OracleDatabase(string connectionString) : base(connectionString)
        {
        }

        protected override DbConnection CreateConnection(string connectionString)
        {
            return new OracleConnection(connectionString);
        }

        protected override DbDataAdapter CreateDataAdapter()
        {
            return new OracleDataAdapter();
        }

        public override DbParameter CreateParameter(string parameter, object value)
        {
            return parameter.StartsWith(ParameterPrefix) ? new OracleParameter(parameter, value) : new OracleParameter(ParameterPrefix + parameter, value);
        }

        protected override ModificationGenerator CreateModificationGenerator()
        {
            return new OracleModificationGenerator();
        }

        protected override int ExecuteInsertCommand(string sql, object[] parameters, out object autoIncrementValue)
        {
            throw new NotSupportedException();
        }

        protected override object FetchSequence(string sequenceName)
        {
            string sql = string.Format("SELECT \"{0}\".NEXTVAL FROM DUAL", sequenceName);
            return ExecuteScalar(sql);
        }

        protected override object[] FetchSequences(string sequenceName, int size)
        {
            // SELECT {sequenceName}.NEXTVAL FROM DUAL CONNECT BY LEVEL/ROWNUM <= {size};
            string sql = string.Format("SELECT \"{0}\".NEXTVAL FROM DUAL CONNECT BY LEVEL <= {1}", sequenceName, size);
            DataTable table = ExecuteDataTable(sql);
            List<object> list = new List<object>();
            foreach (DataRow row in table.Rows)
            {
                list.Add(row[0]);
            }
            return list.ToArray();
        }


    }
}
