using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Modification
{
    public abstract partial class UpdateAggregation<T> : ExecuteAggregation<T>
    {
        public T Original { get; private set; }

        public UpdateAggregation(T aggreg, T original, string entity, XElement schema) : base(aggreg, entity, schema)
        {
            Original = original;

            throw new NotImplementedException();
        }


    }
}
