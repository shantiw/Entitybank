using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Design.PluralizationServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Schema
{
    public class PluralNameMapping //: INameMapping
    {
        private readonly EnglishPluralizationService _service = new EnglishPluralizationService();

        protected void AddWord(string singular, string plural)
        {
            _service.AddWord(singular, plural);
        }

        protected bool IsPlural(string word)
        {
            return _service.IsPlural(word);
        }

        protected bool IsSingular(string word)
        {
            return _service.IsSingular(word);
        }

        protected string Pluralize(string word)
        {
            return _service.Pluralize(word);
        }

        protected string Singularize(string word)
        {
            return _service.Singularize(word);
        }

        public virtual string GetCollectionName(string tableName)
        {
            return tableName;
        }

        public virtual string GetEntityName(string tableName)
        {
            string entityName = Singularize(tableName);

            // Oracle
            if (tableName.ToUpper() == tableName) entityName = entityName.ToUpper();

            return entityName;
        }

        public virtual string GetPropertyName(string tableName, string columnName)
        {
            return columnName;
        }


    }
}
