using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    internal class DbSchemaReviser : IDbSchemaReviser
    {
        public XElement Revise(XElement dbSchema)
        {
            return new XElement(dbSchema);
        }
    }
}
