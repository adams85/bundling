using System.Collections.Generic;
using System.Linq;
using Karambolo.AspNetCore.Bundling;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace QuickStartTemplate;

public class Bundles : DesignTimeBundlingConfiguration
{
    // setup for run-time mode bundling
    public static void ConfigureServices(IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddBundling()
            .UseDefaults(environment)
            .UseNUglify()
            .AddSass()
            .AddEcmaScript();
    }

    public Bundles() { }

    // setup for design-time mode bundling
    public override IEnumerable<IBundlingModule> Modules => base.Modules.Concat(new IBundlingModule[]
    {
            new NUglifyBundlingModule(),
            new SassBundlingModule(),
            new EcmaScriptBundlingModule()
    });

    public override void Configure(BundleCollectionConfigurer bundles)
    {
        bundles.AddSass("/css/vendor.css")
            .Include("/lib/bootstrap/scss/bootstrap.scss");

        bundles.AddSass("/css/site.css")
            .Include("/scss/site.scss");

        bundles.AddJs("/js/vendor.js")
            .Include("/lib/jquery/dist/jquery.js")
            .Include("/lib/jquery-validation/dist/jquery.validate.js")
            .Include("/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.js")
            .Include("/lib/bootstrap/dist/js/bootstrap.bundle.js");

        bundles.AddJs("/js/site.js")
            .Include("/js/site.js")
            .EnableEs6ModuleBundling();
    }
}
