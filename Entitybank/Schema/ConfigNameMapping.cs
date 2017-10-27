using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    //<configuration>
    //
    //  <mapping collection="Users" entity="User" table="T_Users">
    //    <mapping property="Id" column="UserId" />
    //  </mapping>
    //
    //</configuration>
    public class ConfigNameMapping //: INameMapping
    {
        protected XElement Config;

        public ConfigNameMapping(XElement config)
        {
            Config = config;
        }

        public string GetCollectionName(string tableName)
        {
            XElement xMapping = GetMapping(tableName);
            if (xMapping == null) return null;
            XAttribute attr = xMapping.Attribute(SchemaVocab.Collection);
            return attr?.Value.ToString();
        }

        public string GetEntityName(string tableName)
        {
            XElement xMapping = GetMapping(tableName);
            if (xMapping == null) return null;
            XAttribute attr = xMapping.Attribute(SchemaVocab.Entity);
            return attr?.Value.ToString();
        }

        public string GetPropertyName(string tableName, string columnName)
        {
            XElement xMapping = GetMapping(tableName);
            if (xMapping == null) return null;
            XElement xColMapping = xMapping.Elements(SchemaVocab.Mapping).FirstOrDefault(x => x.Attribute(SchemaVocab.Column).Value == columnName);
            if (xColMapping == null) return null;
            XAttribute attr = xColMapping.Attribute(SchemaVocab.Property);
            return attr?.Value.ToString();
        }

        protected XElement GetMapping(string tableName)
        {
            XElement xMapping = Config.Elements(SchemaVocab.Mapping).FirstOrDefault(x => x.Attribute(SchemaVocab.Table).Value == tableName);
            return xMapping;
        }


    }
}
