namespace FilterTypes
{
	/// <summary>
	/// Query result containing a result set and a count of the total number of results available, that match a filter criteria.
	/// </summary>
	public struct QueryResult<T>
	{ 
		/// <summary>
		/// Results matching the filter criteria.
		/// </summary>
		public IList<T> Results
		{ 
			get;
			set;
		}

		/// <summary>
		/// Total number of results available.
		/// </summary>
		public int TotalCount
		{ 
			get;
			set;
		}
	}
}
