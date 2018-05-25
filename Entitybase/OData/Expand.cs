using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.OData
{
    public class Expand
    {
        public string Property { get; private set; }
        public string Select { get; private set; }
        public string Filter { get; private set; }
        public string Orderby { get; private set; }

        public Expand[] Children { get; set; } = new Expand[0];

        public Expand(string property, string select, string filter, string orderby)
        {
            Property = property;
            Select = select;
            Filter = filter;
            Orderby = orderby;
        }

    }
}
