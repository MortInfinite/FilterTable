using MudBlazor;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using System.Reflection;
using Microsoft.JSInterop;

namespace FilterTable
{
	/// <summary>
	/// Filtered data table, displaying a filtered list of public properties contained in the data type <typeparamref name="T"/>.
	/// 
	/// Specify a <see cref="GetData"/> delegate used to retrieve a page of data.
	/// </summary>
	/// <typeparam name="T">Type of data to display in the table.</typeparam>
	/// <example>
	/// <![CDATA[
	/// <DataTable @ref="Table" T="MyDataType" GetData="GetMyDataType"/>
	/// 
	/// @{
	///		/// <summary>
	///		/// Returns a filtered result, by querying an EntityFramework DataContext.
	///		/// </summary>
	///		/// <summary>
	///		/// Retrieves data to display in the table.
	///		/// </summary>
	///		/// <param name="filters">Filters to apply to the data.</param>
	///		/// <param name="sortLabel">Name of the property that the query should be ordered in.</param>
	///		/// <param name="sortAscending">Indicates if sorting is ascending (Otherwise it will be descending).</param>
	///		/// <param name="skip">Number of items to skip, in the query result. This is used for paging through a large result set.</param>
	///		/// <param name="maxCount">Maximum number of results to retrieve.</param>
	///		/// <param name="cancellationToken">Token used to cancel the retrieval of data.</param>
	///		/// <returns>Filtered results.</returns>
	///		protected virtual async Task<DataTable<MyDataType>.FilteredResult> GetMyDataType(IEnumerable<Expression<Func<MyDataType, bool>>> filters, string sortLabel, bool sortAscending, int skip, int maxCount, CancellationToken cancellationToken)
	///		{ 
	///			using(MyDataTypeContext myDataTypeContext = await MyDataTypeContextFactory.CreateDbContextAsync(cancellationToken))
	///			{
	///				// Queryable set of data, retrieved from the EntityFramework data context.
	///				IQueryable<MyDataType>	myDataTypes = myDataTypeContext.MyDataTypes;
	///				
	///				// Filtered set of results to return.
	///				DataTable<MyDataType>.FilteredResult filteredResult = new DataTable<MyDataType>.FilteredResult();
	///		
	///				try
	///				{
	///					// Apply filter expressions.
	///					foreach(Expression<Func<MyDataType, bool>>? filter in filters)
	///						myDataTypes = myDataTypes.Where(filter);
	///		
	///					// Sort data by the sorting label.
	///					myDataTypes = myDataTypes.SortData(sortLabel, sortAscending);
	///		
	///					// Determine how many elements match the specified filter.
	///					int totalCount = await myDataTypes.CountAsync(cancellationToken);
	///		
	///					// Indicate the total number of results available.
	///					filteredResult.TotalCount = totalCount;
	///		
	///					if(skip < 0)
	///						skip = 0;
	///		
	///					// Determine the maximum number of results to retrieve.
	///					// This is done to prevent the SQL Server from hanging forever, if requesting more results than exist.
	///					int maxTake = Math.Min(totalCount-skip, maxCount);
	///		
	///					// If there are any results and the current page contains any elements.
	///					if(totalCount > 0 && maxTake > 0)
	///					{
	///						// Retrieve a subset of data.
	///						filteredResult.Items = await myDataTypes.Skip(skip).Take(maxTake).ToArrayAsync(cancellationToken);
	///					}
	///					else
	///					{ 
	///						filteredResult.Items = new MyDataType[0];
	///					}
	///				}
	///				catch
	///				{ 
	///					filteredResult.TotalCount	= 0;
	///					filteredResult.Items		= new MyDataType[0];
	///				}
	///
	///				return filteredResult;
	///			}
	///		}
	/// }
	/// ]]>
	/// </example>
	public partial class DataTable<T>
	{
		public DataTable()
		{
		}

		#region Types
		/// <summary>
		/// Delegate used to return a filtered set of results.
		/// </summary>
		/// <param name="filters">Filters to apply to the data.</param>
		/// <param name="sortLabel">Name of the property that the query should be ordered in.</param>
		/// <param name="sortAscending">Indicates if sorting is ascending (Otherwise it will be descending).</param>
		/// <param name="skip">Offset of which data to return, from the beginning of the data. (0 = Start of data).</param>
		/// <param name="maxCount">Maximum number of items to return.</param>
		/// <param name="cancellationToken">Token used to cancel the retrieval of data.</param>
		/// <returns>Filtered results.</returns>
		public delegate Task<FilteredResult> FilterExpressionsDelegate(IEnumerable<Expression<Func<T, bool>>> filters, string sortLabel, bool sortAscending, int skip, int maxCount, CancellationToken cancellationToken);

		/// <summary>
		/// Number of total results and list of filtered results.
		/// </summary>
		public struct FilteredResult
		{
			/// <summary>
			/// Total number of filtered results available.
			/// </summary>
			public int TotalCount 
			{
				get; 
				set; 
			}

			/// <summary>
			/// Set of results to show.
			/// 
			/// This may contain a sub set of the <see cref="TotalCount"/>.
			/// </summary>
			public IEnumerable<T> Items
			{
				get; 
				set; 
			}
		}

		#endregion

		#region Methods
		/// <summary>
		/// Updates the properties from the query string and ensures that the navigation manager location is monitored for changes.
		/// </summary>
		protected override void OnInitialized()
		{
			Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

			Dictionary<string, FilterOperators> dataFilterOperators = new Dictionary<string, FilterOperators>();
			Dictionary<string, string?>			dataFilterValues	= new Dictionary<string, string?>();

			foreach(PropertyInfo property in Properties)
			{
				// Retrieve the initial values to use for each data filter.
				FilterOperators	filterOperator	= DefaultDataFilterOperators.GetValueOrDefault(property.Name, FilterOperators.Equals);
				string?			filterValue		= DefaultDataFilterValues.GetValueOrDefault(property.Name, null);

				dataFilterOperators.Add(property.Name, filterOperator);
				dataFilterValues.Add(property.Name, filterValue);
			}

			DataFilterOperators = dataFilterOperators;
			DataFilterValues	= dataFilterValues;
		}

		/// <summary>
		/// Update the expression filters defined for each DataFilter column, the first time the page is loaded.
		/// </summary>
		/// <param name="firstRender">Not used.</param>
		/// <returns>Created task.</returns>
		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			await base.OnAfterRenderAsync(firstRender);

			if(!InitialRenderingComplete)
			{
				// Copy the initial data filter operator values to the data filter operators.
				foreach(KeyValuePair<string, FilterOperators> initialDataFilterOperators in DefaultDataFilterOperators)
					if(DataFilterOperators.ContainsKey(initialDataFilterOperators.Key))
						DataFilterOperators[initialDataFilterOperators.Key] = initialDataFilterOperators.Value;

				// Copy the initial data filter values to the data filter values.
				foreach(KeyValuePair<string, string?> initialDataFilterValue in DefaultDataFilterValues)
					if(DataFilterValues.ContainsKey(initialDataFilterValue.Key))
						DataFilterValues[initialDataFilterValue.Key] = initialDataFilterValue.Value;

				// Update the expression filters defined for each DataFilter column.
				UpdateFilters();

				InitialRenderingComplete = true;
			}
		}

		/// <summary>
		/// Retrieve the value of the specified <paramref name="property"/>, from the <paramref name="context"/>.
		/// </summary>
		/// <param name="context">Object from which to retrieve the value of the <paramref name="property"/>.</param>
		/// <param name="property">Property to retrieve from the <paramref name="context"/>.</param>
		/// <returns>Value of the property.</returns>
		protected virtual object? GetPropertyValue(object context, PropertyInfo property)
		{ 
			object? value = property.GetValue(context);
			return value;
		}

		/// <summary>
		/// Updates the expression filters defined for each DataFilter column.
		/// </summary>
		protected virtual void UpdateFilters()
		{ 
			if(DataFilterReferences == null)
				return;

			Dictionary<string, Expression<Func<T, bool>>> filters = new Dictionary<string, Expression<Func<T, bool>>>();

			foreach(var dataFilterReference in DataFilterReferences)
				if(dataFilterReference.Value?.FilterExpression != null)
					filters[dataFilterReference.Key] = dataFilterReference.Value.FilterExpression;

			Filters = filters;
		}

		/// <summary>
		/// Load data from the database.
		/// </summary>
		/// <param name="state">Table view state, containing sort order.</param>
		/// <returns>Table data containing matching items.</returns>
		private async Task<TableData<T>> LoadData(TableState state)
		{
			var tableData = new TableData<T>()
			{ 
				TotalItems	= 0,
				Items		= null
			};

			if(GetData == null)
				return tableData;

			try
			{
				// Disable the copy to clipboard button, while loading data.
				CanCopyToClipboard = false;

				DataTable<T>.FilteredResult filteredResult = await GetData(Filters.Values.ToArray(), state.SortLabel, state.SortDirection == SortDirection.Ascending, state.Page * state.PageSize, state.PageSize, default);

				tableData.TotalItems	= filteredResult.TotalCount;
				tableData.Items			= filteredResult.Items;

				Exception = null;
			}
			catch(Exception exception)
			{ 
				Exception	= exception;
			}
			finally
			{ 
				// Enable the copy to clipboard button, if the table contains any results.
				if(tableData.Items?.Any() ?? false)
					CanCopyToClipboard = true;
			}

			return tableData;
		}

		/// <summary>
		/// Filter the queryable collection of items, by applying all filters in the <see cref="Filters"/> property and then
		/// filter the remaining results by the <paramref name="searchString"/>.
		/// </summary>
		/// <param name="items">Items to filter.</param>
		/// <returns>Filtered collection of items.</returns>
		protected IQueryable<T> Filter(IQueryable<T> items)
		{ 
			// Apply every data filter to the queriable expression.
			items = ApplyFilterExpressions(items);
			return items;
		}

		/// <summary>
		/// Apply every filter specified in the <see cref="Filters"/> dictionary, to the specified <paramref name="items"/>.
		/// </summary>
		/// <param name="items">Queryable items to filter.</param>
		/// <returns>Queryable items, with an added filter.</returns>
		protected IQueryable<T> ApplyFilterExpressions(IQueryable<T> items)
		{ 
			foreach(Expression<Func<T, bool>>? expression in Filters.Values)
				items = items.Where(expression);

			return items;
		}

		/// <summary>
		/// Add the clicked item to the list of selected items.
		/// </summary>
		/// <param name="args">Click event, containing the selected argument.</param>
		protected virtual void OnRowClick(TableRowClickEventArgs<T> args)
		{
			bool addItem = !SelectedItems.Contains(args.Item);
			if(addItem)
				SelectedItems.Add(args.Item);
			else
				SelectedItems.Remove(args.Item);
		}

		/// <summary>
		/// Determine which class should be applied to the row.
		/// </summary>
		/// <param name="element">Element contained in the row.</param>
		/// <param name="rowNumber">Row number containing the element.</param>
		/// <returns>Class to add to the row.</returns>
		protected virtual string GetRowStyle(T element, int rowNumber)
		{
			if(SelectedItems.Contains(element))
				return "selected";

			return string.Empty;
		}
		#endregion

		#region Event handlers
		/// <summary>
		/// Update the filter dictionary with the new filter expression, belonging to the specified property.
		/// </summary>
		/// <param name="property">
		/// Property that this filter applies to. 
		/// This property is used to uniquely identify the filter expression, so it can be replaced when the filter changes.
		/// </param>
		/// <param name="expression">Filter expression to apply.</param>
		protected async Task FilterExpressionChanged(string property, Expression<Func<T, bool>>? expression)
		{
			if(!InitialRenderingComplete)
				return;

			// Update the Filters property with filter expressions from each DataColumn.
			UpdateFilters();

			// Ask the data table to request new data to be loaded, by calling the LoadData method.
			if(Table != null)
				await Table.ReloadServerData();
		}

		/// <summary>
		/// Updates the <see cref="DataFilterOperators"/> and calls the <see cref="DataFilterOperatorChanged"/> event callback.
		/// </summary>
		/// <param name="property">Name of the property that changed.</param>
		/// <param name="newValue">New value of the property.</param>
		/// <returns>Created task.</returns>
		protected async Task UpdateDataFilterOperator(string property, FilterOperators newValue)
		{ 
			DataFilterOperators[property] = newValue;

			await DataFilterOperatorChanged.InvokeAsync(property);
		}

		/// <summary>
		/// Updates the <see cref="DataFilterValues"/> and calls the <see cref="DataFilterValuesChanged"/> event callback.
		/// </summary>
		/// <param name="property">Name of the property that changed.</param>
		/// <param name="newValue">New value of the property.</param>
		/// <returns>Created task.</returns>
		protected async Task UpdateDataFilterValue(string property, string? newValue)
		{ 
			DataFilterValues[property] = newValue;

			await DataFilterValueChanged.InvokeAsync(property);
		}

		/// <summary>
		/// Copy the table to the clipboard.
		/// </summary>
		/// <returns>Created task.</returns>
		protected async Task CopyToClipboard()
		{
			if(Clipboard == null)
				return;

			try
			{
				CanCopyToClipboard = false;

				await JSRuntime.InvokeVoidAsync("copyTable");
			}
			catch
			{
				// Don't crash if we can't copy to the clipboard.
			}
			finally
			{ 
				CanCopyToClipboard = true;
			}
		}

		/// <summary>
		/// Copy only selected items from the table to the clipboard.
		/// </summary>
		/// <returns>Created task.</returns>
		protected async Task CopySelectionToClipboard()
		{
			if(Clipboard == null)
				return;

			try
			{
				CanCopyToClipboard = false;

				await JSRuntime.InvokeVoidAsync("copySelectedRows");
			}
			catch
			{
				// Don't crash if we can't copy to the clipboard.
			}
			finally
			{ 
				CanCopyToClipboard = true;
			}
		}

		#endregion

		#region Properties
		/// <summary>
		/// Capture undefined attributes and pass them on to the <see cref="Table"/>.
		/// </summary>
		[Parameter(CaptureUnmatchedValues = true)]
		public Dictionary<string, object> InputAttributes
		{
			get; 
			set;
		} = new Dictionary<string, object>();

		/// <summary>
		/// Delegate used to retrieve a filtered set of results.
		/// </summary>
		[Parameter, EditorRequired]
		public FilterExpressionsDelegate? GetData
		{
			get; 
			set;
		}

		/// <summary>
		/// Indicates if the CopyToClipboard button is disabled.
		/// </summary>
		protected bool CanCopyToClipboard
		{
			get; 
			set;
		}

		/// <summary>
		/// Public properties to display, for type <see cref="T"/>.
		/// </summary>
		public PropertyInfo[] Properties
		{
			get;
			protected set;
		}

		/// <summary>
		/// Filter operators for each DataFilter.
		/// </summary>
		public Dictionary<string, FilterOperators> DataFilterOperators
		{ 
			get;
			protected set;
		} = new Dictionary<string, FilterOperators>();

		/// <summary>
		/// Filter values for each DataFilter.
		/// </summary>
		public Dictionary<string, string?> DataFilterValues
		{ 
			get;
			protected set;
		} = new Dictionary<string, string?> ();

		/// <summary>
		/// Default filter operator values for each DataFilter.
		/// </summary>
		public Dictionary<string, FilterOperators> DefaultDataFilterOperators
		{
			get; 
		} = new Dictionary<string, FilterOperators>();

		/// <summary>
		/// Default filter values for each DataFilter.
		/// </summary>
		public Dictionary<string, string?> DefaultDataFilterValues
		{
			get; 
		} = new Dictionary<string, string?>();

		/// <summary>
		/// Items selected in the table.
		/// </summary>
		public ISet<T> SelectedItems
		{
			get; 
		} = new HashSet<T>();

		/// <summary>
		/// Error that occurred on the page.
		/// </summary>
		public Exception? Exception
		{
			get; 
			protected set;
		}

		/// <summary>
		/// Raises events when the <see cref="DataFilterOperator"/> property changes.
		/// </summary>
		[Parameter]
		public EventCallback<string> DataFilterOperatorChanged
		{ 
			get; 
			set; 
		}

		/// <summary>
		/// Raises events when the <see cref="DataFilterValues"/> property changes.
		/// </summary>
		[Parameter]
		public EventCallback<string> DataFilterValueChanged
		{ 
			get; 
			set; 
		}

		/// <summary>
		/// Table containing items.
		/// </summary>
		protected MudTable<T>? Table
		{ 
			get; 
			set;
		}

		/// <summary>
		/// Expression filters defined for each DataFilter column.
		/// </summary>
		protected virtual Dictionary<string, Expression<Func<T, bool>>> Filters
		{
			get; 
			set;
		} = new Dictionary<string, Expression<Func<T, bool>>>();

		/// <summary>
		/// Token used to cancel the previous query.
		/// </summary>
		protected CancellationTokenSource LoadDataCancellation
		{
			get; 
			set;
		}

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
		/// Used to copy cell contents to the clipboard.
		/// </summary>
		[Inject]
		protected ClipboardService? Clipboard
		{
			get; 
			set;
		}
		
		/// <summary>
		/// Used to call javascript.
		/// </summary>
		[Inject]
		protected IJSRuntime JSRuntime
		{
			get;
			set;
		}

		/// <summary>
		/// References to each DataFilter component.
		/// </summary>
		protected Dictionary<string, DataFilter<T>?> DataFilterReferences
		{ 
			get;
		} = new Dictionary<string, DataFilter<T>?>();

		/// <summary>
		/// Indicates if <see cref="OnAfterRenderAsync"/> has been called at least once.
		/// 
		/// When <see cref="OnAfterRenderAsync"/> has been called, the <see cref="Filters"/> have been updated and any changes made to a filter, 
		/// will cause the TableData to be reloaded, when a <see cref="FilterExpressionChanged"/> is called due to a filter expression being updated.
		/// </summary>
		protected bool InitialRenderingComplete
		{
			get;
			set;
		}

		/// <summary>
		/// Rows per page to display.
		/// </summary>
		public int RowsPerPage
		{
			get; 
			set;
		} = 50;
		#endregion
	}
}

