using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Modification
{
    public class SqlModificationGenerator : ModificationGenerator
    {
        protected override int DbParamLimit => 2100;

        protected override string DecorateTableName(string table)
        {
            return string.Format("[{0}]", table);
        }

        protected override string DecorateColumnName(string column)
        {
            return string.Format("[{0}]", column);
        }

        protected override string DecorateDbParameterName(string parameter)
        {
            return "@" + parameter;
        }

        public override string GenerateIsExistsStatement(Dictionary<string, object> propertyValues, XElement entitySchema, XElement keySchema,
            out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            string select = GenerateFindStatement(propertyValues, entitySchema, keySchema, out dbParameterValues);

            string sql = string.Format("SELECT 1 WHERE EXISTS ({0})", select);
            return sql;
        }


    }
}
