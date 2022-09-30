using ApplicationHealthCheck.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApplicationHealthCheck
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddSingleton<APIHealthChecks>();
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.AddHealthChecksUI(s =>
            {
                s.AddHealthCheckEndpoint("Health Check DashBoard", "/healthcheck");
                s.SetEvaluationTimeInSeconds(5);
                s.MaximumHistoryEntriesPerEndpoint(10);
                s.SetMinimumSecondsBetweenFailureNotifications(60);
            }).AddInMemoryStorage();
            AddAllHealthChecks(services);
        }

        private void AddAllHealthChecks(IServiceCollection services)
        {
            // services.AddHealthChecksUI().Services.AddMemoryCache();
            services.AddHealthChecks().
                //AddSqlServer(Configuration["SQLServerConnectionString"], name: "sql",
                //    failureStatus: HealthStatus.Degraded,
                //    tags: new[] { "db", "sql", "sqlserver" }).
            AddCheck(name: "API Check",check: () => { return HealthCheckResult.Healthy(); }, tags: new[] { "API", "API health" })
            .AddCheck<APIHealthChecks>("Custom Check", tags: new[] { "custom" });
                //services.AddHealthChecksUI();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();
           
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapHealthChecksUI(setup =>
                {
                    setup.UIPath = "/health-ui";
                    setup.ApiPath = "/health-api";
                });
                //endpoints.MapHealthChecks("/healthcheck", new HealthCheckOptions()
                //{
                //    // Predicate = _ => true,
                //    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                //}); ;
            });
            app.UseHealthChecks("/healthchecksql", new HealthCheckOptions
            {
                Predicate = healthCheck => healthCheck.Tags.Contains("sql"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.UseHealthChecks("/healthcheck", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter =  UIResponseWriter.WriteHealthCheckUIResponse
            }) ;
            app.UseHealthChecksUI(setup =>
            {
                setup.UIPath = "/health-ui";
                setup.ApiPath = "/health-api";
               
            });

        }

        private Task CreateCustomResponse(HttpContext httpContext, HealthReport result)
        {
            //var text = JsonConvert.SerializeObject(result, Formatting.Indented, new JsonSerializerSettings
            //{
            //    MissingMemberHandling = MissingMemberHandling.Ignore,
            //    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            //    Converters = new List<JsonConverter>
            //    {
            //        new StringEnumConverter()
            //    },
            //    StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
            //});
            return httpContext.Response.WriteAsync(result.ToString());
        }
        private static Task WriteResponse(HttpContext context, HealthReport healthReport)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var options = new JsonWriterOptions { Indented = true };

            using var memoryStream = new MemoryStream();
            using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("status", healthReport.Status.ToString());
                jsonWriter.WriteStartObject("results");

                foreach (var healthReportEntry in healthReport.Entries)
                {
                    jsonWriter.WriteStartObject(healthReportEntry.Key);
                    jsonWriter.WriteString("status",
                        healthReportEntry.Value.Status.ToString());
                    jsonWriter.WriteString("description",
                        healthReportEntry.Value.Description);
                    jsonWriter.WriteStartObject("data");

                    foreach (var item in healthReportEntry.Value.Data)
                    {
                        jsonWriter.WritePropertyName(item.Key);

                        System.Text.Json.JsonSerializer.Serialize(jsonWriter, item.Value,
                            item.Value?.GetType() ?? typeof(object));
                    }

                    jsonWriter.WriteEndObject();
                    jsonWriter.WriteEndObject();
                }

                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }

            return context.Response.WriteAsync(
                Encoding.UTF8.GetString(memoryStream.ToArray()));
        }
        private static Task CustomHealthCheckResponse(HttpContext httpContext, HealthReport result)
        {
            httpContext.Response.ContentType = "application / json";
            //var json = new JObject(
            //new JProperty("status", result.Status.ToString()),
            //new JProperty("results", new JObject(result.Entries.Select(pair =>
            //    new JProperty(pair.Key, new JObject(
            //        new JProperty("status", pair.Value.Status.ToString()),
            //new JProperty("description", pair.Value.Description),
            //new JProperty("data", new JObject(pair.Value.Data.Select(
            //    p => new JProperty(p.Key, p.Value))))))))));
            //var text = json.ToString(Formatting.Indented);
            var text = JsonConvert.SerializeObject(result, Formatting.Indented, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter()
                },
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
            });
            return httpContext.Response.WriteAsync((text));
        }
    }
}
