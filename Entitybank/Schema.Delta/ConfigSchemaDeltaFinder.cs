using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Helpers;
using XData.Data.Schema.Delta;

namespace XData.Data.Schema
{
    public class ConfigSchemaDeltaFinder : SchemaDeltaFinder
    {
        protected readonly IEnumerable<string> ExcludedAttributes;
        protected readonly string Separator;

        protected readonly XmlProvider XmlProvider;

        public ConfigSchemaDeltaFinder(string name, string excludedAttributes, string separator) : base(name)
        {
            Separator = separator;

            string[] excluded = new string[0];
            if (!string.IsNullOrWhiteSpace(excludedAttributes))
            {
                excluded = excludedAttributes.Split(',');
                for (int i = 0; i < excluded.Length; i++)
                {
                    excluded[i] = excluded[i].Trim();
                }
            }
            ExcludedAttributes = excluded;

            XmlProvider = new DirectoryXmlProvider(Path.Combine(Name, "deltas"), ".config", ExcludedAttributes, Separator);
        }

        public override XElement Find(IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            IEnumerable<XElement> elements = XmlProvider.FindElements(keyValues);
            if (elements == null) return null;

            int count = elements.Count();
            //if (count == 0) throw new SchemaException(string.Format(SchemaDeltaMessages.NotFoundDelta, GetKeyValueString(deltaKey)));
            if (count == 0) return null;
            if (count > 1) throw new SchemaException(string.Format(SchemaDeltaMessages.AmbiguousDelta, GetKeyValueString(keyValues)));

            return new XElement(elements.First());
        }

        private string GetKeyValueString(IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            return string.Join(",", keyValues.Select(x => string.Format("{0}:\"{1}\"", x.Key, x.Value)));
        }


    }
}
