using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TypeScriptDemo
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
                .UseNUglify()
                // for the sake of demonstration we enable file system caching
                .UseFileSystemCaching(options => options.AutoResetOnCreate = true)
                // you need to add this if you need the ES6 module bundling feature
                .AddEcmaScript();

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

            app.UseBundling(
                bundles =>
                {
                    bundles.AddCss("/site.css")
                        .Include("/css/*.css");

                    // you need tslib if you target older JavaScript versions like ES6

                    bundles.AddJs("/lib.js")
                        .Include("/lib/tslib/tslib.js");

                    bundles.AddJs("/main.js")
                        .Include("/js/main.js")
                        // this enables included files to be treated as ES6 modules and bundled accordingly:
                        // include the root file(s) only and the imports will be automatically discovered,
                        // moreover all the involved files will be watched and the bundle will be automatically
                        // rebuilt when any change detected (as long as running in development environment)
                        .EnableEs6ModuleBundling();

                    // alternatively, instead of referencing tslib.js directly, you may as well enable importHelpers in tsconfig.json and use a custom resolver
                    // (this will prevent source includes though because TypeScript compiler is unable to emit a relative path to the tslib module)

                    //bundles.AddJs("/main.js")
                    //    .Include("/js/main.js")
                    //    .EnableEs6ModuleBundling(options =>
                    //    {
                    //        options.ImportResolver = (url, initiator, factory) => url switch
                    //        {
                    //            "tslib" when initiator.TryResolveModule("/lib/tslib/tslib.es6.js", out _, out ModuleResource module) => module,
                    //            _ => null
                    //        };
                    //    })
                    //    .EnableSourceIncludes(false);
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
