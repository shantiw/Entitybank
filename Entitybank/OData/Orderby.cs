using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Objects;

namespace XData.Data.OData
{
    public class Orderby
    {
        public Order[] Orders { get; private set; }

        protected readonly string Value;
        protected readonly string Entity;
        protected readonly XElement Schema;

        public Orderby(string value, string entity, XElement schema)
        {
            Value = value;
            Entity = entity;
            Schema = schema;

            //
            List<Order> list = new List<Order>();
            string[] orderStrs = value.Split(',');
            for (int i = 0; i < orderStrs.Length; i++)
            {
                string orderStr = orderStrs[i].Trim();

                string[] ss = orderStr.Split(new char[] { (char)32 }, StringSplitOptions.RemoveEmptyEntries);
                if (ss.Length > 2) throw new SyntaxErrorException(string.Format(ODataMessages.IncorrectSyntax, orderStr));

                if (ss.Length == 1)
                {
                    list.Add(new AscendingOrder(ss[0]));
                }
                else if (ss[1] == "asc")
                {
                    list.Add(new AscendingOrder(ss[0]));
                }
                else if (ss[1] == "desc")
                {
                    list.Add(new DescendingOrder(ss[0]));
                }
                else
                {
                    throw new SyntaxErrorException(string.Format(ODataMessages.IncorrectSyntax, orderStr));
                }
            }

            Orders = list.ToArray();
        }


    }
}
