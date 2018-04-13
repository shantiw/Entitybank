﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Modification;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.Xml
{
    public class XmlCreateAggregation : CreateAggregation<XElement>
    {
        public XmlCreateAggregation(XElement aggreg, string entity, XElement schema) : base(aggreg, entity, schema)
        {
        }

        internal protected XmlCreateAggregation(XElement aggreg, string entity, XElement schema,
            DirectRelationship parentRelationship, Dictionary<string, object> parentPropertyValues, string path)
            : base(aggreg, entity, schema, parentRelationship, parentPropertyValues, path)
        {
        }

        protected override IExecuteAggregationHelper<XElement> GetExecuteAggregationHelper()
        {
            return new ExecuteAggregationHelper();
        }
    }
}
