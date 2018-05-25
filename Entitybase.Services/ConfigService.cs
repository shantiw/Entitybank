using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Objects;

namespace XData.Data.Services
{
    // name: ConnectionStringName
    public sealed class ConfigService
    {
        private ConfigUpdater ConfigUpdater = new ConfigUpdater();

        public void UpdateDatabase(string name)
        {
            ConfigUpdater.UpdateDatabase(name);
        }

        public void UpdateDateFormatters()
        {
            ConfigUpdater.UpdateDateFormatters();
        }

        public void UpdateDataConverters()
        {
            ConfigUpdater.UpdateDataConverters();
        }
    }
}
