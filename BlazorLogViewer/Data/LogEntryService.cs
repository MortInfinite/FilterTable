using System;
using System.ComponentModel;
using System.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Text.Json;
using System.Text.Json.Serialization;
using FilterTypes;
using LogData;
using Microsoft.Extensions.Configuration;

namespace BlazorLogViewer.Data
{
	/// <summary>
	/// Retrieves log entry data from the log en try data access web service.
	/// </summary>
	public partial class LogEntryService :IDisposable
	{
		public LogEntryService(IConfiguration configuration, HttpClient client)
		{
			string? webServiceUri = configuration["LogEntryServiceUrl"];
			if(string.IsNullOrEmpty(webServiceUri))
				throw new ConfigurationErrorsException($"The \"LogEntryServiceUrl\" appSettings entry is missing.");

			Client = client;
		}

		#region IDisposable Members
		/// <summary>
		/// Dispose of the object and its unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose pattern implementation.
		/// </summary>
		/// <param name="disposing">True if disposing, false if finalizing.</param>
		protected virtual void Dispose(bool disposing)
		{
			if(Disposed)
				return;

			// Dispose unmanaged code here.

			if(disposing)
			{
				// Dispose managed code here.
				Client?.Dispose();
			}

			Disposed = true;
		}

		/// <summary>
		/// Indicates if the object has been disposed.
		/// </summary>
		public bool Disposed
		{
			get;
			protected set;
		}
		#endregion

		/// <summary>
		/// Test the connection to the log entry service.
		/// </summary>
		/// <param name="cancellationToken">Token used to cancel the operation.</param>
		/// <returns>Result returned from the service.</returns>
		/// <exception cref="Exception">Thrown if the call failed.</exception>
		public async Task<string> Ping(CancellationToken cancellationToken=default)
		{
			// Retrieve log entry data from the LogEntryDataAccess web service.
			using(HttpResponseMessage message = await Client.GetAsync($"api/LogEntry/Ping", cancellationToken))
			{
				if(!message.IsSuccessStatusCode)
					throw new Exception($"Failed to ping LogEntryService, error code {message.StatusCode}.");

				string result = await message.Content.ReadAsStringAsync();
				return result;
			}
		}

		/// <summary>
		/// Retrieve a filtered list of log entries.
		/// </summary>
		/// <param name="filter">Filter expressions used to filter the list of log entries.</param>
		/// <param name="sortLabel">Name of the property that the query should be ordered in.</param>
		/// <param name="sortAscending">Indicates if sorting is ascending (Otherwise it will be descending).</param>
		/// <param name="skip">Number of items to skip, in the query result. This is used for paging through a large result set.</param>
		/// <param name="maxCount">Maximum number of results to retrieve.</param>
		/// <returns>
		/// Query result containing total number of available results, matching the filter, and a list of results not 
		/// exceeding the number of results specified in <paramref name="maxCount"/>.
		/// </returns>
		public async Task<QueryResult<LogEntry>> GetLogEntries(FilterOperation[] filter, string sortLabel="Id", bool sortAscending=true, int skip = 0, int maxCount=100, CancellationToken cancellationToken=default)
		{
			GetLogEntriesArguments getLogEntriesArguments = new GetLogEntriesArguments(filter, sortLabel ?? nameof(LogEntry.Id), sortAscending, skip, maxCount);

			JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions();
			jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

			// Retrieve log entry data from the LogEntryDataAccess web service.
			using(HttpResponseMessage message = await Client.PostAsJsonAsync($"api/LogEntry", getLogEntriesArguments, jsonSerializerOptions, cancellationToken))
			{
				if(!message.IsSuccessStatusCode)
					throw new Exception($"Failed to retrieve log entries, error code {message.StatusCode}.");

				QueryResult<LogEntry> result = await message.Content.ReadFromJsonAsync<QueryResult<LogEntry>>();

				return result;
			}
		}

		#region Types
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
			public GetLogEntriesArguments(FilterOperation[] filter, string sortLabel="Id", bool sortAscending=true, int skip=0, int maxCount=100)
			{
				Filter			= filter ?? new FilterOperation[0];
				SortLabel		= sortLabel;
				SortAscending	= sortAscending;
				Skip			= skip;
				MaxCount		= maxCount;
			}

			/// <summary>
			/// Filter operations used to filter the list of log entries.
			/// </summary>
			public FilterOperation[] Filter {get;set;} = new FilterOperation[0];

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
		/*
		public struct GetLogEntriesArguments
		{
			public GetLogEntriesArguments(FilterOperation[] filter, string sortLabel="Id", bool sortAscending=true, int skip=0, int maxCount=100)
			{
				if(filter is null)
					throw new ArgumentNullException(nameof(filter));

				Filter			= filter;
				SortLabel		= sortLabel;
				SortAscending	= sortAscending;
				Skip			= skip;
				MaxCount		= maxCount;
			}

			/// <summary>
			/// Filter operations used to filter the list of log entries.
			/// </summary>
			public FilterOperation[] Filter {get;set;}

			/// <summary>
			/// Name of the property that the query should be ordered in.
			/// </summary>
			[DefaultValue("Id")]
			public string SortLabel {get;set;}

			/// <summary>
			/// Indicates if sorting is ascending (Otherwise it will be descending).
			/// </summary>
			[DefaultValue(true)]
			public bool SortAscending {get;set;}

			/// <summary>
			/// Number of items to skip, in the query result. This is used for paging through a large result set.
			/// </summary>
			[DefaultValue("0")]
			public int Skip  {get;set;}

			/// <summary>
			/// Maximum number of results to retrieve.
			/// </summary>
			[DefaultValue("100")]
			public int MaxCount  {get;set;}
		}
		*/
		#endregion

		#region Properties
		/// <summary>
		/// Client used to connect to the web service.
		/// </summary>
		protected virtual HttpClient Client
		{
			get;
			set;
		}
		#endregion
	}
}
