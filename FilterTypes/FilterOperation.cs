using System.Diagnostics;
using System.ComponentModel;

namespace FilterTypes
{
	/// <summary>
	/// Describes a filter operation to perform, when filtering data.
	/// 
	/// This type is mutable.
	/// </summary>
	/// <see cref="FilterOperationValue">This is an immutable version of the same data.</see>
	[DebuggerDisplay("{Property,nq} {Operator} {Value}")]
	public class FilterOperation	:NotifyPropertyChangedBase
	{
		/// <summary>
		/// Creates a new blank filter operation.
		/// </summary>
		public FilterOperation()
		{
		}

		/// <summary>
		/// Creates a new filter operation.
		/// </summary>
		/// <param name="property">Name of the property that the filter will apply to</param>
		/// <param name="operator">Which filter operation to perform on the data.</param>
		/// <param name="value">Value to filter by.</param>
		public FilterOperation(string property, FilterOperators @operator, string value)
		{ 
			Property	= property;
			Operator	= @operator;
			Value		= value;
		}

		#region Methods
		/// <summary>
		/// Creates a new <see cref="FilterOperationValue"/> based on the values of the <see cref="FilterOperation"/>.
		/// </summary>
		/// <param name="filterOperationClass"><see cref="FilterOperation"/> to convert to a <see cref="FilterOperationValue"/>.</param>
		public static implicit operator FilterOperationValue(FilterOperation filterOperationClass)
		{ 
			return new FilterOperationValue(filterOperationClass.Property, filterOperationClass.Operator, filterOperationClass.Value);
		}

		/// <summary>
		/// Determine if this object has the same value as the other object.
		/// </summary>
		/// <param name="other">Object to compare this object with.</param>
		/// <returns>Returns true if this object has the same value as the <paramref name="other"/> object.</returns>
		public bool Equals(FilterOperation other)
		{ 
			if(	Property == other?.Property &&
				Operator == other?.Operator &&
				Value == other?.Value)
				return true;

			return false;
		}
		#endregion

		#region Properties
		/// <summary>
		/// Name of the property that the filter will apply to.
		/// </summary>
		public string Property
		{
			get
			{
				return m_property;
			}
			set
			{
				// Update the field and notify subscribers that the property changed.
				this.SetProperty(ref m_property, value, NotifyPropertyChanged);
			}
		}

		/// <summary>
		/// Which filter operation to perform on the data.
		/// </summary>
		public FilterOperators Operator
		{
			get
			{
				return m_operator;
			}
			set
			{
				// Update the field and notify subscribers that the property changed.
				this.SetProperty(ref m_operator, value, NotifyPropertyChanged);
			}
		}

		/// <summary>
		/// Value to filter by.
		/// 
		/// This value will be converted to the data type of the specified <see cref="Property"/>, before performing the filter operation.
		/// </summary>
		public string? Value
		{
			get
			{
				return m_value;
			}
			set
			{
				// Update the field and notify subscribers that the property changed.
				this.SetProperty(ref m_value, value, NotifyPropertyChanged);
			}
		}
		#endregion

		#region Fields
		/// <summary>
		/// Backing field for the <see cref="Property"/> property.
		/// </summary>
		private string m_property = string.Empty;

		/// <summary>
		/// Backing field for the <see cref="Operator"/> property.
		/// </summary>
		private FilterOperators m_operator = FilterOperators.Equals;

		/// <summary>
		/// Backing field for the <see cref="Value"/> property.
		/// </summary>
		private string? m_value = null;
		#endregion
	}
}
