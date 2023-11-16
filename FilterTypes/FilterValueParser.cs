namespace FilterTypes
{
	/// <summary>
	/// Provides helper methods used to parse filter strings.
	/// </summary>
	public static class FilterValueParser
	{
		/// <summary>
		/// Convert the <paramref name="filterString"/> into an array of type <see cref="T"/>.
		/// </summary>
		/// <param name="propertyType">Type of property to parse the <paramref name="valueString"/> as.</param>
		/// <param name="filterOperator">
		/// Operator used to determine whether to comma separate the string into individual 
		/// parts (<see cref="FilterOperators.Any"/>) or to treat the filterString as a single phrase.</param>
		/// <param name="valueString">String to parse.</param>
		/// <returns>Array of parsed parts or null if no parts could be parsed.</returns>
		public static object[]? ParseFilterValues(Type propertyType, FilterOperators filterOperator, string? valueString)
		{ 
			// If no filter value is specified.
			if(string.IsNullOrEmpty(valueString))
				return null;

			// If the filter operator is an Any operator, parse the value string as a comma separated list of values.
			if(filterOperator == FilterOperators.Any || filterOperator == FilterOperators.NotAny)
			{
				// Split the filter string into individual parts.
				string[] parts = StringHelpers.Split(valueString);

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
				if(propertyType.IsAssignableFrom(typeof(DateTime)) && valueString.StartsWith("-"))
				{ 
					// Parse the string as a time span.
					result = StringHelpers.Parse(valueString, typeof(TimeSpan), null);
					if(result == null)
						return null;

					// Subtract the parsed time span from the current time.
					return new object[]{DateTime.Now+(TimeSpan) result};
				}

				// Parse the string as a single value.
				result = StringHelpers.Parse(valueString, propertyType, null);
				if(result == null)
					return null;

				return new object[]{result};
			}
		}
	}
}
