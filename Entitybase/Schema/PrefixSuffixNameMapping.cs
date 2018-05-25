using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Schema
{
    public class PrefixSuffixNameMapping //: INameMapping
    {
        public string Prefix { get; private set; }
        public string Suffix { get; private set; }

        public PrefixSuffixNameMapping() : this("ArrayOf", string.Empty)
        {
        }

        public PrefixSuffixNameMapping(string prefix, string suffix)
        {
            Prefix = prefix;
            Suffix = suffix;
        }

        public virtual string GetCollectionName(string tableName)
        {
            return Prefix + tableName + Suffix;
        }

        public virtual string GetEntityName(string tableName)
        {
            return tableName;
        }

        public virtual string GetPropertyName(string tableName, string columnName)
        {
            return columnName;
        }


    }
}
