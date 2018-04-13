using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Modification;
using XData.Data.Objects;

namespace XData.Data.Xml
{
    public class XmlDeleteAggregation : DeleteAggregation<XElement>
    {
        public XmlDeleteAggregation(XElement aggreg, string entity, XElement schema) : base(aggreg, entity, schema)
        {
        }

        public XmlDeleteAggregation(XElement aggreg, string entity, XElement schema, string path) : base(aggreg, entity, schema, path)
        {
        }

        protected override IExecuteAggregationHelper<XElement> GetExecuteAggregationHelper()
        {
            return new ExecuteAggregationHelper();
        }
    }
}
