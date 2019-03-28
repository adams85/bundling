using Karambolo.AspNetCore.Bundling;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace VueDemo
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
                .AddLess();

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
                    // we use LESS in this demo
                    bundles.AddLess("/site.css")
                        .Include("/less/site.less");

                    // defines a Javascript bundle containing your Vue application and components
                    bundles.AddJs("/app.js")
                        .Include("/js/components/*.js")
                        .Include("/js/app.js");
                });

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
