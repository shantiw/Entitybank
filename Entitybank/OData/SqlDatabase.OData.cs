using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.OData;

namespace XData.Data.Objects
{
    public partial class SqlDatabase : Database
    {
        protected override QueryGenerator CreateQueryGenerator()
        {
            return new SqlQueryGenerator();
        }

        protected override QueryExpandResultGetter CreateQueryExpandResultGetter()
        {
            return new SqlTempTableResultGetter(this);
        }


    }
}
