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
    public class DynUpdateAggregation : UpdateAggregation<dynamic>
    {
        public DynUpdateAggregation(object aggreg, string entity, XElement schema) : base(aggreg, entity, schema)
        {
        }

        internal protected DynUpdateAggregation(object aggreg, object original, string entity, XElement schema)
            : base(aggreg, original, entity, schema,
                  (p0, p1, p2) => new DynCreateAggregation(p0, p1, p2),
                  (p0, p1, p2) => new DynDeleteAggregation(p0, p1, p2))
        {
        }

        protected override IExecuteAggregationHelper<dynamic> GetExecuteAggregationHelper()
        {
            return new ExecuteAggregationHelper();
        }
    }
}
