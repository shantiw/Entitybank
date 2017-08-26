using XData.Data.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    public class ConfigSchemaReviser : SchemaReviser, ISchemaReviser
    {
        protected XElement Config;

        public ConfigSchemaReviser(XElement config)
        {
            Config = config;
        }

        public ConfigSchemaReviser(string fileName) : this(LoadFromFile(fileName))
        {
        }

        protected static XElement LoadFromFile(string fileName)
        {
            return ElementHelper.LoadFromFile(fileName);
        }

        public override XElement Revise(XElement schema)
        {
            XElement revised = base.Revise(schema);
            revised.Modify(Config);
            return revised;
        }


    }
}
