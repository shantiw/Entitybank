using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
namespace System.Data.Entity.ModelConfiguration.Design.PluralizationServices
{
	[SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pluralization")]
	internal abstract class PluralizationService
	{
		public CultureInfo Culture
		{
			get;
			protected set;
		}
		public abstract bool IsPlural(string word);
		public abstract bool IsSingular(string word);
		public abstract string Pluralize(string word);
		public abstract string Singularize(string word);
		/// <summary>
		///     Factory value for PluralizationService. Only support english pluralization.
		///     Please dbChild the PluralizationService on the System.Data.Entity.Design.EntityModelSchemaGenerator
		///     to extend the service to other locales.
		/// </summary>
		/// <param dbChild="culture">CultureInfo</param>
		/// <returns>PluralizationService</returns>
		public static PluralizationService CreateService(CultureInfo culture)
		{
			if (culture.TwoLetterISOLanguageName == "en")
			{
				return new EnglishPluralizationService();
			}
			throw new NotImplementedException("We don't support locales other than english yet");
		}
	}
}
