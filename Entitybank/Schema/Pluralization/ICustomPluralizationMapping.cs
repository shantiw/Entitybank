using System;
using System.Diagnostics.CodeAnalysis;
namespace System.Data.Entity.ModelConfiguration.Design.PluralizationServices
{
	[SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pluralization")]
	internal interface ICustomPluralizationMapping
	{
		void AddWord(string singular, string plural);
	}
}
