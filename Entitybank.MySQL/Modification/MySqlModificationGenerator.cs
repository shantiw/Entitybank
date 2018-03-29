using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Modification
{
    public class MySqlModificationGenerator : ModificationGenerator
    {
        protected override int DbParamLimit => 65535;

        protected override string DecorateTableName(string table)
        {
            return string.Format("`{0}`", table);
        }

        protected override string DecorateColumnName(string column)
        {
            return string.Format("`{0}`", column);
        }

        protected override string DecorateDbParameterName(string parameter)
        {
            return "?" + parameter;
        }

        public override string GenerateHasChildStatement(Dictionary<string, object> propertyValues, XElement entitySchema, XElement keySchema,
            out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            string select = GenerateFetchStatement(propertyValues, entitySchema, keySchema, out dbParameterValues);

            string sql = string.Format("SELECT 1 FROM DUAL WHERE EXISTS ({0})", select);
            return sql;
        }


    }
}
