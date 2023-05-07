using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace FilterTable
{
	/// <summary>
	/// Provides access to copy content to the clipboard.
	/// </summary>
	public class ClipboardService
	{
		/// <summary>
		/// Create a new ClipboardService.
		/// </summary>
		/// <param name="jsRuntime">Javascript interop object, used to access Javascript engine.</param>
		public ClipboardService(IJSRuntime jsRuntime)
		{
			m_jsRuntime = jsRuntime;
		}

		/// <summary>
		/// Read clipboard contents.
		/// </summary>
		/// <returns>String containing clipboard contents.</returns>
		public ValueTask<string> ReadTextAsync()
		{
			return m_jsRuntime.InvokeAsync<string>("navigator.clipboard.readText");
		}

		/// <summary>
		/// Copy the specified text to the clipboard.
		/// </summary>
		/// <param name="text">Text to copy to the clipboard.</param>
		/// <returns>Created task.</returns>
		public ValueTask WriteTextAsync(string text)
		{
			return m_jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
		}

		private readonly IJSRuntime m_jsRuntime;
	}
}
