using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using FilterTable;

namespace FilterTableExample.Shared
{
	/// <summary>
	/// Provides extension methods to the navigation manager, accessing parts of the query string.
	/// </summary>
	public static class NavigationManagerExtensions
	{
		/// <summary>
		/// Retrieve a dictionary of query filter arguments and their values.
		/// 
		/// Query filters that don't have a value, are skipped.
		/// If the query filter specifies the same argument multiple times, only the value of the last argument is returned.
		/// </summary>
		/// <param name="navigationManager">Navigation manager from which to retrieve the query string.</param>
		/// <returns>Dictionary of query string arguments and their values.</returns>
		public static Dictionary<string, string>? GetQueryString(this NavigationManager navigationManager)
		{
			Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

			// Retrieve the current URI.
			Uri? uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);
			if(uri == null)
				return result;

			// Convert the query string into a dictionary of parameter name and parameter values.
			// If a parameter is specified more than once, each parameter value is added as a value belonging to the same key.
			Dictionary<string, StringValues>? queryString = QueryHelpers.ParseQuery(uri.Query);
			if(queryString == null)
				return result;
			
			// Convert the dictionary of enumerable results to a dictionary where only the last result is returned.
			foreach(var currentEntry in queryString)
			{
				string? currentValue = currentEntry.Value.LastOrDefault();

				// Don't return parameters that don't have any value specified.
				if(currentValue != null)
					result.Add(currentEntry.Key, currentValue);
			}

			return result;
		}

		/// <summary>
		/// Retrieve the value of the specified query string.
		/// </summary>
		/// <typeparam name="T">Type of value to retrieve.</typeparam>
		/// <param name="navigationManager">Navigation manager from which to retrieve the query string.</param>
		/// <param name="name">Case insensitive name of the value to retrieve.</param>
		/// <param name="fallbackValue">Value to return if the name isn't found.</param>
		/// <returns>Value of the specified query string or <paramref name="fallbackValue"/> if the query name isn't found.</returns>
		public static T? TryGetQueryString<T>(this NavigationManager navigationManager, string name, T? fallbackValue=default)
		{
			var queryStringDictionary = navigationManager.GetQueryString();
			
			string? valueString = null;

			bool exists = queryStringDictionary?.TryGetValue(name, out valueString) ?? false;
			if(!exists || valueString == null)
				return fallbackValue;

			T? result = StringHelpers.Parse<T>(valueString, fallbackValue);
			return result;
		}

		/// <summary>
		/// Set the value of the specified query string, in the URL.
		/// </summary>
		/// <typeparam name="T">Type of value to set.</typeparam>
		/// <param name="navigationManager">Navigation manager from which to get and set the query string.</param>
		/// <param name="name">Name of the value to set.</param>
		/// <param name="value">Value to set or null to clear the value.</param>
		public static void SetQueryString<T>(this NavigationManager navigationManager, string name, T value)
		{ 
			string? valueString = value?.ToString();

			string? currentQueryStringValue = navigationManager.TryGetQueryString<string?>(name, null);

			// If the specified value is already set in the query string.
			if(currentQueryStringValue == valueString)
				return;
			
			// Specify the query parameter.
			navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, valueString));
		}

		/// <summary>
		/// Set the value of the specified query string, in the URL.
		/// </summary>
		/// <typeparam name="T">Type of value to set.</typeparam>
		/// <param name="navigationManager">Navigation manager from which to get and set the query string.</param>
		/// <param name="name">Name of the value to set.</param>
		/// <param name="value">Value to set or null to clear the value.</param>
		public static void SetQueryString(this NavigationManager navigationManager, IEnumerable<KeyValuePair<string, string?>> keyValuePairs)
		{
			// Retrieve all query values from the query string.
			Dictionary<string, string>? currentQueryString = GetQueryString(navigationManager) ?? new Dictionary<string, string>();

			// Set or update each query value. Remove values that are set to the value null.
			foreach(KeyValuePair<string, string?> keyValuePair in keyValuePairs)
			{
				// If the value is not empty, set the value on the dictionary. 
				// If the value is empty, remove the value from the dictionary.
				if(!string.IsNullOrEmpty(keyValuePair.Value))
					currentQueryString[keyValuePair.Key] = keyValuePair.Value;
				else
					currentQueryString.Remove(keyValuePair.Key);
			}

			// Retrieve the current URI.
			Uri uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);

			// Uri without query string.
			string uriString = uri.GetLeftPart(UriPartial.Path);

			// Add the query string to the URI.
			uriString = QueryHelpers.AddQueryString(uriString, keyValuePairs);
			
			// Specify the query parameter.
			navigationManager.NavigateTo(uriString);
		}
	}
}
