using FilterTypes;
using Microsoft.AspNetCore.Components;

namespace FilterTable
{
	/// <summary>
	/// Describes a filter operation to apply to a collection.
	/// 
	/// Data entered by the user is bound to the <see cref="FilterString"/>. 
	/// 
	/// The <see cref="FilteredProperty"/> defines which property to compare the <see cref="FilterString"/> with, using the 
	/// filter <see cref="Operator"/>.
	/// </summary>
	/// <typeparam name="T">Type of object to filter.</typeparam>
	/// <see cref="https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/"/>
	public partial class DataFilter<T>
	{
		/// <summary>
		/// Updates the <see cref="FilteredPropertyType"/> to match the property described in the <see cref="FilteredProperty"/>.
		/// </summary>
		protected virtual Type? GetFilteredPropertyType(string propertyName)
		{
			if(string.IsNullOrEmpty(propertyName))
				return null;

			return typeof(T).GetProperty(propertyName)?.PropertyType;
		}

		#region Properties
		/// <summary>
		/// HTML attributes to pass on to the contained HTML element.
		/// </summary>
		[Parameter(CaptureUnmatchedValues = true)]
		public Dictionary<string, object> InputAttributes
		{
			get; 
			set;
		} = new Dictionary<string, object>();
		/// <summary>
		/// Filter operation performed or null if no valid filter is specified.
		/// </summary>
		[Parameter, EditorRequired]
		public FilterOperation FilterOperation
		{
			get
			{
				return m_filterOperation;
			}
			set
			{
				// Don't set the property to its current value.
				if(value == m_filterOperation)
					return;

				// Don't accept null values for filter operations.
				if(value == null)
					throw new ArgumentNullException(nameof(FilterOperation));

				if(m_filterOperation != null)
					m_filterOperation.PropertyChanged -= FilterOperation_PropertyChanged;

				// Retrieve the type of the specified property (Or null if the type isn't found on type T).
				FilteredPropertyType = GetFilteredPropertyType(value.Property);

				m_filterOperation = value;

				m_filterOperation.PropertyChanged += FilterOperation_PropertyChanged;

				// Notify subscribers that the property changed.
				FilterOperationChanged.InvokeAsync(value);
			}
		}

		/// <summary>
		/// Update the <see cref="FilteredPropertyType"/> property, if the <see cref="FilterOperation.Property"/> changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void FilterOperation_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch(e.PropertyName)
			{
				case nameof(FilterOperation.Property):
				{ 
					// Retrieve the type of the specified property (Or null if the type isn't found on type T).
					FilteredPropertyType = GetFilteredPropertyType(FilterOperation.Property);
				}
				break;
			}
		}

		/// <summary>
		/// Property changed event for the <see cref="FilterOperation"/> property.
		/// </summary>
		[Parameter]
		public EventCallback<FilterOperation> FilterOperationChanged
		{
			get;
			set;
		}

		/// <summary>
		/// Backing field for the <see cref="FilterOperation"/> property.
		/// </summary>
		private FilterOperation m_filterOperation = new FilterOperation();

		/// <summary>
		/// List of operators supported by the <see cref="FilteredProperty"/>.
		/// </summary>
		public FilterOperators[] Operators
		{
			get
			{
				if(FilteredPropertyType == typeof(string))
					return new FilterOperators[]{FilterOperators.Equals, FilterOperators.NotEquals, FilterOperators.Like, FilterOperators.NotLike, FilterOperators.Any, FilterOperators.NotAny, FilterOperators.GreaterThan, FilterOperators.LessThan, FilterOperators.GreaterThanOrEqual, FilterOperators.LessThanOrEqual};

				return new FilterOperators[]{FilterOperators.Equals, FilterOperators.NotEquals, FilterOperators.Any, FilterOperators.NotAny, FilterOperators.GreaterThan, FilterOperators.LessThan, FilterOperators.GreaterThanOrEqual, FilterOperators.LessThanOrEqual};
			}
		}

		/// <summary>
		/// Indicates if the entered filter is valid.
		/// </summary>
		public bool FilterValid
		{
			get
			{ 
				// If the property type isn't specified, we can't verify that the filter string is correct.
				if(FilteredPropertyType == null)
					return false;

				// If no filter is specified.
				if(string.IsNullOrEmpty(FilterOperation.Value))
					return false;

				// Attempt to parse the filter string.
				object[]? parsedFilterValues = FilterValueParser.ParseFilterValues(FilteredPropertyType, FilterOperation.Operator, FilterOperation.Value);

				// If the filter couldn't be parsed.
				if(parsedFilterValues == null)
					return false;

				// If the filter was parsed, but didn't contain any values.
				if(parsedFilterValues.Length < 1)
					return false;

				return true;
			}
		}

		/// <summary>
		/// Indicates if the filter is empty and should be disregarded.
		/// </summary>
		public bool FilterEmpty
		{
			get
			{ 
				return string.IsNullOrEmpty(FilterOperation.Value);
			}
		}

		/// <summary>
		/// Data type of the <see cref="FilteredProperty"/>.
		/// 
		/// The specified data type determines which <see cref="Operators"/> can be selected.
		/// </summary>
		protected Type? FilteredPropertyType
		{
			get
			{
				return m_filteredPropertyType;
			}
			set
			{
				// Don't set the property to its current value.
				if(value == m_filteredPropertyType)
					return;

				m_filteredPropertyType = value;
			}
		}
		#endregion

		#region Fields
		/// <summary>
		/// Backing field for the <see cref="FilteredPropertyType"/> property.
		/// </summary>
		private Type? m_filteredPropertyType;
		#endregion
	}
}
