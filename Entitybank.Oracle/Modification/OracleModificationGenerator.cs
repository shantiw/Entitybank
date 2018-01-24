using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Modification
{
    public partial class OracleModificationGenerator : ModificationGenerator
    {
        protected override string DecorateTableName(string table)
        {
            return "\"" + table + "\"";
        }

        protected override string DecorateColumnName(string column)
        {
            return "\"" + column + "\"";
        }

        protected override string DecorateDbParameterName(string parameter)
        {
            return ":" + parameter;
        }

        public override string GenerateIsExistsStatement(Dictionary<string, object> propertyValues, XElement entitySchema, XElement keySchema,
            out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            string select = GenerateFindStatement(propertyValues, entitySchema, keySchema, out dbParameterValues);

            string sql = string.Format("SELECT 1 FROM DUAL WHERE EXISTS ({select})", select);
            return sql;
        }


    }
}
