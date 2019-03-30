# Karambolo.AspNetCore.Bundling

This repository contains components which provide run-time bundling and minification features for ASP.NET Core 2 in a similar way the System.Web.Optimization library does for classic ASP.NET.

[![NuGet Release](https://img.shields.io/nuget/v/Karambolo.AspNetCore.Bundling.svg)](https://www.nuget.org/packages/Karambolo.AspNetCore.Bundling/) [![Join the chat at https://gitter.im/Karambolo-AspNetCore-Bundling/Lobby](https://badges.gitter.im/Karambolo-AspNetCore-Bundling/Lobby.svg)](https://gitter.im/Karambolo-AspNetCore-Bundling/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

### Main features
- Css and Js minification and bundling (powered by [NUglify](https://github.com/xoofx/NUglify) and [WebMarkupMin](https://github.com/Taritsyn/WebMarkupMin)).
- Less compilation and bundling (built on [dotLess](https://github.com/dotless/dotless)).
- Sass/Scss compilation and bundling (built on [LibSassHost](https://github.com/Taritsyn/LibSassHost)).
- Straightforward and flexible configuration:
  - Configuration through fluent API.
  - **Compatibility with *bundleconfig.json***.
  - Multi-level settings inheritance.
- Full control over server and client-side caching (including cache busting). **Memory** and **file system cache** implementations are included.
- Replaceable backing storage by leveraging [.NET Core file system abstractions](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers).
- Automatic refresh when input files change. As of version 1.3, **Less/Sass/Scss imports are tracked, as well.**
- Razor tag helpers and the familiar, System.Web.Optimization-like API can be used as well.
- Correct handling of URL path prefixes (app branch prefix, static files middleware prefix, etc.)
- Fully customizable transformation pipelines.
- **Dynamic content sources** and query string **parameterized bundles**.
- Modular design, great extensibility.

### Version 2.0

The release candidate of the new major version is already available at NuGet.

There are some interesting improvements which are worth checking out:
* Change detection works now not only for the top level files but also the imported ones.
* Bundling of ES6 (ECMAScript 2015) modules (á la webpack). Also in this case, changes are detected in the imports.
* Design-time bundling manually using the new dotnet CLI tool or automatically by customizing the build process. 

More info to follow soon.

### Basic usage

#### 1. Install NuGet packages

The *Karambolo.AspNetCore.Bundling* package contains the core components only, so you need to install a package that provides an actual implementation. You can choose between *NUglify* and *WebMarkupMin* implementations currently.

    Install-Package Karambolo.AspNetCore.Bundling.NUglify

or

    Install-Package Karambolo.AspNetCore.Bundling.WebMarkupMin

If you want to use CSS pre-proccessor features as well, you need to install a further package:

- Less

      Install-Package Karambolo.AspNetCore.Bundling.Less

- Sass/Scss

      Install-Package Karambolo.AspNetCore.Bundling.Sass

  The current implementation uses LibSassHost under the hood, which is a wrapper around [LibSass](https://sass-lang.com/libsass), an unmanaged library written in C/C++. Therefore you need to install an additional NuGet package which contains this native dependency compiled to your target platform. E.g. on Windows x64 systems: `Install-Package LibSassHost.Native.win-x64`. For further details refer to the [documentation of LibSassHost](https://github.com/Taritsyn/LibSassHost#installation).
  
#### 2. Register bundling services

Add the following to the *ConfigureServices* method in your *Startup* class:

    services.AddBundling()
        .UseDefaults(_env) // see below
        .UseNUglify() // or .UseWebMarkupMin()
        .AddLess(); // to enable Less support
        .AddSass(); // to enable Sass/Scss support
        .EnableCacheHeader(TimeSpan.FromDays(1)); // to enable client-side caching

The *_env* field contains the current hosting environment. You can inject it in the constructor like this:

    readonly IHostingEnvironment _env;

    public Startup(IConfiguration configuration, IHostingEnvironment env)
    {
        Configuration = configuration;
        _env = env;
    }

*UseDefaults* adds support for Css and Js and sets the default transformations, enables hash-based cache busting and memory caching. When hosting environment is set to *Development*, it enables change detection, otherwise enables minification.

If you want to switch to file system-backed caching, call *UseFileSystemCaching()* on the builder (after *UseDefaults*). Besides that, there are some further settings available on the builder.

#### 3. Configure bundles

Bundles are defined in the *Configure* method of the *Startup* class in the following manner:

    app.UseBundling(bundles =>
    {
        // loading bundles from json config file
        bundles.LoadFromConfigFile("/bundleconfig.json", _env.ContentRootFileProvider);

        // defining a Css bundle (you can use patterns to include/exclude files)
        bundles.AddCss("/virtual-path/to/bundle.css")
            .Include("/physical-path/to/include.css")
            .Include("/another/physical-path/to/pattern*.css")
            .Exclude("/**/*.min.css");

        // defining a Less bundle (you should include entry point files only)
        bundles.AddLess("/virtual-path/to/less-bundle.css")
            .Include("/physical-path/to/main.less");
            
        // defining a Sass bundle (you should include entry point files only)
        bundles.AddSass("/virtual-path/to/scss-bundle.css")
            .Include("/physical-path/to/main.scss");

        // defining a Js bundle
        bundles.AddJs("/virtual-path/to/bundle.js")
            .Include("/physical-path/to/*.js");
    });

*UseBundling* adds a middleware to the ASP.NET Core request pipeline. You can consider it as a static files middleware, so you need to place it after the exception handler middleware but before the MVC middleware (and probably before authentication or authorization middlewares).

By default, the bundle URL paths will be prefixed with */bundles* (this can be changed by supplying options to the *UseBundling* method) so the bundle registered to */virtual-path/to/bundle.css* will be accessible at *~/bundles/virtual-path/to/bundle.css*. **You need to supply prefixed paths when you referencing bundles in the Razor views!** (Since even multiple bundling middlewares can be registered with different prefixes. ;)

It's also important to **include the proper file extension** (which corresponds to the outputted content) in the bundle path, otherwise the file won't be served. (Or you may mess with the options and supply another *IContentTypeProvider* but I don't think it's worth it... :)

By default, the include (and exclude) file paths are relative to the web root folder (this can be changed again by supplying another *IFileProvider* in the options). You can use the ordinary globbing patterns [supported by the .NET Core file providers](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers#globbing-patterns).

Tip: instead of inefficiently adding excludes to remove pre-minified files, you may consider to implement an *IFileBundleSourceFilter* and add it as a global file filter.

#### 4. Configure Razor views

In order to enable the bundling tag helpers you need to include the following in your *_ViewImports.cshtml*:

    @using Karambolo.AspNetCore.Bundling.ViewHelpers
    @addTagHelper *, Karambolo.AspNetCore.Bundling

#### 5. Referencing bundles in Razor views

It's recommended to use helpers as follows:

    <!DOCTYPE html>
    <html>
    <head>
        @* [...] *@

        @* referencing stylesheet using tag helper *@
        <link rel="stylesheet" href="~/bundles/virtual-path/to/bundle.css" />

        @* referencing stylesheet using static helper *@
        @await Styles.RenderAsync("~/bundles/virtual-path/to/less-bundle.css")
    </head>

    <body>
        @* [...] *@

        @* referencing script using tag helper *@
        <script src="~/bundles/virtual-path/to/bundle.js"></script>

        @* referencing script using static helper *@
        @await Scripts.RenderAsync("~/bundles/virtual-path/to/bundle.js")

        @RenderSection("Scripts", required: false)
    </body>
    </html>

### Advanced features

Check out the [DynamicBundle demo](https://github.com/adams85/bundling/tree/master/samples/DynamicBundle) to get an idea what you can use dynamic parameterized bundles for.

### Bundling middleware settings

The behavior of the bundling middleware can be tweaked by passing a *BundlingOptions* instance to the *UseBundling* method.

#### Reference

| | Description | Default value |
|---|---|---|
| SourceFileProvider | The provider used by file sources. | IHostingEnvironment.  WebRootFileProvider |
| StaticFilesRequestPath | The path prefix used when doing URL rebasing of static files. | none |
| RequestPath | The path prefix added to bundle URLs. It may be empty but not recommended as in that case identifying non-bundle requests requires some additional steps. | "/bundles" |
| ContentTypeProvider | Used to map files to content-types. | |
| DefaultContentType | The default content type for a request if the ContentTypeProvider cannot determine one. | none |
| ServeUnknownFileTypes| Specifies if files of unrecognized content-type should be served. | false |
| OnPrepareResponse | This can be used to add or change the response headers. | |

### Bundle settings

Bundle settings can be configured on multiple levels: globally, per bundle type, per bundle, etc. Settings made on a higher level is effective until it's overridden on a lower level. Technically, this means if a property is set to null, the corresponding setting value will be inherited from the higher level (if any). If it's set to a non-null value, the setting will be effective on the current and lower levels.

#### Global settings

These can be configured when registering services using the *AddBundling* method. You may specify them by supplying a configuration delegate as its argument. Some of them can be specified using fluent API.

#### Per bundle type settings

These can be set similarly when calling the *AddCss*, *AddJson*, etc. extensions methods on the builder object returned by *AddBundling*.

#### Per bundle settings

If you need more fine-grained control, you can specify settings for a single bundle when defining them in the *UseBundling* using fluent API.

#### Reference

|  | Description | Default value | Global | Per type | Per bundle | Per item |
|---|---|---|:---:|:---:|:---:|:---:|
| EnableMinification | Enables minification globally. If set to true, minification transformations will be added to the pipeline by default. | false | X |  |  |  |
| EnableChangeDetection| Enables change detection. | false | X |  |  |  |
| EnableCacheHeader | Enables client-side caching. If set to true, the Cache-Control HTTP header will be sent automatically. | false | X |  |  |  |
| CacheHeaderMaxAge | Specifies the max-age value the Cache-Control HTTP header. | undefined | X |  |  |  |
| Builder | The object responsible to produce the output of the bundle. | | X | X  | X |  |
| FileFilters | Objects that filters or sorts the input file list provided by file sources. | | X | X  | X |  |
| ItemTransforms | Transformations that are applied to each input item. This can even be set on item level. | | X | X  | X | X |
| Transforms | Transformations that are applied to the concatenated output of input item transformations. | | X | X  | X |  |
| ConcatenationToken | The string used to concatenate the outputs of input item transformations. | "\n" for Css outputs<br/>";\n" for Js outputs |  | X  | X |  |
| CacheOptions | Options for bundle output caching. | cache with no expiration |  | | X |  |
| DependsOnParams | Declares that the query string is relevant for caching as the bundle use its parameters to provide its output. | false |  | | X |  |
| OutputEncoding | Encoding of the output. | UTF-8 | | | X |  |
| InputEncoding | Encoding of the input. | auto-detect, fallback to UTF-8 | | | | X |

----------

### Planned features

- css imports inlining (currently less or sass bundles can be used as a workaround)
- change tracking of css / less / sass imports ?
- html bundling ?

### TODO

- documentation (vs xml docs, settings)
- increase unit test code coverage

----------

### *Any feedback appreciated, contributions are welcome!*
Feel free to contact me at [Gitter](https://gitter.im/Karambolo-AspNetCore-Bundling/Lobby?utm_source=share-link&utm_medium=link&utm_campaign=share-link).
