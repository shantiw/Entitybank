using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Helpers;

namespace XData.Data.Schema
{
    public class UnderscorePreSufMapper : Mapper, IMapper
    {
        protected string Prefix;
        protected string Suffix;
        protected PrefixSuffixNameMapping PrefixSuffixNameMapping;

        public UnderscorePreSufMapper()
        {
            PrefixSuffixNameMapping = new PrefixSuffixNameMapping();
        }

        public UnderscorePreSufMapper(string prefix, string suffix)
        {
            Prefix = prefix;
            Suffix = suffix;

            PrefixSuffixNameMapping = new PrefixSuffixNameMapping(Prefix, Suffix);
        }

        protected override string GetCollectionName(string tableName)
        {
            return PrefixSuffixNameMapping.GetCollectionName(GetEntityName(tableName));
        }

        protected override string GetEntityName(string tableName)
        {
            return tableName.UnderscoreToUpperCamel();
        }

        protected override string GetPropertyName(string tableName, string columnName)
        {
            return columnName.UnderscoreToUpperCamel();
        }


    }
}
