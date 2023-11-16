using Microsoft.AspNetCore.Components;

namespace BlazorLogViewer.Shared
{
	public partial class CookieWarning
	{
		[Parameter]
		public bool Shown
		{
			get; 
			set;
		} = true;

		protected void ConsentClicked()
		{ 
			Shown = false;
		}
	}
}
