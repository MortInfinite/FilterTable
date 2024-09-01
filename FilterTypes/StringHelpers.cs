using System.Text.RegularExpressions;

namespace FilterTypes
{
	/// <summary>
	/// Helper methods for parsing data types.
	/// </summary>
	public static class StringHelpers
	{
		/// <summary>
		/// Split the string into parts, based on the specified <paramref name="separator"/>.
		/// 
		/// Quoted strings are not split.
		/// </summary>
		/// <param name="inputString">Input string to parse.</param>
		/// <param name="separator">Separator to use to split the string.</param>
		/// <param name="trim">When true, trims whitespaces before and after each parsed expression.</param>
		/// <returns>Array of parts the string is split into.</returns>
		/// <remarks>
		/// Based on: https://github.com/TinyCsvParser/TinyCsvParser/issues/4
		/// </remarks>
		/// <seealso cref="https://stackoverflow.com/questions/3776458/split-a-comma-separated-string-with-both-quoted-and-unquoted-strings"/>
		public static string[] Split(string inputString, char separator=',', bool trim=true)
		{
			try
			{
				// Define an expression that splits the string, ensuring that quoted strings are not split.
				string			regExFormatString	= string.Format("((?<=\")[^\"]*(?=\"({0}|$)+)|(?<={0}|^)[^{0}\"]*(?={0}|$))", separator);
				Regex			regex				= new Regex(regExFormatString, RegexOptions.Compiled);
				MatchCollection matches				= regex.Matches(inputString);

				// Convert the matches to an array of results.
				string[]? result = matches.Select(currentMatch => trim ? currentMatch.Value.Trim() : currentMatch.Value).ToArray();
				return result;
			}
			catch
			{ 
				return new string[]{inputString};
			}
		}

		/// <summary>
		/// Parse the input string as a specified type.
		/// </summary>
		/// <typeparam name="T">Type to parse the string as.</typeparam>
		/// <param name="inputString">String to parse.</param>
		/// <param name="fallbackValue">Value to return if the <paramref name="inputString"/> couldn't be parsed as the specified type.</param>
		/// <returns></returns>
		public static T? Parse<T>(string inputString, T? fallbackValue = default)
		{ 
			return (T?) Parse(inputString, typeof(T), fallbackValue);
		}

		/// <summary>
		/// Parse the input string as a specified type.
		/// </summary>
		/// <param name="inputString">String to parse.</param>
		/// <param name="valueType">Type to parse the string as.</param>
		/// <param name="fallbackValue">Value to return if the <paramref name="inputString"/> couldn't be parsed as the specified type.</param>
		/// <returns>Parsed value or <paramref name="fallbackValue"/> if parsing is not possible.</returns>
		public static object? Parse(string inputString, Type valueType, object? fallbackValue = default)
		{ 
			if(valueType.IsAssignableFrom(typeof(string)))
			{
				return (object) inputString;
			}
			else if(valueType.IsAssignableFrom(typeof(int)))
			{
				if(int.TryParse(inputString, out int parseResult))
					return (object) parseResult;
			}
			else if(valueType.IsAssignableFrom(typeof(float)))
			{
				if(float.TryParse(inputString, out float parseResult))
					return (object) parseResult;
			}
			else if(valueType.IsAssignableFrom(typeof(double)))
			{
				if(double.TryParse(inputString, out double parseResult))
					return (object) parseResult;
			}
			else if(valueType.IsAssignableFrom(typeof(long)))
			{
				if(long.TryParse(inputString, out long parseResult))
					return (object) parseResult;
			}
			else if(valueType.IsAssignableFrom(typeof(short)))
			{
				if(short.TryParse(inputString, out short parseResult))
					return (object) parseResult;
			}
			else if(valueType.IsAssignableFrom(typeof(bool)))
			{
				if(bool.TryParse(inputString, out bool parseResult))
					return (object) parseResult;
			}
			else if(valueType.IsAssignableFrom(typeof(byte)))
			{
				if(byte.TryParse(inputString, out byte parseResult))
					return (object) parseResult;
			}
			else if(valueType.IsAssignableFrom(typeof(char)))
			{
				if(char.TryParse(inputString, out char parseResult))
					return (object) parseResult;
			}
			else if(valueType.IsAssignableFrom(typeof(decimal)))
			{
				if(decimal.TryParse(inputString, out decimal parseResult))
					return (object) parseResult;
			}
			else if(valueType.IsAssignableFrom(typeof(Guid)))
			{
				if(Guid.TryParse(inputString, out Guid parseResult))
					return (object) parseResult;
			}
			else if(valueType.IsAssignableFrom(typeof(uint)))
			{
				if(uint.TryParse(inputString, out uint parseResult))
					return (object) parseResult;
			}
			else if(valueType.IsAssignableFrom(typeof(DateTime)))
			{
				if(DateTime.TryParse(inputString, out DateTime parseResult))
					return (object) parseResult;
			}
			else if(valueType.IsAssignableFrom(typeof(TimeSpan)))
			{
				// Generate the format string -1.12:34:56 from -1 12:34:56
				if(TimeSpan.TryParse(inputString.Replace(" ", "."), out TimeSpan parseResult))
					return (object) parseResult;
			}
			else if(valueType.IsEnum)
			{ 
				if(Enum.TryParse(valueType, inputString, out object? parseResult) && parseResult != null)
					return (object) parseResult;
			}

			return fallbackValue;
		}

		/// <summary>
		/// Parse the input string as a specified type.
		/// </summary>
		/// <param name="inputString">String to parse.</param>
		/// <param name="valueType">Type to parse the string as.</param>
		/// <param name="result">Parsed value or null if the value could not be parsed.</param>
		/// <returns>Returns true if the value was successfully parsed.</returns>
		public static bool TryParse(string inputString, Type valueType, out object? result)
		{ 
			if(valueType.IsAssignableFrom(typeof(string)))
			{
				result = (object) inputString;
				return true;
			}
			else if(valueType.IsAssignableFrom(typeof(int)))
			{
				if(int.TryParse(inputString, out int parseResult))
				{
					result = (object) parseResult;
					return true;
				}
			}
			else if(valueType.IsAssignableFrom(typeof(float)))
			{
				if(float.TryParse(inputString, out float parseResult))
				{
					result = (object) parseResult;
					return true;
				}
			}
			else if(valueType.IsAssignableFrom(typeof(double)))
			{
				if(double.TryParse(inputString, out double parseResult))
				{
					result = (object) parseResult;
					return true;
				}
			}
			else if(valueType.IsAssignableFrom(typeof(long)))
			{
				if(long.TryParse(inputString, out long parseResult))
				{
					result = (object) parseResult;
					return true;
				}
			}
			else if(valueType.IsAssignableFrom(typeof(short)))
			{
				if(short.TryParse(inputString, out short parseResult))
				{
					result = (object) parseResult;
					return true;
				}
			}
			else if(valueType.IsAssignableFrom(typeof(bool)))
			{
				if(bool.TryParse(inputString, out bool parseResult))
				{
					result = (object) parseResult;
					return true;
				}
			}
			else if(valueType.IsAssignableFrom(typeof(byte)))
			{
				if(byte.TryParse(inputString, out byte parseResult))
				{
					result = (object) parseResult;
					return true;
				}
			}
			else if(valueType.IsAssignableFrom(typeof(char)))
			{
				if(char.TryParse(inputString, out char parseResult))
				{
					result = (object) parseResult;
					return true;
				}
			}
			else if(valueType.IsAssignableFrom(typeof(decimal)))
			{
				if(decimal.TryParse(inputString, out decimal parseResult))
				{
					result = (object) parseResult;
					return true;
				}
			}
			else if(valueType.IsAssignableFrom(typeof(Guid)))
			{
				if(Guid.TryParse(inputString, out Guid parseResult))
				{
					result = (object) parseResult;
					return true;
				}
			}
			else if(valueType.IsAssignableFrom(typeof(uint)))
			{
				if(uint.TryParse(inputString, out uint parseResult))
				{
					result = (object) parseResult;
					return true;
				}
			}
			else if(valueType.IsAssignableFrom(typeof(DateTime)))
			{
				if(DateTime.TryParse(inputString, out DateTime parseResult))
				{
					result = (object) parseResult;
					return true;
				}
			}
			else if(valueType.IsAssignableFrom(typeof(TimeSpan)))
			{
				// Generate the format string -1.12:34:56 from -1 12:34:56
				if(TimeSpan.TryParse(inputString.Replace(" ", "."), out TimeSpan parseResult))
				{
					result = (object) parseResult;
					return true;
				}
			}
			else if(valueType.IsEnum)
			{ 
				if(Enum.TryParse(valueType, inputString, out object? parseResult) && parseResult != null)
				{
					result = (object) parseResult;
					return true;
				}
			}

			result = null;
			return false;
		}

		/// <summary>
		/// Parse the input string as a specified type.
		/// </summary>
		/// <typeparam name="T">Type to parse the string as.</typeparam>
		/// <param name="inputString">String to parse.</param>
		/// <param name="result">Parsed value or null if the value could not be parsed.</param>
		/// <returns>Returns true if the value was successfully parsed.</returns>
		public static bool TryParse<T>(string inputString, out T? result)
		{ 
			bool success = TryParse(inputString, typeof(T), out object? parsedValue);
			result = (T?) (object?) parsedValue;
			return success;
		}

		/// <summary>
		/// Converts a filter operator to a string.
		/// </summary>
		/// <param name="filterOperator">Filter operator to convert to a string.</param>
		/// <returns>String representation of the filter operator.</returns>
		public static string StringFormat(this FilterOperators filterOperator)
		{ 
			switch(filterOperator)
			{ 
				case FilterOperators.Equals:				return "=";	
				case FilterOperators.NotEquals:				return "≠";	
				case FilterOperators.Like:					return "≈";	
				case FilterOperators.NotLike:				return "!≈";	
				case FilterOperators.Any:					return ",";	
				case FilterOperators.NotAny:				return "!,";	
				case FilterOperators.GreaterThan:			return ">";	
				case FilterOperators.LessThan:				return "<";	
				case FilterOperators.GreaterThanOrEqual:	return "≥";	
				case FilterOperators.LessThanOrEqual:		return "≤";	
			}

			return string.Empty;
		}

		/// <summary>
		/// Parses a filter operator string into a <see cref="FilterOperators"/> type.
		/// 
		/// If the string doesn't match a valid filter operator, <see cref="FilterOperators.None"/> will be returned.
		/// </summary>
		/// <param name="filterOperatorString">String to parse.</param>
		/// <returns>Parsed filter operator or <see cref="FilterOperators.None"/> if the string could not be parsed.</returns>
		public static FilterOperators ToFilterOperator(string filterOperatorString)
		{ 
			switch(filterOperatorString)
			{ 
				case "=":	return FilterOperators.Equals;				
				case "≠":	return FilterOperators.NotEquals;			
				case "≈":	return FilterOperators.Like;				
				case "!≈":	return FilterOperators.NotLike;				
				case ",":	return FilterOperators.Any;					
				case "!,":	return FilterOperators.NotAny;					
				case ">":	return FilterOperators.GreaterThan;			
				case "<":	return FilterOperators.LessThan;			
				case "≥":	return FilterOperators.GreaterThanOrEqual;	
				case "≤":	return FilterOperators.LessThanOrEqual;		
			}

			return FilterOperators.Equals;
		}
	}
}
