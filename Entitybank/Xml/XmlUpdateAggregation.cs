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
    public class XmlUpdateAggregation : UpdateAggregation<XElement>
    {
        public XmlUpdateAggregation(XElement aggreg, string entity, XElement schema) : base(aggreg, entity, schema)
        {
        }

        public XmlUpdateAggregation(XElement aggreg, XElement original, string entity, XElement schema)
            : base(aggreg, original, entity, schema,
                  (p0, p1, p2, p3, p4, p5) => new XmlCreateAggregation(p0, p1, p2, p3, p4, p5),
                  (p0, p1, p2, p3) => new XmlDeleteAggregation(p0, p1, p2, p3))
        {
        }

        protected override IExecuteAggregationHelper<XElement> GetExecuteAggregationHelper()
        {
            return new ExecuteAggregationHelper();
        }
    }
}
