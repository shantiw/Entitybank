using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    public interface IDbSchemaProvider
    {
        XElement GetDbSchema();
    }

    public interface IDbSchemaReviser
    {
        XElement Revise(XElement dbSchema);
    }

    public interface IMapper
    {
        XElement Map(XElement dbSchema);
    }

    public interface ISchemaReviser
    {
        XElement Revise(XElement schema);
    }


}
