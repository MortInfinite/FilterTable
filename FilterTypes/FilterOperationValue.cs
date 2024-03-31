using System.Diagnostics;

namespace FilterTypes
{
	/// <summary>
	/// Describes a filter operation to perform, when filtering data.
	/// 
	/// This type is an immutable.
	/// </summary>
	/// <see cref="FilterOperation">This is a mutable version of the same data.</see>
	[DebuggerDisplay("{Property,nq} {Operator} {Value}")]
	public struct FilterOperationValue
	{
		/// <summary>
		/// Creates a new blank filter operation.
		/// </summary>
		public FilterOperationValue()
		{
		}

		/// <summary>
		/// Creates a new filter operation.
		/// </summary>
		/// <param name="property">Name of the property that the filter will apply to</param>
		/// <param name="operator">Which filter operation to perform on the data.</param>
		/// <param name="value">Value to filter by.</param>
		public FilterOperationValue(string property, FilterOperators @operator, string? value)
		{ 
			Property	= property;
			Operator	= @operator;
			Value		= value;
		}

		/// <summary>
		/// Name of the property that the filter will apply to.
		/// </summary>
		public string Property
		{
			get; 
			set;
		} = string.Empty;

		/// <summary>
		/// Which filter operation to perform on the data.
		/// </summary>
		public FilterOperators Operator
		{
			get; 
			set;
		} = FilterOperators.Equals;

		/// <summary>
		/// Value to filter by.
		/// 
		/// This value will be converted to the data type of the specified <see cref="Property"/>, before performing the filter operation.
		/// </summary>
		public string? Value
		{
			get; 
			set;
		} = null;
	}
}
