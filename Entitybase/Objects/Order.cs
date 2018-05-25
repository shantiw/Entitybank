using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Objects
{
    public abstract class Order
    {
        public string Property { get; private set; }

        public Order(string property)
        {
            Property = property;
        }
    }

    public class AscendingOrder : Order
    {
        public AscendingOrder(string property) : base(property)
        {
        }
    }

    public class DescendingOrder : Order
    {
        public DescendingOrder(string property) : base(property)
        {
        }
    }
}
