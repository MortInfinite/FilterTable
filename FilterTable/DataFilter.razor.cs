using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace FilterTable
{
	/// <summary>
	/// Describes a filter operation to apply to a collection and generates the <see cref="FilterExpression"/>
	/// needed to perform the filter operation on the LINQ expression.
	/// 
	/// Data entered by the user is bound to the <see cref="FilterString"/>. The user entered <see cref="FilterString"/>
	/// is then parsed, based on the data type of the <see cref="FilteredProperty"/> and is stored in as <see cref="FilterValues"/>.
	/// 
	/// The <see cref="FilteredProperty"/> defines which property to compare the <see cref="FilterValues"/> with, using the 
	/// filter <see cref="Operator"/>.
	/// </summary>
	/// <typeparam name="T">Type of object to filter.</typeparam>
	/// <see cref="https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/"/>
	/// <example>
	/// To perform the filter operation "MyCollection.Where(currentValue => currentValue.MyValue > 17), use the following DataFilter:
	/// <![CDATA[
	/// MyDataFilter.FilteredProperty = nameof(MyValue);
	/// MyDataFilter.Operator = FilterOperators.GreaterThan;
	/// MyDataFilter.FilterString = "17";
	/// 
	/// var myFilteredCollection = MyCollection.AsQueryable().Where(MyDataFilter.Expression);
	/// ]]>
	/// 
	/// When using the component to let the user select the <see cref="Operator"/> in an select control and enter 
	/// the <see cref="FilterString"/> as an input control, declare the following DataFilter:
	/// <![CDATA[
	/// <DataFilter T="MyDataType" FilteredProperty="MyValue" FilterExpressionChanged="OnExpressionChanged"/>
	/// ]]>
	/// </example>
	/// <remarks>
	/// When using the <see cref="Expression"/> to filter data in memory, string filter operations will be case sensitive.
	/// 
	/// When using the <see cref="Expression"/> to filter data using Entity Framework, the string filter operations will follow the rules 
	/// of the database engine they are used on. For SQL Server this most commonly means that string operations are case insensitive.
	/// </remarks>
	public partial class DataFilter<T>
	{
		#region Methods
		/// <summary>
		/// Convert the <paramref name="filterString"/> into an array of type <see cref="T"/>.
		/// </summary>
		/// <param name="filterString">String to parse.</param>
		/// <param name="filterOperator">
		/// Operator used to determine whether to comma separate the string into individual 
		/// parts (<see cref="FilterOperators.Any"/>) or to treat the filterString as a single phrase.</param>
		/// <returns>Array of parsed parts or null if no parts could be parsed.</returns>
		protected object[]? ParseFilterString(string? filterString, FilterOperators filterOperator)
		{ 
			if(string.IsNullOrEmpty(filterString))
				return null;
			if(FilteredPropertyType == null)
				return null;

			if(filterOperator == FilterOperators.Any || filterOperator == FilterOperators.NotAny)
			{
				// Split the filter string into individual parts.
				string[] parts = StringHelpers.Split(filterString);

				// Parse each part of the string.
				List<object> results = new List<object>();
				foreach(string? part in parts)
				{
					// Parse the current part of the string.
					object? currentPart = StringHelpers.Parse(part, FilteredPropertyType, null);
					if(currentPart == null)
						continue;

					results.Add(currentPart);
				}
				
				if(results.Count == 0)
					return null;

				return results.ToArray();
			}
			else
			{
				object? result;

				// If a date time is prefixed with a minus.
				if(FilteredPropertyType.IsAssignableFrom(typeof(DateTime)) && filterString.StartsWith("-"))
				{ 
					// Parse the string as a time span.
					result = StringHelpers.Parse(filterString, typeof(TimeSpan), null);
					if(result == null)
						return null;

					// Subtract the parsed time span from the current time.
					return new object[]{DateTime.Now+(TimeSpan) result};
				}

				// Parse the string.
				result = StringHelpers.Parse(filterString, FilteredPropertyType, null);
				if(result == null)
					return null;

				return new object[]{result};
			}
		}

		/// <summary>
		/// Updates the <see cref="FilteredPropertyType"/> to match the property described in the <see cref="FilteredProperty"/>.
		/// </summary>
		protected virtual Type? GetFilteredPropertyType(string propertyName)
		{
			if(string.IsNullOrEmpty(propertyName))
				return null;

			return typeof(T).GetProperty(propertyName)?.PropertyType;
		}

		/// <summary>
		/// Generate a lambda expression, used to filter data of type T, in a Where() statement.
		/// </summary>
		/// <returns>Lambda expression, used to filter data of type T, in a Where() statement.</returns>
		/// <example>
		/// MyArrayOfValues.AsQueryable().Where(expression);
		/// </example>
		protected virtual Expression<Func<T, bool>>? GenerateFilterExpression()
		{ 
			// Check that a property type is specified.
			if(FilteredPropertyType == null)
				return null;

			// Check that at least one filter value is specified.
			if(FilterValues == null || FilterValues.Length < 1)
				return null;

			// Define an input argument called "filteredProperty".
			// The name is used for debugging purposes, it does not have to match the name of the FilteredProperty property.
			ParameterExpression filterParameter	= Expression.Parameter(typeof(T), "filteredProperty");

			// Extract the value from the property with the name specified in the FilteredProperty.
			MemberExpression propertyExpression = Expression.Property(filterParameter, FilteredProperty);

			// If more than one OR filter value should be applied.
			if(Operator == FilterOperators.Any || Operator == FilterOperators.NotAny)
			{
				List<BinaryExpression> filterExpressions = new List<BinaryExpression>();

				// Generate each filter value.
				for(int count = 0; count<FilterValues.Length; count++)
				{
					// Generate a filter part, containing the constant value, specified in the FilterValues, 
					// and the operator used to filter.
					ConstantExpression	constantExpression	= Expression.Constant(FilterValues[count]);
					BinaryExpression	filterExpression	= GetExpression(Operator, propertyExpression, constantExpression);

					filterExpressions.Add(filterExpression);
				}

				// To combine each filter part, start by the first filter part, so that the next filter parts can be added to the first filter part.
				Expression currentExpression = filterExpressions[0];

				// Combine the current expression with the next expression.
				for(int count = 1; count<FilterValues.Length; count++)
					currentExpression = Expression.Or(currentExpression, filterExpressions[count]);

				// If the operator is a NotLike, invert the expression.
				if(Operator == FilterOperators.NotAny)
					currentExpression = Expression.Not(currentExpression);

				// Convert the expression to a lambda expression, specifying that "filteredProperty" will be used as an argument.
				Expression<Func<T, bool>> lambdaExpression = Expression.Lambda<Func<T, bool>>(currentExpression, filterParameter);

				return lambdaExpression;
			}
			else if(Operator == FilterOperators.Like || Operator == FilterOperators.NotLike)
			{
				// Like operator can only be used on string types.
				if(FilteredPropertyType != typeof(string))
					return null;

				// Generate a filter, containing the constant value.
				ConstantExpression		constantExpression	= Expression.Constant(FilterValues[0]);

				// Find the string.Contains method, that takes a single string argument.
				MethodInfo				containsMethod		= typeof(string).GetMethod("Contains", new Type[]{typeof(string)}) ?? throw new InvalidOperationException("Unable to retrieve \"Contains\" method from string.");

				// Call the string.Contains method on the property, giving the constant value as the second argument.
				Expression?	expression = Expression.Call(propertyExpression, containsMethod, constantExpression);

				// If the operator is a NotLike, invert the expression.
				if(Operator == FilterOperators.NotLike)
					expression = Expression.Not(expression);

				// Convert the expression to a lambda expression, specifying that "filteredProperty" will be used as an argument.
				Expression<Func<T, bool>> lambdaExpression = Expression.Lambda<Func<T, bool>>(expression, filterParameter);

				return lambdaExpression;
			}
			else
			{
				// Generate a filter, containing the constant value, specified in the first FilterValues, 
				// and the operator used to filter.
				ConstantExpression	constantExpression	= Expression.Constant(FilterValues[0], FilteredPropertyType);
				BinaryExpression	filterExpression	= GetExpression(Operator, propertyExpression, constantExpression);
			
				// Convert the expression to a lambda expression, specifying that "filteredProperty" will be used as an argument.
				Expression<Func<T, bool>> lambdaExpression = Expression.Lambda<Func<T, bool>>(filterExpression, filterParameter);

				return lambdaExpression;
			}
		}

		/// <summary>
		/// Convert the specified <paramref name="filterOperator"/> to a <see cref="BinaryExpression"/>, 
		/// such that it can be used to generate a LINQ expression.
		/// </summary>
		/// <param name="filterOperator">FilterOperator to convert to an Expression.</param>
		/// <param name="left">The value or expression on the left side of the operator.</param>
		/// <param name="right">The value or expression on the right side of the operator.</param>
		/// <returns><see cref="BinaryExpression"/> that can be used as part of a LINQ expression.</returns>
		protected virtual BinaryExpression GetExpression(FilterOperators filterOperator, Expression left, Expression right)
		{
			switch(filterOperator)
			{ 
				case FilterOperators.Equals:				return Expression.Equal					(left, right);
				case FilterOperators.NotEquals:				return Expression.NotEqual				(left, right);
				case FilterOperators.Any:					return Expression.Equal					(left, right);
				case FilterOperators.GreaterThan:			return Expression.GreaterThan			(left, right);
				case FilterOperators.LessThan:				return Expression.LessThan				(left, right);
				case FilterOperators.GreaterThanOrEqual:	return Expression.GreaterThanOrEqual	(left, right);
				case FilterOperators.LessThanOrEqual:		return Expression.LessThanOrEqual		(left, right);
				default:									return Expression.Equal					(left, right);
			}
		}
		#endregion

		#region Properties
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

				// Parse the string and convert it into an array of parts.
				// This also updates the FilterExpression property.
				FilterValues = ParseFilterString(FilterString, Operator);

				// Indicate if the specified filter is valid.
				FilterValid = string.IsNullOrEmpty(FilterString) || FilterValues != null;
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
		/// Values to filter by. These values are parsed from the user entered <see cref="FilterString"/>.
		/// 
		/// When <see cref="Operator"/> equals any value other than <see cref="FilterOperators.Any"/>, this property can contain only a single value.
		/// When <see cref="Operator"/> equals <see cref="FilterOperators.Any"/>, this property can multiple values.
		/// </summary>
		public object[]? FilterValues
		{
			get
			{
				return m_filterValues;
			}
			protected set
			{
				// Don't set the property to its current value.
				if(object.Equals(value, m_filterValues))
					return;
				
				m_filterValues = value;

				// Notify subscribers that the property changed.
				FilterValuesChanged.InvokeAsync(value);

				// Generate a filter expression matching the selected filter criteria.
				FilterExpression = GenerateFilterExpression();
			}
		}

		/// <summary>
		/// Raises events whenever the <see cref="FilterValues"/> property changes.
		/// </summary>
		[Parameter]
		public EventCallback<object[]> FilterValuesChanged 
		{ 
			get; 
			set; 
		}

		/// <summary>
		/// Filter expression used to filter a Queryable<typeparamref name="T"/>, in a LINQ statement.
		/// </summary>
		public Expression<Func<T, bool>>? FilterExpression
		{
			get
			{
				return m_filterExpression;
			}
			protected set
			{
				// Don't set the property to its current value.
				if(value == m_filterExpression)
					return;

				m_filterExpression = value;

				// Notify subscribers that the property changed.
				FilterExpressionChanged.InvokeAsync(value);
			}
		}

		/// <summary>
		/// Raises events when the <see cref="FilterExpression"/> property changes.
		/// </summary>
		[Parameter]
		public EventCallback<Expression<Func<T, bool>>?> FilterExpressionChanged
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
			get; 
			protected set;
		} = true;

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

				FilterValues = ParseFilterString(FilterString, Operator);

				// Indicate if the specified filter is valid.
				FilterValid = string.IsNullOrEmpty(FilterString) || FilterValues != null;

				// Notify subscribers that the value changed.
				FilterStringChanged.InvokeAsync(value);
			}
		}

		[Parameter]
		public EventCallback<string> FilterStringChanged { get; set; }  

		/// <summary>
		/// Data type of the <see cref="FilteredProperty"/>.
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
		/// Backing field for the <see cref="Operator"/> property.
		/// </summary>
		private FilterOperators m_operator = FilterOperators.Equals;

		/// <summary>
		/// Backing field for the <see cref="FilterString"/> property.
		/// </summary>
		private string? m_filterString = string.Empty;

		/// <summary>
		/// Backing field for the <see cref="FilterValues"/> property.
		/// </summary>
		private object[]? m_filterValues;

		/// <summary>
		/// Backing field for the <see cref="FilteredProperty"/> property.
		/// </summary>
		private string m_filteredProperty = null!;

		/// <summary>
		/// Backing field for the <see cref="FilteredPropertyType"/> property.
		/// </summary>
		private Type? m_filteredPropertyType;

		/// <summary>
		/// Backing field for the <see cref="FilterExpression"/> property.
		/// </summary>
		private Expression<Func<T, bool>>? m_filterExpression;
		#endregion
	}

	/// <summary>
	/// Filter operator determining which operation will be applied to filter a collection of items.
	/// </summary>
	public enum FilterOperators
	{ 
		Equals = 0,
		NotEquals,
		Like,
		NotLike,
		Any,
		NotAny,
		GreaterThan,
		LessThan,
		GreaterThanOrEqual,
		LessThanOrEqual
	}
}
