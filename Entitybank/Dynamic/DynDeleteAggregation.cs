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
    public class DynDeleteAggregation : DeleteAggregation<dynamic>
    {
        public DynDeleteAggregation(object aggreg, string entity, XElement schema) : base(aggreg, entity, schema)
        {
        }

        internal protected DynDeleteAggregation(object aggreg, string entity, XElement schema, string path) : base(aggreg, entity, schema, path)
        {
        }

        protected override IExecuteAggregationHelper<dynamic> GetExecuteAggregationHelper()
        {
            return new ExecuteAggregationHelper();
        }
    }
}
