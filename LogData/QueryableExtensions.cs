using System.Linq.Expressions;

namespace LogData
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
		/// Sorts the specified log entries.
		/// </summary>
		/// <param name="logEntries">Log entries to sort.</param>
		/// <param name="sortLabel">Name of the field to sort by.</param>
		/// <param name="sortAscending">Indicates if results should be sorted in ascending order (true) or descending order (false).</param>
		public static IOrderedQueryable<LogEntry> SortData(this IQueryable<LogEntry> logEntries, string sortLabel, bool sortAscending=true)
		{ 
			// Sort data (This should have been done in the call to GetLogEntries).
			switch(sortLabel)
			{
				default:
				case nameof(LogEntry.Id):			return logEntries.OrderBy(logEntry => logEntry.Id,			sortAscending);
				case nameof(LogEntry.EventId):		return logEntries.OrderBy(logEntry => logEntry.EventId,		sortAscending);
				case nameof(LogEntry.TimeStamp):	return logEntries.OrderBy(logEntry => logEntry.TimeStamp,	sortAscending);
				case nameof(LogEntry.Category):		return logEntries.OrderBy(logEntry => logEntry.Category,	sortAscending);
				case nameof(LogEntry.LogLevel):		return logEntries.OrderBy(logEntry => logEntry.LogLevel,	sortAscending);
				case nameof(LogEntry.Message):		return logEntries.OrderBy(logEntry => logEntry.Message,		sortAscending);
				case nameof(LogEntry.Exception):	return logEntries.OrderBy(logEntry => logEntry.Exception,	sortAscending);
				case nameof(LogEntry.Payload):		return logEntries.OrderBy(logEntry => logEntry.Payload,		sortAscending);
				case nameof(LogEntry.PayloadType):	return logEntries.OrderBy(logEntry => logEntry.PayloadType,	sortAscending);
			}
		}
	}
}
