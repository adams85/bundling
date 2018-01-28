using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling;
using Karambolo.AspNetCore.Bundling.Less;
using Karambolo.AspNetCore.Bundling.NUglify;
using Karambolo.AspNetCore.Bundling.WebMarkupMin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DynamicBundle
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
                .UseNUglify()
                .UseWebMarkupMin()
                .AddLess();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
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

                    // we are adding a less bundle whose input is generated dynamically based on the query string
                    var dynamicSource = new DynamicSource(bundles.Bundles.SourceFileProvider);
                    bundles.AddLess("/dynamic.css")
                        .AddDynamicSource(dynamicSource.ProvideItems, dynamicSource.ChangeTokenFactory)
                        .DependsOnParams()
                        .UseCacheOptions(new BundleCacheOptions { SlidingExpiration = TimeSpan.FromMinutes(10) });
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
