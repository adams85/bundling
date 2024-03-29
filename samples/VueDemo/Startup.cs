﻿using Karambolo.AspNetCore.Bundling;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VueDemo
{
    public class Startup
    {
        readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBundling()
                .UseDefaults(_env)
                .UseWebMarkupMin()
                .AddLess();

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseBundling(bundles =>
            {
                // we use file configuration in this sample, which is equivalent to the commented code configuration below
                bundles.LoadFromConfigFile("bundleconfig.json", _env.ContentRootFileProvider);

                //// we use LESS in this demo
                //bundles.AddLess("/site.css")
                //    .Include("/less/site.less");

                //// defines a Javascript bundle containing your Vue application and components
                //bundles.AddJs("/app.js")
                //    .Include("/js/components/*.js")
                //    .Include("/js/app.js");
            });

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
