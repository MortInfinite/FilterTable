using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;

namespace LogEntryDataAccess
{
	/// <summary>
	/// Web service providing access to read log entries.
	/// 
	/// The web service running this web service should be configured to use both anonymous and Windows Authentication.
	/// </summary>
	/// <remarks>
	/// In order to be accessed from a Blazor WASM application, this web service needs to be configured to allow both
	/// Windows Authentication AND anonymous authentication. The Blazor WASM application needs to connect to the web service
	/// using anonymous authentication, before it will allow requesting authentication information using Windows authentication.
	/// 
	/// To restrict access to retrieving log entries, without authenticating, the LogEntryController is configured with 
	/// an [Authorize] attribute. This ensures that its methods can only be accessed by authenticated users and not anonymous users.
	/// </remarks>
	public class Program
	{
		public static void Main(string[] args)
		{
			WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

			// Enable cross origin resource sharing, so the web service can be accessed from another website.
			builder.Services.AddCors();

			// Add support for MVC controllers.
			// Use strings instead of numbers, for enum parameters.
			builder.Services.AddControllers()
							.AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); })
							.ConfigureApiBehaviorOptions(options =>
							{
								options.SuppressModelStateInvalidFilter                 = true;
							});

			// Add support for authentication.
			builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
							.AddNegotiate();

			// Add permission control.
			builder.Services.AddAuthorization(options =>
			{
				// By default, all incoming requests will be authorized according to the default policy.
				options.FallbackPolicy = options.DefaultPolicy;
			});

			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			// Create a log entry service that can be accesed using dependency injection.
			builder.Services.AddDbContextFactory<LoggingContext>((Action<DbContextOptionsBuilder>?) null, ServiceLifetime.Transient);
			builder.Services.AddScoped<LogEntryService>();

			WebApplication app = builder.Build();

			// Configure the HTTP request pipeline.
			if(app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			// Allow the web service to be accessed from any URL.
			// This can be modified by examining the value of the origins property, in the SetIsOriginAllowed delegate,
			// and only return true if the origin matches an accepted URL.
			app.UseCors(configurePolicy => configurePolicy	.AllowAnyMethod()
															.AllowAnyHeader()
															.AllowCredentials()
															.SetIsOriginAllowed((origin) =>
															{ 
																return true;
															}));

			// Redirect to HTTPS from HTTP.
			app.UseHttpsRedirection();

			// Add support for login and permission controls.
			app.UseAuthentication();
			app.UseAuthorization();

			// Map controller routes, so controllers can be accessed by specifying a matching URL.
			app.MapControllers();

			app.Run();
		}
	}
}