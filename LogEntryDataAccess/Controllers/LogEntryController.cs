using System.ComponentModel;
using FilterTypes;
using LogData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// TODO: Determine if https://github.com/6bee/Remote.Linq library (Remote.Linq NuGet package), provides desired functionality
// to serialize and deserialize a LINQ expression.

namespace LogEntryDataAccess.Controllers
{
	/// <summary>
	/// Reads <see cref="LogEntry"/> data from the database.
	/// </summary>
	/// <seealso cref="https://www.c-sharpcorner.com/article/enable-windows-authentication-in-web-api-and-angular-app/"/>
	[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LogEntryController : ControllerBase
    {
		/// <summary>
		/// Create a new LogEntryController.
		/// </summary>
		/// <param name="logEntryService">Service used to read log entries from the database.</param>
		public LogEntryController(LogEntryService logEntryService, ILogger<LogEntryController> logger)
		{
			LogEntryService = logEntryService;
			Logger			= logger;
		}

		/// <summary>
		/// Test that the controller can be called by the client.
		/// </summary>
		/// <returns></returns>
		[HttpGet("Ping")]
		public Task<ActionResult<string>> Ping()
		{ 
			return Task.Run(()=>
			{
				Logger.Log(LogLevel.Information, $"Pinged by user \"{User?.Identity?.Name}\".");

				return new ActionResult<string>(DateTime.Now.ToString());
			});
		}

		/// <summary>
		/// Retrieve a filtered list of log entries.
		/// </summary>
		/// <param name="getArguments">Single argument object containing, containing all required arguments.</param>
		/// <param name="cancellationToken">Token used to cancel the operation.</param>
		/// <returns>
		/// Query result containing total number of available results, matching the filter, and a list of results not 
		/// exceeding the number of results specified in <paramref name="maxCount"/>.
		/// </returns>
		[HttpPost]
        public async Task<ActionResult<QueryResult<LogEntry>>> GetLogEntries([FromBody] GetLogEntriesArguments getArguments, CancellationToken cancellationToken=default)
        {
			if(!ModelState.IsValid)
			{
				Logger.Log(LogLevel.Warning, $"Invalid model state ancountered trying to get log entries for user \"{User?.Identity?.Name}\".");
				return BadRequest(ModelState);
			}

			Logger.Log(LogLevel.Information, $"Getting log entries for user \"{User?.Identity?.Name}\".");

			QueryResult<LogEntry> result = await LogEntryService.GetFilteredValues(getArguments.Filter, getArguments.SortLabel, getArguments.SortAscending, getArguments.Skip, getArguments.MaxCount, cancellationToken);
			return new ActionResult<QueryResult<LogEntry>>(result);
        }

		/// <summary>
		/// Arguments needed to call the <see cref="Post"/> method.
		/// </summary>
		public struct GetLogEntriesArguments
		{
			/// <summary>
			/// Create a new GetLogEntriesArguments with default values.
			/// </summary>
			public GetLogEntriesArguments()
			{
			}

			/// <summary>
			/// Create a new GetLogEntriesArguments.
			/// </summary>
			/// <param name="filter">Filter operations used to filter the list of log entries.</param>
			/// <param name="sortLabel">Name of the property that the query should be ordered in.</param>
			/// <param name="sortAscending">Indicates if sorting is ascending (Otherwise it will be descending).</param>
			/// <param name="skip">Number of items to skip, in the query result. This is used for paging through a large result set.</param>
			/// <param name="maxCount">Maximum number of results to retrieve.</param>
			/// <exception cref="ArgumentNullException">Thrown if the filter is null.</exception>
			public GetLogEntriesArguments(FilterOperationValue[] filter, string sortLabel="Id", bool sortAscending=true, int skip=0, int maxCount=100)
			{
				Filter			= filter ?? new FilterOperationValue[0];
				SortLabel		= sortLabel;
				SortAscending	= sortAscending;
				Skip			= skip;
				MaxCount		= maxCount;
			}
			
			/// <summary>
			/// Filter operations used to filter the list of log entries.
			/// </summary>
			public FilterOperationValue[] Filter {get;set;} = new FilterOperationValue[0];

			/// <summary>
			/// Name of the property that the query should be ordered in.
			/// </summary>
			[DefaultValue("Id")]
			public string SortLabel {get;set;} = "Id";

			/// <summary>
			/// Indicates if sorting is ascending (Otherwise it will be descending).
			/// </summary>
			[DefaultValue(true)]
			public bool SortAscending {get;set;} = true;

			/// <summary>
			/// Number of items to skip, in the query result. This is used for paging through a large result set.
			/// </summary>
			[DefaultValue("0")]
			public int Skip  {get;set;} = 0;

			/// <summary>
			/// Maximum number of results to retrieve.
			/// </summary>
			[DefaultValue("100")]
			public int MaxCount  {get;set;} = 100;
		}

		#region Properties
		/// <summary>
		/// Service used to read log entries from the database.
		/// </summary>
		protected virtual LogEntryService LogEntryService
		{
			get;
		}

		/// <summary>
		/// Service used to write log entries.
		/// </summary>
		protected virtual ILogger Logger
		{
			get; 
			set;
		}
		#endregion
	}
}
