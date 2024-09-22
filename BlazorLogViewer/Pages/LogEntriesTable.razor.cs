using System.Linq.Expressions;
using System.Reflection;
using BlazorLogViewer.Data;
using BlazorLogViewer.Shared;
using Microsoft.AspNetCore.Components;
using FilterTable;
using LogData;
using FilterTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace BlazorLogViewer.Pages
{
	public partial class LogEntriesTable
	{
		#region Methods
		/// <summary>
		/// Updates the properties from the query string.
		/// </summary>
		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			await base.OnAfterRenderAsync(firstRender);

			// Set default values on the table.
			// Unfortunately this can't be done on the razor page, it has to be done in code behind.
			if(Table != null)
			{
				Table.DefaultDataFilterOperators	[nameof(LogEntry.TimeStamp)]	= FilterOperators.GreaterThanOrEqual;
				Table.DefaultDataFilterValues		[nameof(LogEntry.TimeStamp)]	= Configuration["TimeSpan"] ?? "-1";
			}
			
			// Copy query string parameters to the table's filters.
			if(!InitialRenderingComplete)
				GetPropertiesFromQueryString();
			
			InitialRenderingComplete = true;
		}

		/// <summary>
		/// Call the log entry service, to determine if the service can be found.
		/// </summary>
		/// <param name="cancellationToken">Token used to cancel the operation.</param>
		/// <returns>Result returned from the Ping call.</returns>
		protected virtual async Task<string> Ping(CancellationToken cancellationToken)
		{ 
			try
			{
				return await LogEntryService.Ping(cancellationToken);
			}
			catch(Exception exception)
			{ 
				return exception.Message;
			}
		}

		/// <summary>
		/// Retrieves the filtered set of results.
		/// </summary>
		/// <summary>
		/// Retrieves data to display in the table.
		/// </summary>
		/// <param name="filters">Filters to apply to the data.</param>
		/// <param name="sortLabel">Name of the property that the query should be ordered in.</param>
		/// <param name="sortAscending">Indicates if sorting is ascending (Otherwise it will be descending).</param>
		/// <param name="skip">Number of items to skip, in the query result. This is used for paging through a large result set.</param>
		/// <param name="maxCount">Maximum number of results to retrieve.</param>
		/// <param name="cancellationToken">Token used to cancel the retrieval of data.</param>
		/// <returns>Filtered results.</returns>
		protected virtual async Task<DataTable<LogEntry>.FilteredResult> GetData(FilterOperationValue[] filters, string sortLabel, bool sortAscending, int skip, int maxCount, CancellationToken cancellationToken)
		{ 
			try
			{
				// Retrieve the filtered query result, from the log entry service.
				QueryResult<LogEntry> queryResult = await LogEntryService.GetLogEntries(filters, sortLabel, sortAscending, skip, maxCount, cancellationToken);

				DataTable<LogEntry>.FilteredResult filteredResult = new DataTable<LogEntry>.FilteredResult()
				{ 
					TotalCount	= queryResult.TotalCount,
					Items		= queryResult.Results
				};

				// Remember the error message returned by calling the log entry service.
				Exception = queryResult.Exception;

				return filteredResult;
			}
			catch(Exception exception)
			{ 
				// If the LogEntryService throws an Exception with the text "'TypeError: Failed to fetch'", this means that the web browser 
				// can't connect to the LogEntryService and URL specified in the "LogEntryServiceUrl" setting of the "appsettings.json" 
				// (Found in "wwwroot") is most likely doesn't match the correct address of the LogEntryDataAccess project.

				// Remember the error message thrown when calling the log entry service.
				Exception = exception;

				// Return an empty result.
				DataTable<LogEntry>.FilteredResult filteredResult = new DataTable<LogEntry>.FilteredResult()
				{ 
					TotalCount	= 0,
					Items		= new LogEntry[0]
				};

				return filteredResult;
			}
		}

		/// <summary>
		/// Changes the format of date time values, displayed in the table.
		/// </summary>
		/// <param name="context">Object from which to retrieve the value of the <paramref name="property"/>.</param>
		/// <param name="property">Property to retrieve from the <paramref name="context"/>.</param>
		/// <returns>Value of the property.</returns>
		protected virtual object? GetPropertyValue(object context, PropertyInfo property)
		{
			object? value = Table?.GetPropertyValueDefault(context!, property);
			if(value == null)
				return null;

			// Format dates.
			if(value is DateTime dateTimeValue)
				return dateTimeValue.ToString(DateTimeFormat, CultureInfo.InvariantCulture);

			// Format durations.
			if(value is TimeSpan timeSpanValue)
				return timeSpanValue.ToString(TimeSpanFormat, CultureInfo.InvariantCulture);

			return value;
		}

		/// <summary>
		/// Updates the navigation manager query filter, when a data filter operator changes.
		/// </summary>
		/// <param name="propertyName">Name of the data filter operator that changed.</param>
		/// <param name="filterOperationChangedArgs">Information about how the filter operation changed.</param>
		protected void FilterOperationChanged(DataTable<LogEntry>.FilterOperationChangedArgs filterOperationChangedArgs)
		{ 
			// Don't update the query string, if the DataFilterOperator was changed while reading the query string.
			if(UpdatingPropertiesFromQueryString)
				return;

			// Update the navigation manager query filter
			SetQueryStringFromProperties();
		}

		/// <summary>
		/// Updates properties with values from the query string.
		/// </summary>
		protected virtual void GetPropertiesFromQueryString()
		{ 
			if(NavigationManager == null || Table == null)
				return;
			if(UpdatingPropertiesFromQueryString)
				return;

			try
			{
				// Indicate that events indicating that Table.DataFilterValues and Table.DataFilterOperators have changed, should be ignored,
				// instead of calling SetQueryStringFromProperties to update the query string due to the properties changing.
				UpdatingPropertiesFromQueryString = true;

				// Extract each key/value pair from the query string.
				IList<KeyValuePair<string, string>> queryStringParts = NavigationManager.GetQueryString();
				List<FilterOperation> filterOperations = new List<FilterOperation>();

				// Parse each numbered set of filter operators, up to a maximum of the number of query string parts specified.
				// This is done to find the property, value and operator that belong together.
				for(int count=0; count<queryStringParts.Count; count++)
				{
					// Retrieve the filter values from the query string.
					IEnumerable<KeyValuePair<string, string>> filterValues = queryStringParts.Where(queryStringPart => queryStringPart.Key.EndsWith($"_V{count}"));
					if(filterValues.Any())
					{
						KeyValuePair<string, string> filterValue = filterValues.Last();

						// Get the name of the property, to which the value belongs.
						string property = filterValue.Key.Substring(0, filterValue.Key.Length-$"_V{count}".Length);

						// Get the filter operator as a string.
						string? filterOperatorString = NavigationManager.TryGetQueryString<string>($"{property}_O{count}")?.LastOrDefault();

						// Attempt to parse the filter operator.
						if(!Enum.TryParse<FilterOperators>(filterOperatorString, true, out var filterOperator))
							filterOperator = FilterOperators.Equals;

						FilterOperation currentFilterOperation = new FilterOperation()
						{
							Property	= property,
							Value		= filterValue.Value,
							Operator	= filterOperator
						};

						filterOperations.Add(currentFilterOperation);
					}
				}

				// Determine if the generated list of filters match the filters that already exist.
				bool filtersIdentical = filterOperations.All(newFilterOperation => Table.FilterOperations.Any(existingFilterOperation => newFilterOperation.Equals(existingFilterOperation))) && filterOperations.Count == Table.FilterOperations.Count;
				if(filtersIdentical)
					return;

				// Replace the set of filters to use.
				Table.ReplaceFilters(filterOperations);
			}
			finally
			{ 
				UpdatingPropertiesFromQueryString = false;
			}
		}

		/// <summary>
		/// Read the properties and store each non-default property value and query operator in the query string.
		/// </summary>
		protected virtual void SetQueryStringFromProperties()
		{
			if(NavigationManager == null || Table == null)
				return;

			List<KeyValuePair<string, string>> nameValuePairs = new List<KeyValuePair<string, string>>();

			int filterOperationIndex = 0;

			foreach(var filterOperation in Table.FilterOperations)
			{ 
				if(!string.IsNullOrEmpty(filterOperation.Value))
				{
					// Add the value and operator of the filter operation, as query parameters.
					// The value and operator share the same unique number, indicating that they belong to the same filter operator.
					nameValuePairs.Add(new KeyValuePair<string, string>(filterOperation.Property+$"_V{filterOperationIndex}", filterOperation.Value));
					nameValuePairs.Add(new KeyValuePair<string, string>(filterOperation.Property+$"_O{filterOperationIndex}", filterOperation.Operator.ToString()));

					filterOperationIndex++;
				}
			}

			NavigationManager.SetQueryString(nameValuePairs);
		}
		#endregion

		#region Properties
		/// <summary>
		/// Items selected in the table.
		/// </summary>
		public ISet<LogEntry> SelectedItems
		{
			get; 
		} = new HashSet<LogEntry>();

		/// <summary>
		/// Error that occurred on the page.
		/// </summary>
		public Exception? Exception
		{
			get; 
			protected set;
		}

		/// <summary>
		/// Table containing log entries.
		/// </summary>
		protected DataTable<LogEntry>? Table
		{ 
			get; 
			set;
		}

		/// <summary>
		/// Expression filters defined for each DataFilter column.
		/// </summary>
		protected virtual Dictionary<string, Expression<Func<LogEntry, bool>>> Filters
		{
			get; 
			set;
		} = new Dictionary<string, Expression<Func<LogEntry, bool>>>();

		/// <summary>
		/// Used to retrieve the URL of the page, including its query parameters.
		/// </summary>
		[Inject]
		protected virtual NavigationManager? NavigationManager
		{
			get;
			set;
		}

		/// <summary>
		/// Used to retrieve the default TimeStamp value.
		/// </summary>
		[Inject]
		protected virtual IConfiguration Configuration
		{ 
			get;
			set;
		}

		/// <summary>
		/// Indicates if <see cref="OnAfterRenderAsync"/> has been called at least once.
		/// 
		/// When <see cref="OnAfterRenderAsync"/> has been called, the <see cref="Filters"/> have been updated and any changes made to a filter, 
		/// will cause the TableData to be reloaded, when a <see cref="FilterExpressionChanged"/> is called due to a filter expression being updated.
		/// </summary>
		protected virtual bool InitialRenderingComplete
		{
			get;
			set;
		}

		/// <summary>
		/// Indicates if the <see cref="Table.DataFilterValues"/> and <see cref="Table.DataFilterOperators"/> properties are being updated,
		/// with values from the URL's query string. While these properties are being updated, property changed events from these properties
		/// should not call SetQueryStringFromProperties to update the URL in response to the properties changing.
		/// </summary>
		protected virtual bool UpdatingPropertiesFromQueryString
		{
			get;
			set;
		}

		/// <summary>
		/// Format string used to format <see cref="DateTime"/> values shown in the table.
		/// </summary>
		public string DateTimeFormat
		{
			get; 
			set;
		} = "yyyy-MM-dd hh:mm:ss.ffff";

		/// <summary>
		/// Format string used to format <see cref="TimeSpan"/> values shown in the table.
		/// </summary>
		public string TimeSpanFormat
		{
			get; 
			set;
		} = "-d hh:mm:ss.ffff";
		#endregion
	}
}

