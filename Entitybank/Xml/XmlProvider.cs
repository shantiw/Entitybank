using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Xml
{
    public abstract class XmlProvider
    {
        protected readonly IEnumerable<XElement> Elements;
        protected readonly IEnumerable<string> ExcludedKeyNames;
        protected readonly IEnumerable<string> KeyNames;

        protected XmlProvider(IEnumerable<XElement> elements, IEnumerable<string> excludedKeyNames)
        {
            Elements = elements;
            ExcludedKeyNames = excludedKeyNames;
            KeyNames = Elements.Attributes().Select(x => x.Name.ToString()).Distinct().Except(ExcludedKeyNames);
        }

        // <delta key1="key1">
        // <delta key1="key1" key2="key2">
        public IEnumerable<XElement> FindElements(IEnumerable<KeyValuePair<string, string>> keyNameValues, out bool hasMatchedKey)
        {
            IEnumerable<KeyValuePair<string, string>> key = keyNameValues.Where(p => KeyNames.Contains(p.Key));
            int keyCount = key.Count();
            hasMatchedKey = keyCount > 0;
            if (keyCount == 0) return new XElement[0];

            IEnumerable<XElement> elements = Elements;
            foreach (KeyValuePair<string, string> item in key)
            {
                elements = elements.Where(x => x.Attribute(item.Key) != null && x.Attribute(item.Key).Value == item.Value);
            }

            elements = elements.Where(x => x.Attributes().Count() == keyCount);

            return elements;
        }

    }

    public class FileXmlProvider : XmlProvider
    {
        public string File { get; private set; }

        public FileXmlProvider(string file) : this(file, new string[0])
        {
        }

        public FileXmlProvider(string file, IEnumerable<string> excludedKeyNames)
            : base(LoadFile(file), excludedKeyNames)
        {
            File = file;
        }

        private static IEnumerable<XElement> LoadFile(string path)
        {
            string exePath = System.IO.Path.Combine(System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location), path);
            string basePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exePath);

            string file = System.IO.File.Exists(exePath) ? exePath : basePath;
            if (System.IO.File.Exists(file)) return XElement.Load(file).Elements();

            throw new FileNotFoundException(path);
        }

    }

    public class DirectoryXmlProvider : XmlProvider
    {
        public string Directory { get; private set; }
        public string Extension { get; private set; }

        public DirectoryXmlProvider(string dir, string fileExtension) : this(dir, fileExtension, new string[0])
        {
        }

        public DirectoryXmlProvider(string dir, string fileExtension, IEnumerable<string> excludedKeyNames)
            : base(LoadDirectory(dir, fileExtension), excludedKeyNames)
        {
            Directory = dir;
            Extension = fileExtension;
        }

        private static IEnumerable<XElement> LoadDirectory(string path, string extension)
        {
            string exePath = System.IO.Path.Combine(System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location), path);
            string basePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exePath);

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
                    if (System.IO.Path.GetExtension(fileName) == extension)
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
