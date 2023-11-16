using System.Linq.Expressions;
using System.Reflection;
using BlazorLogViewer.Data;
using BlazorLogViewer.Shared;
using Microsoft.AspNetCore.Components;
using FilterTable;
using LogData;
using FilterTypes;
using Microsoft.Extensions.Configuration;

namespace BlazorLogViewer.Pages
{
	public partial class LogEntriesTable
	{
		#region Methods
		/// <summary>
		/// Updates the properties from the query string and ensures that the navigation manager location is monitored for changes.
		/// </summary>
		protected override void OnInitialized()
		{
			// Receive events when the query string changes.
			if(NavigationManager != null)
				NavigationManager.LocationChanged    += NavigationManager_LocationChanged;
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			await base.OnAfterRenderAsync(firstRender);

			await Ping(CancellationToken.None);

			// Set default values on the table.
			// Unfortunately this can't be done on the razor page, it has to be done in code behind.
			if(Table != null)
			{
				Table.DefaultDataFilterOperators	[nameof(LogEntry.TimeStamp)]	= FilterOperators.GreaterThanOrEqual;
				Table.DefaultDataFilterValues		[nameof(LogEntry.TimeStamp)]	= Configuration["TimeSpan"] ?? "-1";
			}

			// Copy query string parameters to the table's DataFilterOperators and DataFilterValues.
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
		protected virtual async Task<DataTable<LogEntry>.FilteredResult> GetData(FilterOperation[] filters, string sortLabel, bool sortAscending, int skip, int maxCount, CancellationToken cancellationToken)
		{ 
			try
			{
				// Retrieve the filtered query result, from the log entry service.
				QueryResult<LogEntry> queryResult = await LogEntryService.GetLogEntries(filters, sortLabel, sortAscending, skip, maxCount, cancellationToken);

				DataTable<LogEntry>.FilteredResult filteredResult = new DataTable<LogEntry>.FilteredResult()
				{ 
					TotalCount	= queryResult.TotalCount,
					Items		= queryResult.LogEntries
				};

				return filteredResult;
			}
			catch(Exception exception)
			{ 
				// If the LogEntryService throws an Exception with the text "'TypeError: Failed to fetch'", this means that the web browser 
				// can't connect to the LogEntryService and URL specified in the "LogEntryServiceUrl" setting of the "appsettings.json" 
				// (Found in "wwwroot") is most likely doesn't match the correct address of the LogEntryDataAccess project.

				// Remember the error message returned by calling the log entry service.
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
		/// Updates the navigation manager query filter, when a data filter operator changes.
		/// </summary>
		/// <param name="propertyName">Name of the data filter operator that changed.</param>
		protected void DataFilterOperatorChanged(string propertyName)
		{ 
			// Don't update the query string, if the DataFilterOperator was changed while reading the query string.
			if(UpdatingPropertiesFromQueryString)
				return;

			// Update the navigation manager query filter
			SetQueryStringFromProperties();
		}

		/// <summary>
		/// Updates the navigation manager query filter, when a data filter value changes.
		/// </summary>
		/// <param name="propertyName">Name of the data filter value that changed.</param>
		protected void DataFilterValueChanged(string propertyName)
		{ 
			// Don't update the query string, if the DataFilterValue was changed while reading the query string.
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

				bool modified = false;
				foreach(PropertyInfo property in Table.Properties)
				{ 
					string? defaultValue = Table.DefaultDataFilterValues.GetValueOrDefault(property.Name, null);
					string? currentValue = Table.DataFilterValues[property.Name];

					// Retrieve the filter value from the query string and check if the query string filter value differs from the current filter value.
					string? newValue = NavigationManager.TryGetQueryString<string>(property.Name+"_V", defaultValue);
					if(newValue != currentValue)
					{
						Table.DataFilterValues[property.Name] = newValue;
						modified = true;
					}

					FilterOperators defaultOperator = Table.DefaultDataFilterOperators.GetValueOrDefault(property.Name, FilterOperators.Equals);
					FilterOperators currentOperator = Table.DataFilterOperators[property.Name];

					// Retrieve the filter value from the query string and check if the query string filter operator differs from the current filter operator.
					FilterOperators newOperator = NavigationManager.TryGetQueryString(property.Name+"_O", defaultOperator);
					if(newOperator != currentOperator)
					{ 
						Table.DataFilterOperators[property.Name] = newOperator;
						modified = true;
					}
				}

				if(modified)
					StateHasChanged();
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

			List<KeyValuePair<string, string?>> nameValuePairs = new List<KeyValuePair<string, string?>>();

			// Add each property value and property operator to the query string, unless they contain default values.
			foreach(PropertyInfo property in Table.Properties)
			{ 
				// Add the property value, if it differs from the default value.
				string? value = Table.DataFilterValues[property.Name];
				string? defaultValue = Table.DefaultDataFilterValues.GetValueOrDefault(property.Name);
				if(value != defaultValue)
					nameValuePairs.Add(new KeyValuePair<string, string?>(property.Name+"_V", value));
				else
					nameValuePairs.Add(new KeyValuePair<string, string?>(property.Name+"_V", null));

				// Add the property filter operator, if it differs from the default value.
				FilterOperators filterOperator = Table.DataFilterOperators[property.Name];
				FilterOperators defaultFilterOperator = Table.DefaultDataFilterOperators.GetValueOrDefault(property.Name);
				if(filterOperator != defaultFilterOperator)
					nameValuePairs.Add(new KeyValuePair<string, string?>(property.Name+"_O", filterOperator.ToString()));
				else
					nameValuePairs.Add(new KeyValuePair<string, string?>(property.Name+"_O", null));
			}

			NavigationManager.SetQueryString(nameValuePairs);
		}
		#endregion

		#region Event handlers
		/// <summary>
		/// Updates properties with values from the query string, when the location changes.
		/// </summary>
		protected virtual void NavigationManager_LocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
		{
			GetPropertiesFromQueryString();
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
		#endregion
	}
}

