using System;
using System.Collections.Generic;
namespace System.Data.Entity.ModelConfiguration.Design.PluralizationServices
{
	/// <summary>
	///     This class provide service for both the singularization and pluralization, it takes the word pairs
	///     in the ctor following the rules that the first one is singular and the second one is plural.
	/// </summary>
	internal class BidirectionalDictionary<TFirst, TSecond>
	{
		internal Dictionary<TFirst, TSecond> FirstToSecondDictionary
		{
			get;
			set;
		}
		internal Dictionary<TSecond, TFirst> SecondToFirstDictionary
		{
			get;
			set;
		}
		internal BidirectionalDictionary()
		{
			this.FirstToSecondDictionary = new Dictionary<TFirst, TSecond>();
			this.SecondToFirstDictionary = new Dictionary<TSecond, TFirst>();
		}
		internal BidirectionalDictionary(Dictionary<TFirst, TSecond> firstToSecondDictionary) : this()
		{
			foreach (TFirst current in firstToSecondDictionary.Keys)
			{
				this.AddValue(current, firstToSecondDictionary[current]);
			}
		}
		internal virtual bool ExistsInFirst(TFirst value)
		{
			return this.FirstToSecondDictionary.ContainsKey(value);
		}
		internal virtual bool ExistsInSecond(TSecond value)
		{
			return this.SecondToFirstDictionary.ContainsKey(value);
		}
		internal virtual TSecond GetSecondValue(TFirst value)
		{
			if (this.ExistsInFirst(value))
			{
				return this.FirstToSecondDictionary[value];
			}
			return default(TSecond);
		}
		internal virtual TFirst GetFirstValue(TSecond value)
		{
			if (this.ExistsInSecond(value))
			{
				return this.SecondToFirstDictionary[value];
			}
			return default(TFirst);
		}
		internal void AddValue(TFirst firstValue, TSecond secondValue)
		{
			this.FirstToSecondDictionary.Add(firstValue, secondValue);
			if (!this.SecondToFirstDictionary.ContainsKey(secondValue))
			{
				this.SecondToFirstDictionary.Add(secondValue, firstValue);
			}
		}
	}
}
