using FilterDataAccess;
using LogData;
using Microsoft.EntityFrameworkCore;

namespace LogEntryDataAccess
{
	/// <summary>
	/// Retrieves log entries from the database, based on a set of filter operations.
	/// </summary>
	/// <remarks>
	/// To create scaffolding, call the following command in Package Manager Console:
	/// <![CDATA[
	/// Scaffold-DbContext -Provider Microsoft.EntityFrameworkCore.SqlServer -Connection name=LoggingConnection
	/// ]]>
	/// </remarks>
	public class LogEntryService	:DataAccessService<LogEntry, LoggingContext>
	{
		/// <summary>
		/// Creates a new log entry service.
		/// </summary>
		/// <param name="loggingContextFactory">Database context used to read <see cref="LogEntry"/> entries from the database.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public LogEntryService(IDbContextFactory<LoggingContext> loggingContextFactory)
			:base(loggingContextFactory, (loggingContext)=>loggingContext.LogEntries, (values, sortLabel, sortAscending) => values.SortData(sortLabel, sortAscending))
		{
		}
	}
}
