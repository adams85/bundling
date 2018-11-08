using System.Globalization;
using System.Linq;
using JsTranslation.Infrastructure.Bundling;
using JsTranslation.Infrastructure.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace JsTranslation
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
            // enabling localization (see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-2.1)
            services.AddLocalization();

            // by supplying our IStringLocalizerFactory impl. we override the default (resource-based) localization with PO localization 
            // (if you want to use the default localization, remove the following three lines and register your ILocalizationProvider implementation)
            services.AddSingleton<IStringLocalizerFactory, POStringLocalizerFactory>();
            services.AddSingleton<IPOLocalizationProvider, POLocalizationProvider>();
            services.AddSingleton<ILocalizationProvider, POLocalizationProvider>();

            // enabling bundling (using WebMarkupMin)
            services.AddBundling()
                .UseDefaults(_env)
                .UseWebMarkupMin();

            services.AddMvc()
                .SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILocalizationProvider localizationProvider, IStringLocalizerFactory stringLocalizerFactory)
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

            // defining bundles
            app.UseBundling(bundles =>
            {
                bundles.AddCss("/site.css")
                    .Include("/css/*.css");

                // we define a separate bundle for each of the available cultures
                // (translations are loaded from {ContentRoot}/App_Data/Localization/ by POLocalizationProvider)
                var currentStringLocalizer = stringLocalizerFactory.Create(typeof(Program));
                foreach (var culture in localizationProvider.AvailableCultures)
                {
                    var stringLocalizer = currentStringLocalizer.WithCulture(culture);

                    bundles.AddJs($"/app.{culture.Name}.js")
                        .Include("/js/app.js")
                        // this is THE KEY STEP: insert the translator in the bundle item transformation pipeline
                        .UseItemTransforms(transforms => transforms.Insert(0, new JsTranslatorTransform(stringLocalizer)));
                }
            });

            app.UseStaticFiles();

            // enabling request localization
            // in this demo we only allow culture to be picked up from query string by setting RequestCultureProviders
            // (see also https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-2.1#localization-middleware)
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = localizationProvider.AvailableCultures,
                SupportedUICultures = localizationProvider.AvailableCultures,
                RequestCultureProviders = new[] { new QueryStringRequestCultureProvider() }
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
