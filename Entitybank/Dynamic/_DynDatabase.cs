using System.Collections.Generic;
using System.Xml.Linq;
using XData.Data.Objects;

namespace XData.Data.Dynamic
{
    internal class _DynDatabase : Database<dynamic>
    {
        public _DynDatabase(Database database) : base(database)
        {

        }

        protected override Dictionary<string, object> ToDictionary(dynamic obj, XElement entitySchema)
        {
            XData.Data.Dynamic.ExecuteAggregationHelper helper = new XData.Data.Dynamic.ExecuteAggregationHelper();
            return helper.GetPropertyValues(obj, entitySchema);
        }
    }
}
