using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FilterTypes;
using LogData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogEntryDataAccessUnitTest
{
	/// <summary>
	/// Unit tests used to test the LogEntryController web service.
	/// </summary>
	[TestClass]
    public class LogEntryControllerTest
    {
        #region Setup and cleanup
        [TestInitialize]
        public void TestSetup()
        {
			// Convert between strings and enums.
			JsonSerializerOptions = new JsonSerializerOptions();
			JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

		/// <summary>
		/// Clean up the HTTP client.
		/// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
        }
        #endregion

        #region Properties
		/// <summary>
		/// URL of the web service to connect to.
		/// </summary>
		protected string WebServiceUrl
		{
			get; 
		} = "https://localhost:7228";

		/// <summary>
		/// Serialization options used with the <see cref="Client"/>.
		/// </summary>
		protected JsonSerializerOptions JsonSerializerOptions
		{
			get;
			set;
		}

		#endregion

		#region Methods
		/// <summary>
		/// Create an HTTP Client used to communicate with the web service at the <see cref="WebServiceUrl"/>.
		/// </summary>
		/// <param name="negotiateCredentialsCache"></param>
		protected virtual HttpClient CreateHttpClient(bool negotiateCredentialsCache)
		{ 
			HttpClient client;

			if(negotiateCredentialsCache)
			{
				// Use current network credentials of the current user or application, to connect to the web service.
				CredentialCache credentialsCache = new CredentialCache();
				credentialsCache.Add(new Uri(WebServiceUrl, UriKind.Absolute), "Negotiate", CredentialCache.DefaultNetworkCredentials);

				HttpClientHandler handler = new HttpClientHandler 
				{ 
					// Doesn't work in WASM.
					Credentials = credentialsCache
				};

				client = new HttpClient(handler);
			}
			else
			{ 
				client = new HttpClient();
			}

            client.BaseAddress = new Uri(WebServiceUrl);

            // Accept json data type only.
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			return client;
		}
		#endregion

		#region Test methods
		/// <summary>
		/// Create an HTTP client, specifying client credentials from the calling process.
		/// 
		/// Get a list of log entries, matching a single filter.
		/// </summary>
		/// <returns>Created task.</returns>
		[TestMethod]
        public async Task GetLogEntriesUsingCredentialsCache()
        {
			GetLogEntriesArguments getLogEntriesArguments = new GetLogEntriesArguments(new FilterOperation[0], nameof(LogEntry.Id), true, 0, 1);

			// Retrieve log entry data from the LogEntryDataAccess web service.
			using(var client = CreateHttpClient(true))
			using(HttpResponseMessage message = await client.PostAsJsonAsync($"api/LogEntry", getLogEntriesArguments, JsonSerializerOptions))
			{
				Assert.IsTrue(message.IsSuccessStatusCode, $"The Web API server returned an error with status code: {message.StatusCode}.");

				QueryResult<LogEntry> result = await message.Content.ReadFromJsonAsync<QueryResult<LogEntry>>();
				Assert.IsTrue(result.TotalCount > 0, $"The call indicated that no data matched the filter.");
				Assert.IsNotNull(result.Results, $"The call returned no log entries.");
				Assert.IsTrue(result.Results[0].Id > 0, $"The ID of the first retrieved entry, was not valid.");
			}
        }

		/// <summary>
		/// Without specifying credentials, get a list of log entries, matching a single filter.
		/// </summary>
		/// <returns>Created task.</returns>
		[TestMethod]
		[ExpectedException(typeof(AssertFailedException))]
        public async Task GetLogEntries()
        {
			GetLogEntriesArguments getLogEntriesArguments = new GetLogEntriesArguments(new FilterOperation[0], nameof(LogEntry.Id), true, 0, 1);

			// Retrieve log entry data from the LogEntryDataAccess web service.
			using(var client = CreateHttpClient(false))
			using(HttpResponseMessage message = await client.PostAsJsonAsync($"api/LogEntry", getLogEntriesArguments, JsonSerializerOptions))
			{
				Assert.IsTrue(message.IsSuccessStatusCode, $"The Web API server returned an error with status code: {message.StatusCode}.");

				QueryResult<LogEntry> result = await message.Content.ReadFromJsonAsync<QueryResult<LogEntry>>();
				Assert.IsTrue(result.TotalCount > 0, $"The call indicated that no data matched the filter.");
				Assert.IsNotNull(result.Results, $"The call returned no log entries.");
				Assert.IsTrue(result.Results[0].Id > 0, $"The ID of the first retrieved entry, was not valid.");
			}
        }
		#endregion

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
		#endregion
	}
}