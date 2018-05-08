﻿using System;
using System.IO;
using System.Net.Http;
using MediaServer.Clients;
using MediaServer.Configuration;
using MediaServer.Models;
using MediaServer.Services;
using MediaServer.Services.Cache;
using MediaServer.Services.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace MediaServer
{
	public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables()
                .AddJsonFile($"config.json", true);
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
			// TODO: Support tags, comma separated
			// TODO: Support multiple speakers pr talk, comma separated
            services.AddMemoryCache();
            services.AddResponseCaching();
			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            var config = Configuration.Get<AppConfig>();         
			var conferenceMetaDataService = new ConferenceMetaDataService(config);
            var conferenceConfig = conferenceMetaDataService.GetConferenceConfig().GetAwaiter().GetResult();
            services.AddSingleton(conferenceConfig);

			services.AddSingleton<IBlogStorageConfig>(config);         

			var httpClient = new HttpClient();
			var slackIntegrationClient = new SlackIntegrationClient(httpClient);
			slackIntegrationClient.PopulateMetaData().GetAwaiter().GetResult();
			var slackUsers = slackIntegrationClient.GetUsers().GetAwaiter().GetResult();
			var users = new Users(slackUsers);
			services.AddSingleton(httpClient);
			services.AddSingleton(users); 
			services.AddSingleton<ISlackClient>(slackIntegrationClient);
			services.AddSingleton<CacheWarmerClient>();

            services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Directory.GetCurrentDirectory()));
			services.AddSingleton<BlobStoragePersistence>();
            
			services.AddSingleton<OldTalkService>();
			services.AddSingleton<ThumbnailService>();
            services.AddSingleton<ConferenceService>();
            services.AddSingleton<ContentService>();    
			services.AddSingleton<MediaCache>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
				// TODO: Fix this
                app.UseExceptionHandler("/Home/Error");
            }
                     
            const int OneYear = 31536000;
            var MaxAgeStaticFiles = "public,max-age=" + OneYear;
            var OneYearTimeSpan = TimeSpan.FromSeconds(OneYear);
			var varyByAllQueryKeys = new[] { "*" };
            app.UseResponseCaching();
            app.Use(async (context, next) =>
            {
                context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = OneYearTimeSpan
                };
                context.Response.Headers[HeaderNames.Vary] = new string[] { "Accept-Encoding" };
				var responseCachingFeature = context.Features.Get<IResponseCachingFeature>();
				if (responseCachingFeature != null) {
					responseCachingFeature.VaryByQueryKeys = varyByAllQueryKeys;
                }

                await next();
            });

            app.UseStaticFiles(new StaticFileOptions {
                OnPrepareResponse = ctx => {
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] = MaxAgeStaticFiles;
                }
            });

			// TODO: Delete and use latest talks in conference controller as deafult
            app.UseMvc(routes => {
                routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
