using MudBlazor;
using Microsoft.AspNetCore.Components;
using System.Reflection;
using Microsoft.JSInterop;
using FilterTypes;
using System.Linq;

namespace FilterTable
{
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
		public delegate Task<FilteredResult> FilterExpressionsDelegate(FilterOperationValue[] filters, string sortLabel, bool sortAscending, int skip, int maxCount, CancellationToken cancellationToken);

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

		/// <summary>
		/// Delegate used to Retrieve the value of the specified <paramref name="property"/>, from the <paramref name="context"/>.
		/// </summary>
		/// <param name="context">Object from which to retrieve the value of the <paramref name="property"/>.</param>
		/// <param name="property">Property to retrieve from the <paramref name="context"/>.</param>
		/// <returns>Value of the property.</returns>
		public delegate object? GetPropertyValueDelegate(object context, PropertyInfo property);

		/// <summary>
		/// Delegate used to retrieve the column name of the specified property.
		/// </summary>
		/// <param name="property">Property for which to retrieve the column name.</param>
		/// <returns>Name of the column.</returns>
		public delegate string GetColumnNameDelegate(PropertyInfo property);
		#endregion

		#region Methods
		/// <summary>
		/// Reloads server data.
		/// </summary>
		/// <returns>Created task.</returns>
		public virtual async Task Reload()
		{ 
			if(Table == null)
				return;

			await Table.ReloadServerData();
		}

		/// <summary>
		/// Determine which class should be applied to the row.
		/// </summary>
		/// <param name="element">Element contained in the row.</param>
		/// <param name="rowNumber">Row number containing the element.</param>
		/// <returns>Class to add to the row.</returns>
		public virtual string GetRowClassDefault(T element, int rowNumber)
		{
			if(SelectedItems.Contains(element))
				return "selected";

			return string.Empty;
		}

		/// <summary>
		/// Retrieve the value of the specified <paramref name="property"/>, from the <paramref name="context"/>.
		/// </summary>
		/// <param name="context">Object from which to retrieve the value of the <paramref name="property"/>.</param>
		/// <param name="property">Property to retrieve from the <paramref name="context"/>.</param>
		/// <returns>Value of the property.</returns>
		public virtual object? GetPropertyValueDefault(object context, PropertyInfo property)
		{ 
			object? value = property.GetValue(context);
			return value;
		}

		/// <summary>
		/// Returns the name of the <see cref="property"/>.
		/// </summary>
		/// <param name="property">Property for which to return the name.</param>
		/// <returns>Name of the property.</returns>
		public virtual string GetColumnNameDefault(PropertyInfo property)
		{ 
			return property?.Name ?? string.Empty;
		}

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

			// Add a new empty data filter at the end of non-empty filters.
			// Remove any empty filters, except for the last one.
			foreach(var property in Properties)
				AddFilter(property.Name, DefaultDataFilterValues.GetValueOrDefault(property.Name, string.Empty)!);
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
		/// Updates the expression filters defined for each DataFilter column.
		/// </summary>
		protected virtual void UpdateFilters()
		{ 
		}

		/// <summary>
		/// Load data from the database.
		/// </summary>
		/// <param name="state">Table view state, containing sort order.</param>
		/// <returns>Table data containing matching log entries.</returns>
		protected virtual async Task<TableData<T>> LoadData(TableState state)
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

				// Retrieve those of the filters that have a filter value.
				FilterOperationValue[] filters = FilterOperations	.Where(currentFilter => !string.IsNullOrEmpty(currentFilter.Value))
													.Select(currentFilter => (FilterOperationValue) currentFilter)
													.ToArray();

				DataTable<T>.FilteredResult filteredResult = await GetData(filters, state.SortLabel, state.SortDirection == SortDirection.Ascending, state.Page * state.PageSize, state.PageSize, default);

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
		/// Add a new filter to the specified property.
		/// </summary>
		/// <param name="propertyName">Name of the property to apply the filter to.</param>
		/// <param name="value">Initial value to set for the added filter.</param>
		protected virtual void AddFilter(string propertyName, string? value=null)
		{ 
			FilterOperation filterOperation = new FilterOperation()
			{
				Property	= propertyName,
				Operator	= DefaultDataFilterOperators.GetValueOrDefault(propertyName, FilterOperators.Equals),
				Value		= value	?? string.Empty
			};

			// When the filter operation changes, reload the server data and ensure that an empty filter exists for each property.
			filterOperation.PropertyChanged += FilterOperation_PropertyChanged;

			//Filters.Add(filterOperation);
			FilterOperations.Insert(0, filterOperation);

			// Update the UI.
			StateHasChanged();
		}

		/// <summary>
		/// Remove an existing filter and unsubscribe from its change events.
		/// </summary>
		/// <param name="filterOperation">Filter to remove.</param>
		/// <returns>Returns true if the filter was removed or false if the filter did not exist in the list of <see cref="FilterOperations"/>.</returns>
		protected virtual bool RemoveFilter(FilterOperation filterOperation)
		{ 
			bool exists = FilterOperations.Remove(filterOperation);
			if(!exists)
				return false;

			// Unsubscribe from the removed filter's change events.
			filterOperation.PropertyChanged -= FilterOperation_PropertyChanged;

			return true;
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
		#endregion

		#region Event handlers

		/// <summary>
		/// Ensure that exactly one empty filter exists, per property.
		/// </summary>
		protected virtual void EnsureSingleEmptyFilterPerProperty()
		{ 
			foreach(PropertyInfo property in Properties)
			{
				string propertyName = property.Name;
				propertyName .ToString();

				FilterOperation[] filtersMatchingProperty = FilterOperations.Where(filter => filter.Property == property.Name).ToArray();
				FilterOperation[] emptyFilters = filtersMatchingProperty.Where(filter => string.IsNullOrEmpty(filter.Value)).ToArray();

				emptyFilters.ToString();

				if(emptyFilters.Length < 1)
				{
					// If no empty filters exist, add one now.
					AddFilter(property.Name);
				}
				else if(emptyFilters.Length > 1)
				{
					// Remove all empty filters, except the last one.
					IEnumerable<FilterOperation> filtersToRemove = emptyFilters.TakeLast(emptyFilters.Length-1).ToArray();
					foreach(FilterOperation filterToRemove in filtersToRemove)
						RemoveFilter(filterToRemove);
				}
			}
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

		/// <summary>
		/// Ensure an empty data filter exists for each property and reload the server data, now that values changed.
		/// </summary>
		/// <param name="sender">Not used.</param>
		/// <param name="e">Not used.</param>
		protected virtual void FilterOperation_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if(!InitialRenderingComplete)
				return;

			Task.Run(async ()=>
			{
				// Add a new empty data filter at the end of non-empty filters.
				// Remove any empty filters, except for the last one.
				EnsureSingleEmptyFilterPerProperty();

				// Ask the data table to request new data to be loaded, by calling the LoadData method.
				if(Table != null)
					await Table.ReloadServerData();
			});
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
		/// Name of the property to sort by.
		/// </summary>
		[Parameter]
		public string? SortProperty
		{
			get
			{
				return m_sortProperty;
			}
			set
			{
				// Don't set the property to its current value.
				if(value == m_sortProperty)
					return;

				m_sortProperty = value;

				// Notify subscribers that the property changed.
				SortPropertyChanged.InvokeAsync(value);
			}
		}

		/// <summary>
		/// Property changed event for the <see cref="SortProperty"/> property.
		/// </summary>
		[Parameter]
		public EventCallback<string?> SortPropertyChanged
		{
			get;
			set;
		}

		/// <summary>
		/// Sort direction applied to the <see cref="SortProperty"/>.
		/// </summary>
		[Parameter]
		public SortDirection SortDirection
		{
			get
			{
				return m_sortDirection;
			}
			set
			{
				// Don't set the property to its current value.
				if(value == m_sortDirection)
					return;

				m_sortDirection = value;

				// Notify subscribers that the property changed.
				SortDirectionChanged.InvokeAsync(value);
			}
		}

		/// <summary>
		/// Property changed event for the <see cref="SortDirection"/> property.
		/// </summary>
		[Parameter]
		public EventCallback<SortDirection> SortDirectionChanged
		{
			get;
			set;
		}

		/// <summary>
		/// Implementation used to retrieve the display name for the property.
		/// 
		/// If this value is set to null, the <see cref="GetGetColumnNameDefault"/> will be used.
		/// </summary>
		[Parameter]
		public GetColumnNameDelegate GetColumnName
		{
			get
			{
				if(m_getColumnName == null)
					return GetColumnNameDefault;

				return m_getColumnName;
			}
			set
			{
				// Don't set the property to its current value.
				if(value == m_getColumnName)
					return;

				m_getColumnName = value;

				// Notify subscribers that the property changed.
				GetColumnNameChanged.InvokeAsync(value);
			}
		}

		/// <summary>
		/// Property changed event for the <see cref="GetColumnName"/> property.
		/// </summary>
		[Parameter]
		public EventCallback<GetColumnNameDelegate> GetColumnNameChanged
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
		/// Implementation used to retrieve the value of a field in a row.
		/// 
		/// If this value is set to null, the <see cref="GetPropertyValueDefault"/> will be used.
		/// </summary>
		[Parameter]
		public GetPropertyValueDelegate GetPropertyValue
		{
			get
			{
				if(m_getPropertyValue == null)
					return GetPropertyValueDefault;

				return m_getPropertyValue;
			}
			set
			{
				// Don't set the property to its current value.
				if(value == m_getPropertyValue)
					return;

				m_getPropertyValue = value;

				// Notify subscribers that the property changed.
				GetPropertyValueChanged.InvokeAsync(value);
			}
		}

		/// <summary>
		/// Property changed event for the <see cref="GetPropertyValue"/> property.
		/// </summary>
		[Parameter]
		public EventCallback<GetPropertyValueDelegate> GetPropertyValueChanged
		{
			get;
			set;
		}

		/// <summary>
		/// Implementation used to retrieve the row style that should be applied to a row.
		/// 
		/// If this value is set to null, the <see cref="GetRowStyleDefault"/> will be used.
		/// </summary>
		[Parameter]
		public Func<T, int, string> GetRowClass
		{
			get
			{
				if(m_getRowClass == null)
					return GetRowClassDefault;

				return m_getRowClass;
			}
			set
			{
				// Don't set the property to its current value.
				if(value == m_getRowClass)
					return;

				m_getRowClass = value;

				// Notify subscribers that the property changed.
				GetRowStyleChanged.InvokeAsync(value);
			}
		}

		/// <summary>
		/// Property changed event for the <see cref="GetRowStyle"/> property.
		/// </summary>
		[Parameter]
		public EventCallback<Func<T, int, string>> GetRowStyleChanged
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
		/// Table containing log entries.
		/// </summary>
		protected MudTable<T>? Table
		{ 
			get; 
			set;
		}

		/// <summary>
		/// Filter operations defined for the added DataFilter columns.
		/// </summary>
		public virtual List<FilterOperation> FilterOperations
		{
			get; 
			set;
		} = new List<FilterOperation>();

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
		/// List of each DataFilter component.
		/// 
		/// The key identifies which <see cref="FilterOperations"/> the DataFilter belongs to.
		/// </summary>
		protected Dictionary<FilterOperation, DataFilter<T>> DataFilters
		{ 
			get;
		} = new Dictionary<FilterOperation, DataFilter<T>>();

		/// <summary>
		/// Indicates if <see cref="OnAfterRenderAsync"/> has been called at least once.
		/// 
		/// When <see cref="OnAfterRenderAsync"/> has been called, the <see cref="FilterOperations"/> have been updated and any changes made to a filter, 
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

		#region Fields
		/// <summary>
		/// Backing field for the <see cref="GetPropertyValue"/> property.
		/// </summary>
		private GetPropertyValueDelegate? m_getPropertyValue;

		/// <summary>
		/// Backing field for the <see cref="GetRowStyle"/> property.
		/// </summary>
		private Func<T, int, string> m_getRowClass;

		/// <summary>
		/// Backing field for the <see cref="SortProperty"/> property.
		/// </summary>
		private string? m_sortProperty;

		/// <summary>
		/// Backing field for the <see cref="SortDirection"/> property.
		/// </summary>
		private SortDirection m_sortDirection = SortDirection.Ascending;

		/// <summary>
		/// Backing field for the <see cref="GetColumnName"/> property.
		/// </summary>
		private GetColumnNameDelegate m_getColumnName;
		#endregion
	}
}

