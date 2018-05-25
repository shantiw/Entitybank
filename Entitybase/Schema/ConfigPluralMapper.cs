using XData.Data.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    public class ConfigPluralMapper : Mapper, IMapper
    {
        protected XElement Config;
        protected ConfigNameMapping ConfigNameMapping;
        protected PluralNameMapping PluralNameMapping = new PluralNameMapping(); 
      
        public ConfigPluralMapper(XElement config) : base()
        {
            Config = config;
            ConfigNameMapping = new ConfigNameMapping(Config);
        }

        public ConfigPluralMapper(string fileName) : this(LoadFromFile(fileName))
        {
        }

        protected override string GetCollectionName(string tableName)
        {
            string name = ConfigNameMapping.GetCollectionName(tableName);
            return (string.IsNullOrWhiteSpace(name)) ? PluralNameMapping.GetCollectionName(tableName) : name;
        }

        protected override string GetEntityName(string tableName)
        {
            string name = ConfigNameMapping.GetEntityName(tableName);
            return (string.IsNullOrWhiteSpace(name)) ? PluralNameMapping.GetEntityName(tableName) : name;
        }

        protected override string GetPropertyName(string tableName, string columnName)
        {
            string name = ConfigNameMapping.GetPropertyName(tableName, columnName);
            return (string.IsNullOrWhiteSpace(name)) ? PluralNameMapping.GetPropertyName(tableName, columnName) : name;
        }

        protected static XElement LoadFromFile(string fileName)
        {
            return ElementHelper.LoadFromFile(fileName);
        }


    }
}
