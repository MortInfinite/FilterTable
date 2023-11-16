namespace FilterTypes
{
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
