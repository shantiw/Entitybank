using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Objects
{
    public class ConfigUpdater
    {
        public void UpdateDatabase(string name)
        {
            new DatabaseManufacturer().Update(name);
        }

        public void UpdateDateFormatters()
        {
            new DateFormatterManufacturer().Update();
        }

        public void UpdateDataConverters()
        {
            new DataConverterManufacturer().Update();
        }
    }
}
