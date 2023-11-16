using BlazorLogViewer.Data;
using FilterTable;
//using Microsoft.AspNetCore.Authentication.Negotiate;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net.Http.Headers;
using BlazorLogViewer.WASM.Shared;

namespace BlazorLogViewer.WASM
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<App>("#app");
			builder.RootComponents.Add<HeadOutlet>("head::after");

			string? webServiceUri = builder.Configuration["LogEntryServiceUrl"];
			if(string.IsNullOrEmpty(webServiceUri))
				throw new Exception($"The \"LogEntryServiceUrl\" appSettings entry is missing.");

			builder.Services.AddScoped(serviceProvider =>
			{
				HttpClientHandler clientHandler = new HttpClientHandler();
				
				// Introduce a message handler which is called every time a REST request is made, in order to modify the request headers.
				DefaultBrowserOptionsMessageHandler browserOptionsMessageHandler = new DefaultBrowserOptionsMessageHandler(clientHandler)
				{ 
					DefaultBrowserRequestCache			= BrowserRequestCache.NoStore,
					DefaultBrowserRequestCredentials	= BrowserRequestCredentials.Include,
					DefaultBrowserRequestMode			= BrowserRequestMode.Cors,
				};

				HttpClient client = new HttpClient(browserOptionsMessageHandler)
				{ 
					BaseAddress = new Uri(webServiceUri),
				};

				// Accept json data type only.
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				return client;
			});

			builder.Services.AddAuthorizationCore();
			builder.Services.AddApiAuthorization();
			builder.Services.AddScoped<LogEntryService>();
			builder.Services.AddScoped<ClipboardService>();
			builder.Services.AddMudServices();

			await builder.Build().RunAsync();
		}
	}
}