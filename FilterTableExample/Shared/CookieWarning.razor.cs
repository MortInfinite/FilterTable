using Microsoft.AspNetCore.Components;

namespace FilterTableExample.Shared
{
	/// <summary>
	/// Cookie warning that is shown until the user consents to the warning.
	/// </summary>
	public partial class CookieWarning
	{
		/// <summary>
		/// Indicate if the cookie warning is shown.
		/// </summary>
		[Parameter]
		public bool Shown
		{
			get; 
			set;
		} = true;

		/// <summary>
		/// Hide the cookie warning, when the user has consented.
		/// </summary>
		protected void ConsentClicked()
		{ 
			Shown = false;
		}
	}
}
