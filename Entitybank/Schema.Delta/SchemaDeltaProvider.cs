using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema.Delta;
using XData.Data.Xml;

namespace XData.Data.Schema
{
    // id: ConnectionStringName
    public class SchemaDeltaProvider
    {
        private static Dictionary<string, XmlProvider> Cache = new Dictionary<string, XmlProvider>();
        private static object LockObj = new object();

        public void Update(string name)
        {
            XmlProvider provider = new DirectoryXmlProvider(Path.Combine(name, "delta"), ".config");
            lock (LockObj)
            {
                Cache[name] = provider;
            }
        }

        // <delta key1="key1"> // /dev/schema/{id}?key1=key1&key2=
        // <delta key1="key1" key2="key2"> // /dev/schema/{id}?key1=key1&key2=key2
        public XElement GetDelta(string name, IEnumerable<KeyValuePair<string, string>> deltaKey)
        {
            if (!Cache.ContainsKey(name))
            {
                Update(name);
            }

            XmlProvider provider = Cache[name];
            IEnumerable<XElement> elements = provider.FindElements(deltaKey,out bool hasMatchedKey);
            if (!hasMatchedKey) return null;

            int count = elements.Count();
            if (count == 1) return new XElement(elements.First());

            string sKey = string.Join(",", deltaKey.Select(x => string.Format("{0}:\"{1}\"", x.Key, x.Value)));
            if (count == 0) throw new SchemaException(string.Format(SchemaDeltaMessages.NotFoundDelta, sKey));
            throw new SchemaException(string.Format(SchemaDeltaMessages.AmbiguousDelta, sKey));
        }


    }
}
