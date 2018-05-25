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

        protected override IExecuteAggregationHelper<XElement> GetExecuteAggregationHelper()
        {
            return new ExecuteAggregationHelper();
        }
    }
}
