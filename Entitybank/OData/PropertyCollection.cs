using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XData.Data.Schema;

namespace XData.Data.OData
{
    public class PropertyCollection : IEnumerable<Property>
    {
        private List<Property> _properties;
        private Query _query;

        internal PropertyCollection(IEnumerable<Property> properties, Query query)
        {
            _properties = new List<Property>(properties);
            _query = query;
        }

        // for $expand
        public void UnionFieldProperties(IEnumerable<string> fieldProperties)
        {
            foreach (string property in fieldProperties)
            {
                if (_query.Properties.Any(p => p.Name == property)) continue;

                Property oProperty = FieldProperty.Create(property, _query.Entity, _query.Schema);
                _properties.Add(oProperty);
            }
        }

        public IEnumerator<Property> GetEnumerator()
        {
            return _properties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
