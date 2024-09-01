using MudBlazor;
using Microsoft.AspNetCore.Components;
using System.Reflection;
using Microsoft.JSInterop;
using FilterTypes;
using System.ComponentModel;

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

		/// <summary>
		/// Describes a changed filter operation.
		/// </summary>
		public struct FilterOperationChangedArgs
		{ 
			/// <summary>
			/// Create a new <see cref="FilterOperationChangedArgs"/>.
			/// </summary>
			/// <param name="filterOperation">Filter operation that changed.</param>
			/// <param name="changedAction">Action describing the kind of change that occurred on the filter operation.</param>
			/// <param name="propertyName">Name of the property that changed or null if action doesn't indicate a property change.</param>
			public FilterOperationChangedArgs(FilterOperation filterOperation, ChangedAction changedAction, string? propertyName)
			{ 
				FilterOperation = filterOperation;
				ChangedAction = changedAction;
				PropertyName = propertyName;
			}

			/// <summary>
			/// Filter operation that changed.
			/// </summary>
			public FilterOperation FilterOperation
			{
				get;
				set;
			}
			
			/// <summary>
			/// Action describing the kind of change that occurred on the filter operation.
			/// </summary>
			public ChangedAction ChangedAction
			{
				get;
				set;
			}
				
			/// <summary>
			/// Name of the property that changed or null if action doesn't indicate a property change.
			/// </summary>
			public string? PropertyName
			{
				get;
				set;
			}
		}

		/// <summary>
		/// Indicates in which way an element has changed.
		/// </summary>
		public enum ChangedAction
		{
			/// <summary>
			/// No change occurred.
			/// </summary>
			None = 0,

			/// <summary>
			/// The specified element was added to the collection.
			/// </summary>
			Added,

			/// <summary>
			/// The specified element was removed from the collection.
			/// </summary>
			Removed,

			/// <summary>
			/// The specified element was modified.
			/// </summary>
			Modified
		}
		#endregion

		#region Methods
		/// <summary>
		/// Reloads server data.
		/// </summary>
		/// <returns>Created task.</returns>
		public virtual async Task Reload()
		{ 
			if(Table == null)
				throw new InvalidOperationException($"Unable to reload server data, the {nameof(Table)} property hasn't been initialized yet.");

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

			// Create empty filters to match each property.
			CreateEmptyFilters();
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
				InitialRenderingComplete = true;
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
				FilterOperationValue[] filters = FilterOperationsProtected	.Where(currentFilter => !string.IsNullOrEmpty(currentFilter.Value))
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

				// Clear currently selected items, before returning new results.
				// This is neccessary because the hash values of the old items don't match the hash values of the newly loaded values.
				SelectedItems.Clear();
				LastSelectedItem = default(T);
			}

			return tableData;
		}

		/// <summary>
		/// Add a new filter to the specified property.
		/// </summary>
		/// <param name="propertyName">Name of the property to apply the filter to.</param>
		/// <param name="operator">Operator to apply to the property or null to use the default operator for that property.</param>
		/// <param name="value">Initial value to set for the added filter or null to use an empty string.</param>
		public virtual void AddFilter(string propertyName, FilterOperators? @operator=null, string? value=null)
		{ 
			FilterOperation filterOperation = new FilterOperation()
			{
				Property	= propertyName,
				Operator	= @operator ?? DefaultDataFilterOperators.GetValueOrDefault(propertyName, FilterOperators.Equals),
				Value		= value	?? string.Empty
			};

			// When the filter operation changes, reload the server data and ensure that an empty filter exists for each property.
			filterOperation.PropertyChanged += FilterOperation_PropertyChanged;

			FilterOperationsProtected.Insert(0, filterOperation);

			// Update the UI.
			StateHasChanged();

			// Notify subscribers about the filter operation that was added.
			FilterOperationChanged.InvokeAsync(new FilterOperationChangedArgs(filterOperation, ChangedAction.Added, null));

			// Reload the server data, now that filters changed.
			if(Table != null)
				_ = Table.ReloadServerData();
		}

		/// <summary>
		/// Remove an existing filter and unsubscribe from its change events.
		/// </summary>
		/// <param name="filterOperation">Filter to remove.</param>
		/// <returns>Returns true if the filter was removed or false if the filter did not exist in the list of <see cref="FilterOperationsProtected"/>.</returns>
		public virtual bool RemoveFilter(FilterOperation filterOperation)
		{ 
			bool exists = FilterOperationsProtected.Remove(filterOperation);
			if(!exists)
				return false;

			// Unsubscribe from the removed filter's change events.
			filterOperation.PropertyChanged -= FilterOperation_PropertyChanged;

			// Update the UI.
			StateHasChanged();

			// Notify subscribers about the filter operation that was removed.
			FilterOperationChanged.InvokeAsync(new FilterOperationChangedArgs(filterOperation, ChangedAction.Removed, null));

			// Reload the server data, now that filters changed.
			if(Table != null)
				_ = Table.ReloadServerData();

			return true;
		}

		/// <summary>
		/// Remove all filters, excep empty filters used to enter new values.
		/// </summary>
		public virtual void ClearFilters()
		{ 
			// Remove all non-empty filters.
			var nonEmptyFilters = FilterOperationsProtected.Where(filter => !string.IsNullOrEmpty(filter.Value)).ToArray();
			foreach(var nonEmptyFilter in nonEmptyFilters)
				RemoveFilter(nonEmptyFilter);
		}

		/// <summary>
		/// Clear the existing filters and add a new set of filters.
		/// </summary>
		/// <param name="filtersToAdd">Filters to add, after clearing the list of filters.</param>
		public virtual void ReplaceFilters(IEnumerable<FilterOperation> filtersToAdd)
		{ 
			// Remove all non-empty filters.
			var nonEmptyFilters = FilterOperationsProtected.Where(filter => !string.IsNullOrEmpty(filter.Value)).ToArray();
			foreach(var nonEmptyFilter in nonEmptyFilters)
				RemoveFilter(nonEmptyFilter);

			foreach(var filterToAdd in filtersToAdd)
				AddFilter(filterToAdd.Property, filterToAdd.Operator, filterToAdd.Value);
		}

		/// <summary>
		/// Create an empty <see cref="FilterOperation"/> for each property in <see cref="Properties"/>.
		/// </summary>
		protected virtual void CreateEmptyFilters()
		{ 
			// Remove existing empty filters, before adding new filters.
			foreach(KeyValuePair<string, FilterOperation> emptyFilter in EmptyFilters)
				emptyFilter.Value.PropertyChanged -= EmptyFilter_PropertyChanged;

			EmptyFilters.Clear();

			foreach(PropertyInfo property in Properties)
			{
				FilterOperators defaultOperator = DefaultDataFilterOperators.GetValueOrDefault(property.Name, FilterOperators.Equals);
				FilterOperation filterOperation = new FilterOperation(property.Name, defaultOperator, string.Empty);
				filterOperation.PropertyChanged += EmptyFilter_PropertyChanged;

				EmptyFilters.Add(property.Name, filterOperation);
			}
		}

		/// <summary>
		/// Add the clicked item to the list of selected items.
		/// </summary>
		/// <param name="args">Click event, containing the selected argument.</param>
		protected virtual async Task OnRowClick(TableRowClickEventArgs<T> args)
		{
			// Don't respond to the row being clicked, if text selection is enabled.
			if(TextSelectionEnabled)
				return;

			// If the shift key is pressed, select a range of items, instead of only selecting the clicked item.
			if(Table != null && LastSelectedItem != null && args.MouseEventArgs.ShiftKey)
			{
				List<T> filteredItems = Table.FilteredItems.ToList();
				int lastSelectedItemIndex = filteredItems.IndexOf(LastSelectedItem);
				int newlySelectedItemIndex = filteredItems.IndexOf(args.Item);

				// If an item was previously selected, such that it is possible to select a range of items.
				if(lastSelectedItemIndex >= 0 && newlySelectedItemIndex >= 0)
				{ 
					// Ensure that the lastSelectedItemIndex comes before the newlySelectedItemIndex.
					if(lastSelectedItemIndex > newlySelectedItemIndex)
					{
						int swapValue = lastSelectedItemIndex;
						lastSelectedItemIndex = newlySelectedItemIndex;
						newlySelectedItemIndex = swapValue;
					}

					// Select or unselect the range of items.
					for(int count=lastSelectedItemIndex; count<=newlySelectedItemIndex; count++)
					{
						if(SelectingRange)
							SelectedItems.Add(filteredItems[count]);
						else
							SelectedItems.Remove(filteredItems[count]);
					}
				}
			}
			else
			{
				// If the clicked item wasn't found in the list of SelectedItems, add it now. 
				// If the clicked item was found in the list of SelectedItems, remove it instead.
				bool addItem = !SelectedItems.Contains(args.Item);
				if(addItem)
					SelectedItems.Add(args.Item);
				else
					SelectedItems.Remove(args.Item);

				// Remember whether the user is starting to select a range, because the selected item was previously unselected,
				// or is deselecting a range, because the selected item was previously unselected.
				SelectingRange = addItem;
			}

			// Remember which item was just selected or deselected.
			// This is used to determine which range to select, when holding down Shift to perform a range selection.
			LastSelectedItem = args.Item;

			// Notify subscribers that the list of selected items has changed.
			await SelectedItemsChanged.InvokeAsync();
		}
		#endregion

		#region Event handlers
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

			// Notify subscribers about the filter operation that was modified.
			if(sender is FilterOperation filterOperation)
			{
				// If the filter is empty, remove the filter and don't notify subscribers about its property changing.
				// (Removing the filter will notify subscribers that the list of filters has changed)
				if(string.IsNullOrEmpty(filterOperation.Value))
				{
					RemoveFilter(filterOperation);
					return;
				}

				FilterOperationChanged.InvokeAsync(new FilterOperationChangedArgs(filterOperation, ChangedAction.Modified, e.PropertyName));
			}

			// Ask the data table to request new data to be loaded, by calling the LoadData method.
			if(Table != null)
				_ = Table.ReloadServerData();
		}

		/// <summary>
		/// Copy the empty filter value to a new FilterOperation, when its value changes to a non-empty value.
		/// </summary>
		/// <param name="sender">Empty <see cref="FilterOperation"/> that changed.</param>
		/// <param name="e">Name of the property that changed, in that <see cref="FilterOperation"/>.</param>
		protected virtual void EmptyFilter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// If the value of the filter operation changed.
			if(sender is FilterOperation filterOperation && e.PropertyName == nameof(FilterOperation.Value) && !string.IsNullOrEmpty(filterOperation.Value))
			{ 
				// Copy the empty filter to a new filter.
				AddFilter(filterOperation.Property, filterOperation.Operator, filterOperation.Value);

				// Clear the value of the empty filter, so it can be reused.
				filterOperation.Operator = DefaultDataFilterOperators.GetValueOrDefault(filterOperation.Property, FilterOperators.Equals);;
				filterOperation.Value = string.Empty;
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
		/// Raises events when a filter operation is added, removed or modified.
		/// </summary>
		[Parameter]
		public EventCallback<FilterOperationChangedArgs> FilterOperationChanged
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
		/// Raises events when the list of selected items has changed.
		/// </summary>
		[Parameter]
		public EventCallback SelectedItemsChanged
		{
			get;
			set;
		}

		/// <summary>
		/// Error that occurred on the page.
		/// </summary>
		public Exception? Exception
		{
			get; 
			protected set;
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
		/// Available page size options to display.
		/// </summary>
		[Parameter]
		public int[] PageSizeOptions
		{
			get
			{
				return m_pageSizeOptions;
			}
			set
			{
				// Don't set the property to its current value.
				if(value == m_pageSizeOptions)
					return;

				m_pageSizeOptions = value;

				// Notify subscribers that the property changed.
				PageSizeOptionsChanged.InvokeAsync(value);
			}
		}

		/// <summary>
		/// Property changed event for the <see cref="PageSizeOptions"/> property.
		/// </summary>
		[Parameter]
		public EventCallback<int[]> PageSizeOptionsChanged
		{
			get;
			set;
		}

		/// <summary>
		/// Determine if text selection is disabled for cells in the table.
		/// 
		/// When text selection is enabled, selecting a range of rows is more difficult.
		/// </summary>
		[Parameter]
		public bool TextSelectionEnabled
		{
			get
			{
				return m_textSelectionEnabled;
			}
			set
			{
				// Don't set the property to its current value.
				if(value == m_textSelectionEnabled)
					return;

				m_textSelectionEnabled = value;

				// Notify subscribers that the property changed.
				TextSelectionEnabledChanged.InvokeAsync(value);
			}
		}

		/// <summary>
		/// Property changed event for the <see cref="TextSelectionEnabled"/> property.
		/// </summary>
		[Parameter]
		public EventCallback<bool> TextSelectionEnabledChanged
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
		/// Read only list of filter operations defined for the added <see cref="DataFilter"/> columns.
		/// </summary>
		public virtual IList<FilterOperation> FilterOperations
		{
			get
			{
				return FilterOperationsProtected.AsReadOnly();
			}
		}

		/// <summary>
		/// Filter operations defined for the added DataFilter columns.
		/// </summary>
		protected virtual List<FilterOperation> FilterOperationsProtected
		{ 
			get;
		} = new List<FilterOperation>();

		/// <summary>
		/// Empty filter used by the data filters belonging to each Property.
		/// 
		/// The key identifies the name of the property.
		/// </summary>
		protected Dictionary<string, FilterOperation> EmptyFilters
		{ 
			get;
		} = new Dictionary<string, FilterOperation>();

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
		/// The key identifies which <see cref="FilterOperationsProtected"/> the DataFilter belongs to.
		/// </summary>
		protected Dictionary<FilterOperation, DataFilter<T>> DataFilters
		{ 
			get;
		} = new Dictionary<FilterOperation, DataFilter<T>>();

		/// <summary>
		/// Indicates if <see cref="OnAfterRenderAsync"/> has been called at least once.
		/// 
		/// When <see cref="OnAfterRenderAsync"/> has been called, the <see cref="FilterOperationsProtected"/> have been updated and any changes made to a filter, 
		/// will cause the TableData to be reloaded, when a <see cref="FilterExpressionChanged"/> is called due to a filter expression being updated.
		/// </summary>
		protected bool InitialRenderingComplete
		{
			get;
			set;
		}

		/// <summary>
		/// The item which was most recently selected, by clicking on its row.
		/// 
		/// This row is used to determine which range of items to select, if range selection is active.
		/// </summary>
		/// <see cref="OnRowClick"/>
		protected T? LastSelectedItem
		{
			get; 
			set;
		}

		/// <summary>
		/// Indicates whether the most recently clicked item was found in the list of <see cref="SelectedItems"/>, when it was clicked.
		/// 
		/// This is used to determine whether to select a range or or deselect a range of items, if the Shift key is pressed.
		/// </summary>
		/// <see cref="OnRowClick"/>
		protected bool SelectingRange
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

		/// <summary>
		/// Backing field for the <see cref="PageSizeOptions"/> property.
		/// </summary>
		private int[] m_pageSizeOptions = new int[]{50, 100, 250, 500, 1000};

		/// <summary>
		/// Backing field for the <see cref="TextSelectionEnabled"/> property.
		/// </summary>
		private bool m_textSelectionEnabled = false;
		#endregion
	}
}

