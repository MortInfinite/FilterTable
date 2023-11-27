using System.Linq.Expressions;

namespace FilterDataAccess
{
	/// <summary>
	/// Extension methods for the IQueryable data type.
	/// </summary>
	public static class QueryableExtensions
	{
		/// <summary>
		/// Sorts the specified data.
		/// </summary>
		/// <param name="source">Data to to sort.</param>
		/// <param name="sortLabel">Name of the field to sort by.</param>
		/// <param name="sortAscending">Indicates if results should be sorted in ascending order (true) or descending order (false).</param>
		public static IOrderedQueryable<T> SortData<T>(this IQueryable<T> source, string sortLabel, bool sortAscending=true)
		{
			// Define the type as a parameter.
			ParameterExpression parameterExpression = Expression.Parameter(typeof(T));

			// Define the property name as a parameter.
			MemberExpression memberExpression = Expression.Property(parameterExpression, sortLabel);

			// Convert the member into an object.
			UnaryExpression unaryExpression = Expression.Convert(memberExpression, typeof(object));

			// Define an expression that performs an operation on the property.
			Expression<Func<T, object>> expression = Expression.Lambda<Func<T, object>>(unaryExpression, parameterExpression);

			if(sortAscending)
				return source.OrderBy(expression);
			else
				return source.OrderByDescending(expression);
		}
	}
}
