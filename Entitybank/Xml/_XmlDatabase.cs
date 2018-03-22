using System.Collections.Generic;
using System.Xml.Linq;
using XData.Data.Objects;

namespace XData.Data.Xml
{
    internal class _XmlDatabase : Database<XElement>
    {
        public _XmlDatabase(Database database) : base(database)
        {

        }

        protected override Dictionary<string, object> ToDictionary(XElement obj, XElement entitySchema)
        {
            XData.Data.Xml.ExecuteAggregationHelper helper = new XData.Data.Xml.ExecuteAggregationHelper();
            return helper.GetPropertyValues(obj as XElement, entitySchema);
        }
    }
}
