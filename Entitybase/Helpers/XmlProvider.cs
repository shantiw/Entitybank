using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Helpers
{
    public abstract class XmlProvider
    {
        protected readonly XElement[] Elements;
        protected readonly IEnumerable<string> ExcludedNames;

        protected readonly IEnumerable<string> KeyNames;
        protected readonly IEnumerable<XElement> Keys;

        protected XmlProvider(IEnumerable<XElement> elements, IEnumerable<string> excludedNames, string separator)
            : this(elements, excludedNames, GetKeySplitter(separator))
        {
        }

        private static Func<string, string[]> GetKeySplitter(string separator)
        {
            if (string.IsNullOrEmpty(separator))
            {
                return (key) => { return new string[] { key }; };
            }
            else
            {
                return (key) => { return key.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries); };
            }
        }

        protected XmlProvider(IEnumerable<XElement> elements, IEnumerable<string> excludedNames, Func<string, string[]> keySplitter)
        {
            Elements = elements.ToArray();
            ExcludedNames = excludedNames;

            List<string> keyNames = new List<string>();
            List<XElement> keys = new List<XElement>();

            for (int i = 0; i < Elements.Length; i++)
            {
                XElement element = Elements[i];
                IEnumerable<string> elementKeyNames = element.Attributes().Select(x => x.Name.LocalName).Except(excludedNames);
                if (elementKeyNames.Count() == 0) continue;

                keyNames.AddRange(elementKeyNames);
                keys.AddRange(GetElementKeys(element, i, elementKeyNames, keySplitter));
            }

            Keys = keys;
            KeyNames = keyNames.Distinct();
        }

        private List<XElement> GetElementKeys(XElement element, int index, IEnumerable<string> elementKeyNames, Func<string, string[]> keySplitter)
        {
            List<XElement> elementKeys = new List<XElement>();
            XElement indexElement = new XElement(element.Name);
            indexElement.SetAttributeValue("index", index);
            elementKeys.Add(indexElement);

            foreach (string key in elementKeyNames)
            {
                List<XElement> list = new List<XElement>();
                string[] values = keySplitter(element.Attribute(key).Value);
                foreach (string value in values)
                {
                    foreach (XElement elementKey in elementKeys)
                    {
                        XElement elem = new XElement(elementKey);
                        elem.SetAttributeValue(key, value);
                        list.Add(elem);
                    }
                }
                elementKeys = list;
            }

            return elementKeys;
        }

        // <delta key1="key1">
        // <delta key1="key1" key2="key2">
        public IEnumerable<XElement> FindElements(IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            IEnumerable<KeyValuePair<string, string>> key_values = keyValues.Where(p => KeyNames.Contains(p.Key));
            int keyValueCount = key_values.Count();
            if (keyValueCount == 0) return null;

            IEnumerable<XElement> keys = Keys.Where(x => x.Attributes().Count() == keyValueCount + 1);
            foreach (KeyValuePair<string, string> keyValue in key_values)
            {
                keys = keys.Where(x => x.Attribute(keyValue.Key) != null && x.Attribute(keyValue.Key).Value == keyValue.Value);
            }

            List<XElement> result = new List<XElement>();
            foreach (XElement key in keys)
            {
                int index = int.Parse(key.Attribute("index").Value);
                result.Add(Elements[index]);
            }
            return result;
        }

    }

    public class FileXmlProvider : XmlProvider
    {
        public string File { get; private set; }

        public FileXmlProvider(string file, IEnumerable<string> excludedNames, string separator)
            : base(LoadFile(file), excludedNames, separator)
        {
            File = file;
        }

        private static IEnumerable<XElement> LoadFile(string path)
        {
            string exePath = Path.Combine(Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location), path);
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exePath);

            string file = System.IO.File.Exists(exePath) ? exePath : basePath;
            if (System.IO.File.Exists(file)) return XElement.Load(file).Elements();

            throw new FileNotFoundException(path);
        }

    }

    public class DirectoryXmlProvider : XmlProvider
    {
        public string Directory { get; private set; }
        public string Extension { get; private set; }

        public DirectoryXmlProvider(string dir, string fileExtension, IEnumerable<string> excludedNames, string separator)
            : base(LoadDirectory(dir, fileExtension), excludedNames, separator)
        {
            Directory = dir;
            Extension = fileExtension;
        }

        private static IEnumerable<XElement> LoadDirectory(string path, string extension)
        {
            string exePath = Path.Combine(Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location), path);
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exePath);

            List<XElement> elements = new List<XElement>();

            string dir = exePath;
            if (!System.IO.Directory.Exists(dir))
            {
                dir = basePath;
            }
            if (System.IO.Directory.Exists(dir))
            {
                foreach (string fileName in System.IO.Directory.GetFiles(dir))
                {
                    if (Path.GetExtension(fileName) == extension)
                    {
                        XElement xFile = XElement.Load(fileName);
                        elements.AddRange(xFile.Elements());
                    }
                }
            }

            if (elements.Count == 0) throw new FileNotFoundException(path + " *" + extension);

            return elements;
        }

    }

}
