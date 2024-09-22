using System.Net;
using System.Net.Http.Headers;
using BlazorLogViewer.Data;
using FilterTable;
using Microsoft.AspNetCore.Authentication.Negotiate;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Determine if the appsettings has a DisableAuthentication: "true" value, for used while developing.
bool.TryParse(builder.Configuration["DisableAuthentication"], out bool disableAuthentication);
if(!disableAuthentication)
{
	// Add services to the container.
	builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
	   .AddNegotiate();

	builder.Services.AddAuthorization(options =>
	{
		// By default, all incoming requests will be authorized according to the default policy.
		options.FallbackPolicy = options.DefaultPolicy;
	});
}

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

string? webServiceUri = builder.Configuration["LogEntryServiceUrl"];
if(string.IsNullOrEmpty(webServiceUri))
	throw new Exception($"The \"LogEntryServiceUrl\" appSettings entry is missing.");

builder.Services.AddScoped(serviceProvider =>
{
	// Use current network credentials of the current user or application, to connect to the web service.
	CredentialCache credentialsCache = new CredentialCache();
	credentialsCache.Add(new Uri(webServiceUri, UriKind.Absolute), "Negotiate", CredentialCache.DefaultNetworkCredentials);

	HttpClientHandler clientHandler = new HttpClientHandler 
	{ 
		Credentials = credentialsCache
	};

	HttpClient client = new HttpClient(clientHandler)
	{ 
		BaseAddress = new Uri(webServiceUri),
	};

	// Accept json data type only.
	client.DefaultRequestHeaders.Accept.Clear();
	client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

	return client;
});

builder.Services.AddScoped<LogEntryService>();
builder.Services.AddScoped<ClipboardService>();
builder.Services.AddMudServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if(!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
//app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
