using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Modification;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.Services
{
    public abstract class ModificationService<T>
    {
        protected readonly XElement Schema;
        protected Modifier<T> Modifier;

        public ModificationService(string name, IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            Schema = GetSchema(name, keyValues);
        }

        public void Create(T obj, string entity, out XElement keys)
        {
            Modifier.Create(obj, entity, Schema, out keys);
        }

        // json
        public void Create(T obj, string entity, out string keys)
        {
            Modifier.Create(obj, entity, Schema, out keys);
        }

        public void Delete(T obj, string entity)
        {
            Modifier.Delete(obj, entity, Schema);
        }

        public void Update(T obj, string entity)
        {
            Modifier.Update(obj, entity, Schema);
        }

        protected static XElement GetSchema(string name, IEnumerable<KeyValuePair<string, string>> deltaKey)
        {
            SchemaProvider schemaProvider = new SchemaProvider(name);
            return schemaProvider.GetSchema(deltaKey);
        }

        protected static XElement GetEntitySchema(XElement schema, string entity)
        {
            return schema.Elements(SchemaVocab.Entity).First(x => x.Attribute(SchemaVocab.Name).Value == entity);
        }

        protected static XElement GetEntitySchemaByCollection(XElement schema, string collection)
        {
            return schema.Elements(SchemaVocab.Entity).FirstOrDefault(x => x.Attribute(SchemaVocab.Collection).Value == collection);
        }

        protected static bool IsNumeric(Type type)
        {
            return (type == typeof(SByte) || type == typeof(Int16) || type == typeof(Int32) || type == typeof(Int64) ||
                type == typeof(Byte) || type == typeof(UInt16) || type == typeof(UInt32) || type == typeof(UInt64) ||
                type == typeof(Decimal) || type == typeof(Single) || type == typeof(Double));
        }


    }
}
