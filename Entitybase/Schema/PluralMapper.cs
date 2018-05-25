using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Schema
{
    // TableName is a plural
    public class PluralMapper : Mapper, IMapper
    {
        protected PluralNameMapping PluralNameMapping = new PluralNameMapping();

        protected override string GetCollectionName(string tableName)
        {
            return PluralNameMapping.GetCollectionName(tableName);
        }

        protected override string GetEntityName(string tableName)
        {
            return PluralNameMapping.GetEntityName(tableName);
        }


    }
}
