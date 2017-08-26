using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.OData;

namespace XData.Data.Objects
{
    public partial class MySqlDatabase : Database
    {
        protected override QueryGenerator CreateQueryGenerator()
        {
            return new MySqlQueryGenerator();
        }

        protected override QueryExpandResultGetter CreateQueryExpandResultGetter()
        {
            return new MySqlTempTableResultGetter(this);
        }


    }
}
