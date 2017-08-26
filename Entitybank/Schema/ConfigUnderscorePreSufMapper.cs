using XData.Data.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    public class ConfigUnderscorePreSufMapper : Mapper, IMapper
    {
        protected string Prefix { get; private set; }
        protected string Suffix { get; private set; }

        protected XElement Config;
        protected ConfigNameMapping ConfigNameMapping;
        protected PrefixSuffixNameMapping PrefixSuffixNameMapping;

        public ConfigUnderscorePreSufMapper(XElement config)
        {
            Config = config;
            ConfigNameMapping = new ConfigNameMapping(Config);
            PrefixSuffixNameMapping = new PrefixSuffixNameMapping();
            Prefix = PrefixSuffixNameMapping.Prefix;
            Suffix = PrefixSuffixNameMapping.Suffix;
        }

        public ConfigUnderscorePreSufMapper(string fileName) : this(LoadFromFile(fileName))
        {
        }

        public ConfigUnderscorePreSufMapper(string prefix, string suffix, XElement config)
        {
            Prefix = prefix;
            Suffix = suffix;
            Config = config;
            ConfigNameMapping = new ConfigNameMapping(Config);
            PrefixSuffixNameMapping = new PrefixSuffixNameMapping(Prefix, Suffix);
        }

        public ConfigUnderscorePreSufMapper(string prefix, string suffix, string fileName) : this(prefix, suffix, LoadFromFile(fileName))
        {
        }

        protected override string GetCollectionName(string tableName)
        {
            string name = ConfigNameMapping.GetCollectionName(tableName);
            return (string.IsNullOrWhiteSpace(name)) ? PrefixSuffixNameMapping.GetCollectionName(GetEntityName(tableName)) : name;
        }

        protected override string GetEntityName(string tableName)
        {
            string name = ConfigNameMapping.GetEntityName(tableName);
            return (string.IsNullOrWhiteSpace(name)) ? tableName.UnderscoreToUpperCamel() : name;
        }

        protected override string GetPropertyName(string tableName, string columnName)
        {
            string name = ConfigNameMapping.GetPropertyName(tableName, columnName);
            return (string.IsNullOrWhiteSpace(name)) ? columnName.UnderscoreToUpperCamel() : name;
        }

        protected static XElement LoadFromFile(string fileName)
        {
            return ElementHelper.LoadFromFile(fileName);
        }


    }
}
