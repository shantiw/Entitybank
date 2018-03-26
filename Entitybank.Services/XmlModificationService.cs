using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Xml;
using XData.Data.Modification;

namespace XData.Data.Services
{
    public sealed class XmlModificationService : ModificationService<XElement>
    {
        public XmlModificationService(string name, IEnumerable<KeyValuePair<string, string>> keyValues) : base(name, keyValues)
        {
            Modifier = XmlModifierFactory.Create(name);
        }

        public void Create(XElement element, out XElement keys)
        {
            keys = Modifier.CreateAndReturnKeys(element, Schema);
        }

        // json
        public void Create(XElement element, out string keys)
        {
            Modifier.Create(element, Schema, out IEnumerable<Dictionary<string, object>> result);
            keys = result.CreateReturnKeysToJson();
        }

        public void Delete(XElement element)
        {
            Modifier.Delete(element, Schema);
        }

        public void Update(XElement element)
        {
            Modifier.Update(element, Schema);
        }

        private static bool IsCollection(XElement element)
        {
            return element.Elements().All(x => x.HasElements);
        }


    }
}
