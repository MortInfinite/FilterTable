using FilterTypes;
using System.Linq.Expressions;
using System.Reflection;

namespace FilterDataAccess
{
	/// <summary>
	/// Provides a method used to generate an expression, matching a <see cref="FilterOperationValue"/>.
	/// </summary>
	/// <typeparam name="T">Type that is being filtered on, in the generated expression.</typeparam>
	public class ExpressionGenerator<T>
	{
		/// <summary>
		/// Create a new expression generator.
		/// </summary>
		public ExpressionGenerator()
		{
			// Populate the list of data types for each property.
			PropertyInfo[] properties = typeof(T).GetProperties();
			foreach(PropertyInfo property in properties)
				PropertyTypes.Add(property.Name, property.PropertyType);
		}

		#region Methods
		/// <summary>
		/// Generate a lambda expression, used to filter data of type T, in a Where() statement.
		/// </summary>
		/// <returns>Lambda expression, used to filter data of type T, in a Where() statement.</returns>
		/// <example>
		/// MyArrayOfValues.AsQueryable().Where(expression);
		/// </example>
		/// <exception cref="ArgumentException">Thrown if the <paramref name="filterOperation"/> contains invalid arguments.</exception>
		public virtual Expression<Func<T, bool>> GenerateFilterExpression(FilterOperationValue filterOperation)
		{ 
			// Determine if the specified property exists.
			bool exists = PropertyTypes.TryGetValue(filterOperation.Property, out Type? propertyType);
			if(!exists || propertyType == null)
				throw new ArgumentException($"The property \"{filterOperation.Property}\" wasn't found on the data type {typeof(T).Name}", nameof(filterOperation));
			/*
			// If no filter value is specified.
			if(string.IsNullOrEmpty(filterOperation.Value))
				return null;
			*/
			// Conver the filter value string into an array of typed values.
			object[]? parsedFilterValues = FilterValueParser.ParseFilterValues(propertyType, filterOperation.Operator, filterOperation.Value);

			// Check that at least one filter value is specified.
			if(parsedFilterValues == null || parsedFilterValues.Length < 1)
				throw new ArgumentException($"The property \"{filterOperation.Value}\" didn't contain a value.", nameof(filterOperation));

			// Define an input argument called "filteredProperty".
			// The name is used for debugging purposes, it does not have to match the name of the FilteredProperty property.
			ParameterExpression filterParameter	= Expression.Parameter(typeof(T), "filteredProperty");

			// Extract the value from the property with the name specified in the FilteredProperty.
			MemberExpression propertyExpression = Expression.Property(filterParameter, filterOperation.Property);

			// If more than one OR filter value should be applied.
			if(filterOperation.Operator == FilterOperators.Any || filterOperation.Operator == FilterOperators.NotAny)
			{
				List<BinaryExpression> filterExpressions = new List<BinaryExpression>();

				// Generate each filter value.
				for(int count=0; count<parsedFilterValues.Length; count++)
				{
					// Generate a filter part, containing the constant value, specified in the parsed filter value, 
					// and the operator used to filter.
					ConstantExpression	constantExpression	= Expression.Constant(parsedFilterValues[count]);
					BinaryExpression	filterExpression	= GetExpression(filterOperation.Operator, propertyExpression, constantExpression);

					filterExpressions.Add(filterExpression);
				}

				// To combine each filter part, start by the first filter part, so that the next filter parts can be added to the first filter part.
				Expression currentExpression = filterExpressions[0];

				// Combine the current expression with the next expression.
				for(int count = 1; count<parsedFilterValues.Length; count++)
					currentExpression = Expression.Or(currentExpression, filterExpressions[count]);

				// If the operator is a NotLike, invert the expression.
				if(filterOperation.Operator == FilterOperators.NotAny)
					currentExpression = Expression.Not(currentExpression);

				// Convert the expression to a lambda expression, specifying that "filteredProperty" will be used as an argument.
				Expression<Func<T, bool>> lambdaExpression = Expression.Lambda<Func<T, bool>>(currentExpression, filterParameter);

				return lambdaExpression;
			}
			else if(filterOperation.Operator == FilterOperators.Like || filterOperation.Operator == FilterOperators.NotLike)
			{
				// Like operator can only be used on string types.
				if(propertyType != typeof(string))
					throw new ArgumentException($"The filter operator \"{filterOperation.Operator}\" can only be used on string type properties and the \"{filterOperation.Property}\" property is of type \"{propertyType}\".", nameof(filterOperation));

				// Generate a filter, containing the constant value.
				ConstantExpression		constantExpression	= Expression.Constant(parsedFilterValues[0]);

				// Find the string.Contains method, that takes a single string argument.
				MethodInfo				containsMethod		= typeof(string).GetMethod("Contains", new Type[]{typeof(string)}) ?? throw new InvalidOperationException("Unable to retrieve \"Contains\" method from string.");

				// Call the string.Contains method on the property, giving the constant value as the second argument.
				Expression?	expression = Expression.Call(propertyExpression, containsMethod, constantExpression);

				// If the operator is a NotLike, invert the expression.
				if(filterOperation.Operator == FilterOperators.NotLike)
					expression = Expression.Not(expression);

				// Convert the expression to a lambda expression, specifying that "filteredProperty" will be used as an argument.
				Expression<Func<T, bool>> lambdaExpression = Expression.Lambda<Func<T, bool>>(expression, filterParameter);

				return lambdaExpression;
			}
			else
			{
				// Generate a filter, containing the constant value, specified in the first parsed filter value, 
				// and the operator used to filter.
				ConstantExpression	constantExpression	= Expression.Constant(parsedFilterValues[0], propertyType);
				BinaryExpression	filterExpression	= GetExpression(filterOperation.Operator, propertyExpression, constantExpression);
			
				// Convert the expression to a lambda expression, specifying that "filteredProperty" will be used as an argument.
				Expression<Func<T, bool>> lambdaExpression = Expression.Lambda<Func<T, bool>>(filterExpression, filterParameter);

				return lambdaExpression;
			}
		}
/*
		/// <summary>
		/// Convert the <paramref name="filterString"/> into an array of type <see cref="T"/>.
		/// </summary>
		/// <param name="filterString">String to parse.</param>
		/// <param name="filterOperator">
		/// Operator used to determine whether to comma separate the string into individual 
		/// parts (<see cref="FilterOperators.Any"/>) or to treat the filterString as a single phrase.</param>
		/// <returns>Array of parsed parts or null if no parts could be parsed.</returns>
		protected object[]? ParseFilterValues(FilterOperation filterOperation)
		{ 
			// Determine if the specified property exists.
			bool exists = PropertyTypes.TryGetValue(filterOperation.Property, out Type? propertyType);
			if(!exists || propertyType == null)
				return null;

			// If no filter value is specified.
			if(string.IsNullOrEmpty(filterOperation.Value))
				return null;

			// If the filter operator is an Any operator, parse the value string as a comma separated list of values.
			if(filterOperation.Operator == FilterOperators.Any || filterOperation.Operator == FilterOperators.NotAny)
			{
				// Split the filter string into individual parts.
				string[] parts = StringHelpers.Split(filterOperation.Value);

				// Parse each part of the string.
				List<object> results = new List<object>();
				foreach(string? part in parts)
				{
					// Parse the current part of the string.
					object? currentPart = StringHelpers.Parse(part, propertyType, null);
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

				// If a date time is prefixed with a minus, parse the value as a time span that is subtracted from the current date and time.
				if(propertyType.IsAssignableFrom(typeof(DateTime)) && filterOperation.Value.StartsWith("-"))
				{ 
					// Parse the string as a time span.
					result = StringHelpers.Parse(filterOperation.Value, typeof(TimeSpan), null);
					if(result == null)
						return null;

					// Subtract the parsed time span from the current time.
					return new object[]{DateTime.Now+(TimeSpan) result};
				}

				// Parse the string as a single value.
				result = StringHelpers.Parse(filterOperation.Value, propertyType, null);
				if(result == null)
					return null;

				return new object[]{result};
			}
		}
*/
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
		/// <summary>
		/// Types of each property.
		/// </summary>
		protected Dictionary<string, Type> PropertyTypes
		{
			get; 
		} = new Dictionary<string, Type>();
		#endregion
	}
}
