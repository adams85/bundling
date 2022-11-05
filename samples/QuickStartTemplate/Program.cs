using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace QuickStartTemplate;

public class Program
{
    public static readonly bool UsesDesignTimeBundling =
#if USES_DESIGNTIME_BUNDLING
        true;
#else
        false;
#endif

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder);

        var app = builder.Build();

        Configure(app);

        app.Run();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        // To enable bundling on build, set the UseDesignTimeBundling property to true in the csproj file.
        if (Program.UsesDesignTimeBundling)
        {
            builder.Services.AddBundling();
        }
        else
        {
            Bundles.ConfigureServices(builder.Services, builder.Environment);
        }

        builder.Services.AddRazorPages();
    }

    private static void Configure(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        if (Program.UsesDesignTimeBundling)
        {
            app.InitializeBundling();
        }
        else
        {
            app.UseBundling(new Bundles());
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();
    }
}
