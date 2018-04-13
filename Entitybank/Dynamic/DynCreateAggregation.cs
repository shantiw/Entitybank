using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Modification;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.Dynamic
{
    public class DynCreateAggregation : CreateAggregation<dynamic>
    {
        public DynCreateAggregation(object aggreg, string entity, XElement schema) : base(aggreg, entity, schema)
        {
        }

        internal protected DynCreateAggregation(object aggreg, string entity, XElement schema,
            DirectRelationship parentRelationship, Dictionary<string, object> parentPropertyValues, string path)
            : base(aggreg, entity, schema, parentRelationship, parentPropertyValues, path)
        {
        }

        protected override IExecuteAggregationHelper<dynamic> GetExecuteAggregationHelper()
        {
            return new ExecuteAggregationHelper();
        }
    }
}
