using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Dynamic;

namespace XData.Data.Services
{
    public sealed class DynModificationService : ModificationService<dynamic>
    {
        public DynModificationService(string name, IEnumerable<KeyValuePair<string, string>> keyValues) : base(name, keyValues)
        {
            Modifier = DynModifierFactory.Create(name);
        }


    }
}
