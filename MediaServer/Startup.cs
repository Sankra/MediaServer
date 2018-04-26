﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaServer.Configuration;
using MediaServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using SlackConnector;

namespace MediaServer
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables()
                .AddJsonFile($"config.json", true);
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddResponseCaching();
            services.AddMvc();

            var config = Configuration.Get<AppConfig>();         
            services.AddSingleton<IConferenceConfig>(config);
            services.AddSingleton<IBlogStorageConfig>(config);
			services.AddSingleton<ISlackConfig>(config);

			services.AddSingleton<ISlackConnector, SlackConnector.SlackConnector>();

            services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Directory.GetCurrentDirectory()));

            services.AddSingleton<TalkService>();
            services.AddSingleton<ITalkService, CachedTalkService>();
            services.AddSingleton<IContentService, ContentService>();
			services.AddSingleton<ISlackService, SlackService>();
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
            app.UseResponseCaching();
            app.Use(async (context, next) =>
            {
                context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = OneYearTimeSpan
                };
                context.Response.Headers[HeaderNames.Vary] = new string[] { "Accept-Encoding" };

                await next();
            });

            app.UseStaticFiles(new StaticFileOptions {
                OnPrepareResponse = ctx => {
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] = MaxAgeStaticFiles;
                }
            });

			// TODO: Delete and use latest talks in conference controller as deafult
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
