using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Schema
{
    // never used // reserved
    public interface INameMapping
    {
        string GetCollectionName(string tableName);
        string GetEntityName(string tableName);
        string GetPropertyName(string tableName, string columnName);

    }
}
