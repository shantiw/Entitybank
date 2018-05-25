using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Helpers
{
    internal static class ElementHelper
    {
        public static void CopyAttributes(XElement source, XElement destination, params string[] exclusion)
        {
            foreach (XAttribute attr in source.Attributes())
            {
                if (exclusion.Contains(attr.Name.ToString())) continue;
                destination.SetAttributeValue(attr.Name, attr.Value);
            }
        }

        public static XElement LoadFromFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            }
            return XElement.Load(fileName);
        }


    }
}
