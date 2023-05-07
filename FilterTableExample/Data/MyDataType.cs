using System.Diagnostics;

namespace FilterTableExample.Data
{
	/// <summary>
	/// Contains all the information needed to create a MyDataType, in the database.
	/// </summary>
    [Serializable()]
	[DebuggerDisplay("Name = {Name}, Description = {Description}, Price = {Price}")]
	public class MyDataType
	{
		/// <summary>
		/// Unique database ID.
		/// </summary>
		public int Id
		{
			get; 
			set;
		}

		/// <summary>
		/// Name of the item.
		/// </summary>
		public string? Name
		{
			get; 
			set;
		}

		/// <summary>
		/// Description of the item.
		/// </summary>
		public string? Description
		{
			get; 
			set;
		}

		/// <summary>
		/// Price of the item.
		/// </summary>
		public decimal Price
		{
			get; 
			set;
		}

		/// <summary>
		/// Optional expiration date.
		/// </summary>
		public DateTime? ExpirationDate
		{
			get; 
			set;
		}
	}
}
