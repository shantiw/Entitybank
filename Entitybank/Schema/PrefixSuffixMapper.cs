using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Schema
{
    // CollectionName: Prefix + TableName + Suffix
    public class PrefixSuffixMapper : Mapper, IMapper
    {
        protected string Prefix;
        protected string Suffix;
        protected PrefixSuffixNameMapping PrefixSuffixNameMapping;
         
        public PrefixSuffixMapper()
        {
            PrefixSuffixNameMapping = new PrefixSuffixNameMapping();
        }

        public PrefixSuffixMapper(string prefix, string suffix)
        {
            Prefix = prefix;
            Suffix = suffix;

            PrefixSuffixNameMapping = new PrefixSuffixNameMapping(Prefix, Suffix);
        }

        protected override string GetCollectionName(string tableName)
        {
            return PrefixSuffixNameMapping.GetCollectionName(tableName);
        }

        protected override string GetEntityName(string tableName)
        {
            return PrefixSuffixNameMapping.GetEntityName(tableName);
        }


    }
}
