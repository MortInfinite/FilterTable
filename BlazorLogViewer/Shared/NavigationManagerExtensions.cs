﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using FilterTable;
using FilterTypes;

namespace BlazorLogViewer.Shared
{
	/// <summary>
	/// Provides helper methods to use by the navigation manager.
	/// </summary>
	public static class NavigationManagerExtensions
	{
		/// <summary>
		/// Retrieve a dictionary of query filter arguments and their values.
		/// Query filters that don't have a value, are skipped.
		/// If the query filter specifies the same argument multiple times, only the value of the last argument is returned.
		/// </summary>
		/// <param name="navigationManager">Navigation manager from which to retrieve the query string.</param>
		/// <returns>Dictionary of query string arguments and their values.</returns>
		public static Dictionary<string, string>? GetQueryStringDictionary(this NavigationManager navigationManager)
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
		/// Retrieve a list of query filter arguments and their values.
		/// 
		/// Query filters that don't have a value, are skipped.
		/// If the query filter specifies the same argument multiple times, all values are returned.
		/// </summary>
		/// <param name="navigationManager">Navigation manager from which to retrieve the query string.</param>
		/// <returns>Dictionary of query string arguments and their values.</returns>
		public static IList<KeyValuePair<string, string>> GetQueryString(this NavigationManager navigationManager)
		{
			List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

			// Retrieve the current URI.
			Uri? uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);
			if(uri == null)
				return result;

			// Convert the query string into a dictionary of parameter name and parameter values.
			// If a parameter is specified more than once, each parameter value is added as a value belonging to the same key.
			Dictionary<string, StringValues>? queryString = QueryHelpers.ParseQuery(uri.Query);
			if(queryString == null)
				return result;
			
			// Convert the dictionary of enumerable results to a one dimentional array of keys and values.
			foreach(KeyValuePair<string, StringValues> currentEntry in queryString)
				foreach(string? currentValue in currentEntry.Value)
					if(currentValue != null)
						result.Add(new KeyValuePair<string, string>(currentEntry.Key, currentValue));

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
		public static T[] TryGetQueryString<T>(this NavigationManager navigationManager, string name)
		{
			IList<KeyValuePair<string, string>> queryStrings = navigationManager.GetQueryString();

			string[] stringResults = queryStrings	.Where(queryString => string.Equals(queryString.Key, name, StringComparison.InvariantCultureIgnoreCase))
													.Select(queryString => queryString.Value)
													.ToArray();

			List<T> results = new List<T>();

			foreach(string stringResult in stringResults)
				if(StringHelpers.TryParse<T>(stringResult, out T? parsedResult))
					results.Add(parsedResult!);

			return results.ToArray();
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

			string[] currentQueryStringValues = navigationManager.TryGetQueryString<string>(name);

			// If the specified value is already set in the query string.
			if(currentQueryStringValues.Any(currentValue => string.Equals(currentValue, valueString, StringComparison.InvariantCultureIgnoreCase)))
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
		public static void SetQueryString(this NavigationManager navigationManager, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
		{
			// Retrieve the current URI.
			Uri uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);

			// Uri without query string.
			string uriString = uri.GetLeftPart(UriPartial.Path);

			// Add the query string to the URI.
			foreach(var keyValuePair in keyValuePairs)
				if(keyValuePair.Value != null)
					uriString = QueryHelpers.AddQueryString(uriString, keyValuePair.Key, keyValuePair.Value);
			
			// Specify the query parameter.
			navigationManager.NavigateTo(uriString);
		}
	}
}
