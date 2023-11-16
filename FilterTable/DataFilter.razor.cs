using System.Linq.Expressions;
using System.Reflection;
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
		/// Name of the property that the filter will apply to.
		/// </summary>
		/// <exception cref="ArgumentException">Thrown if specifying the name of a property that doesn't exist on type <see cref="T"/>.</exception>
		[Parameter, EditorRequired]
		public string FilteredProperty
		{
			get
			{
				return m_filteredProperty;
			}
			set
			{
				// Don't set the property to its current value.
				if(value == m_filteredProperty)
					return;

				var filteredPropertyType = GetFilteredPropertyType(value);
				if(filteredPropertyType == null)
					throw new ArgumentException($"The property \"{value}\" was not found on the type \"{typeof(T).FullName}\".");

				m_filteredProperty = value;

				FilteredPropertyType = filteredPropertyType;

				// The FilterOperation property changes, when this property does.
				FilterOperationChanged.InvokeAsync(FilterOperation);
			}
		}

		/// <summary>
		/// Defines which filter operation to perform on the data.
		/// 
		/// Filter operator defines whether to find a value equal to, greater than or containing the FilterValue.
		/// </summary>
		[Parameter]
		public FilterOperators Operator
		{
			get
			{
				return m_operator;
			}
			set
			{
				// Don't set the property to its current value.
				if(value == m_operator)
					return;

				m_operator = value;

				// Notify subscribers that the property changed.
				OperatorChanged.InvokeAsync(value);

				// The FilterOperation property changes, when this property does.
				FilterOperationChanged.InvokeAsync(FilterOperation);
			}
		}

		/// <summary>
		/// Raises events when the <see cref="Operator"/> property changes.
		/// </summary>
		[Parameter]
		public EventCallback<FilterOperators> OperatorChanged 
		{ 
			get; 
			set; 
		}

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
				if(string.IsNullOrEmpty(FilterString))
					return false;

				// Attempt to parse the filter string.
				object[]? parsedFilterValues = FilterValueParser.ParseFilterValues(FilteredPropertyType, Operator, FilterString);

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
				return string.IsNullOrEmpty(FilterString);
			}
		}

		/// <summary>
		/// Filter string, as it's entered by the user.
		/// </summary>
		[Parameter]
		public string? FilterString
		{
			get
			{
				return m_filterString;
			}
			set
			{
				// Don't set the property to its current value.
				if(value == m_filterString)
					return;

				m_filterString = value;

				// Notify subscribers that the value changed.
				FilterStringChanged.InvokeAsync(value);

				// The FilterOperation property changes, when this property does.
				FilterOperationChanged.InvokeAsync(FilterOperation);
			}
		}

		[Parameter]
		public EventCallback<string> FilterStringChanged { get; set; }

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

		/// <summary>
		/// Filter operation performed or null if no valid filter is specified.
		/// </summary>
		public FilterOperation? FilterOperation
		{
			get
			{
				// If the filter isn't valid or the filter string isn't specified, don't return a filter operation.
				if(!FilterValid || FilterString == null)
					return null;

				return new FilterOperation(FilteredProperty, Operator, FilterString);
			}
		}

		/// <summary>
		/// Raised to indicate that part of the filter has changed.
		/// 
		/// This event is used to notify the caller that a new query must be performed, using the updated filter operation.
		/// </summary>
		[Parameter]
		public EventCallback<FilterOperation?> FilterOperationChanged { get; set; }
		#endregion

		#region Fields
		/// <summary>
		/// Backing field for the <see cref="Operator"/> property.
		/// </summary>
		private FilterOperators m_operator = FilterOperators.Equals;

		/// <summary>
		/// Backing field for the <see cref="FilterString"/> property.
		/// </summary>
		private string? m_filterString = string.Empty;
/*
		/// <summary>
		/// Backing field for the <see cref="FilterValues"/> property.
		/// </summary>
		private object[]? m_filterValues;
*/
		/// <summary>
		/// Backing field for the <see cref="FilteredProperty"/> property.
		/// </summary>
		private string m_filteredProperty = null!;

		/// <summary>
		/// Backing field for the <see cref="FilteredPropertyType"/> property.
		/// </summary>
		private Type? m_filteredPropertyType;
/*
		/// <summary>
		/// Backing field for the <see cref="FilterExpression"/> property.
		/// </summary>
		private Expression<Func<T, bool>>? m_filterExpression;
*/
		#endregion
	}
}
