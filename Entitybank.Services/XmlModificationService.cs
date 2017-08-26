using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Xml;

namespace XData.Data.Services
{
    public sealed class XmlModificationService : ModificationService<XElement>
    {
        public XmlModificationService(string name, IEnumerable<KeyValuePair<string, string>> keyValues) : base(name, keyValues)
        {
            Modifier = XmlModifier.Create(name, Schema);
        }

        public void Create(XElement element, out XElement result)
        {
            (Modifier as XmlModifier).Create(element, out IEnumerable<Dictionary<string, object>> keys);
            XElement entitySchema;
            if (IsCollection(element))
            {
                entitySchema = GetEntitySchemaByCollection(Schema, element.Name.LocalName);
            }
            else
            {
                entitySchema = GetEntitySchema(Schema, element.Name.LocalName);
            }
            result = KeysToXml(keys, entitySchema);
        }

        // json
        public void Create(XElement element, out string result)
        {
            (Modifier as XmlModifier).Create(element, out IEnumerable<Dictionary<string, object>> keys);
            result = KeysToJson(keys);
        }

        public void Delete(XElement element)
        {
            (Modifier as XmlModifier).Delete(element);
        }

        public void Update(XElement element)
        {
            (Modifier as XmlModifier).Update(element);
        }

        private static bool IsCollection(XElement element)
        {
            return element.Elements().All(x => x.HasElements);
        }


    }
}
