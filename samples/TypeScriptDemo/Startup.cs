using System;
using Karambolo.AspNetCore.Bundling;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TypeScriptDemo
{
    public class Startup
    {
        readonly IHostingEnvironment _env;

        public Startup(IConfiguration configuration, IHostingEnvironment env)
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
                // for the sake of demonstration we enable file system caching
                .UseFileSystemCaching(options => options.AutoResetOnCreate = true)
                // you need to add this if you need the ES6 module bundling feature
                .AddEcmaScript();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (_env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseBundling(
                // as we rebased the static files to /static (see UseStaticFiles()),
                // we need to pass this information to the bundling middleware as well,
                // otherwise urls would be rewrited incorrectly
                new BundlingOptions
                {
                    StaticFilesRequestPath = "/static"
                },
                bundles =>
                {
                    bundles.AddCss("/site.css")
                        .Include("/css/*.css");

                    bundles.AddJs("/main.js")
                        .Include("/ts/main.js")
                        // this enables included files to be treated as ES6 modules and bundled accordingly:
                        // include the root file(s) only and the imports will be automatically discovered,
                        // moreover all the involved files will be watched and the bundle will be automatically
                        // rebuilt when any change detected (as long as running in development environment)
                        .EnableEs6ModuleBundling();
                });

            app.UseStaticFiles(new StaticFileOptions { RequestPath = "/static" });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
