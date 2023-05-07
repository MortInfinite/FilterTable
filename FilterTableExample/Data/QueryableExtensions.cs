using System.Linq.Expressions;

namespace FilterTableExample.Data
{
	/// <summary>
	/// Extension methods for the IQueryable data type.
	/// </summary>
	public static class QueryableExtensions
	{
		/// <summary>
		/// Sorts the specified source as either ascending or descending.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of source.</typeparam>
		/// <typeparam name="TKey">The type of the key returned by the function that is represented by keySelector.</typeparam>
		/// <param name="source">A sequence of values to order.</param>
		/// <param name="keySelector">A function to extract a key from an element.</param>
		/// <param name="ascending">Whether to sort by ascending (true) or descending (false).</param>
		/// <returns>An System.Linq.IOrderedQueryable`1 whose elements are sorted according to a key.</returns>
		/// <exception cref="System.ArgumentNullException">source or keySelector is null.</exception>
		public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, bool ascending)
		{
			if(ascending)
				return source.OrderBy(keySelector);
			else
				return source.OrderByDescending(keySelector);
		}

		/// <summary>
		/// Sorts the specified <paramref name="myDataTypes"/>.
		/// </summary>
		/// <param name="myDataTypes">MyDataType items to sort.</param>
		/// <param name="sortLabel">Name of the field to sort by.</param>
		/// <param name="sortAscending">Indicates if results should be sorted in ascending order (true) or descending order (false).</param>
		public static IOrderedQueryable<MyDataType> SortData(this IQueryable<MyDataType> myDataTypes, string sortLabel, bool sortAscending=true)
		{ 
			// Sort data.
			switch(sortLabel)
			{
				default:
				case nameof(MyDataType.Id):				return myDataTypes.OrderBy(myDataType => myDataType.Id,				sortAscending);
				case nameof(MyDataType.Name):			return myDataTypes.OrderBy(myDataType => myDataType.Name,			sortAscending);
				case nameof(MyDataType.Description):	return myDataTypes.OrderBy(myDataType => myDataType.Description,	sortAscending);
				case nameof(MyDataType.Price):			return myDataTypes.OrderBy(myDataType => myDataType.Price,			sortAscending);
				case nameof(MyDataType.ExpirationDate):	return myDataTypes.OrderBy(myDataType => myDataType.ExpirationDate,	sortAscending);
			}
		}
	}
}
