using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.OData;

namespace XData.Data.Objects
{
    public partial class OracleDatabase : Database
    {
        protected override QueryGenerator CreateQueryGenerator()
        {
            return new OracleQueryGenerator();
        }

        protected override QueryExpandResultGetter CreateQueryExpandResultGetter()
        {
            return new OracleTempTableResultGetter(this);
        }


    }
}
