| :mega: Important notices |
|--------------|
| If you use the design-time features of the library, please note that version 3.0 switches to the Global / Local .NET Core CLI extensibility model. See the [documentation](#design-time-mode) for further information. |

# Karambolo.AspNetCore.Bundling

This library can be used to bundle and optimize web assets of ASP.NET Core 2+ applications. Primarily, it was developed as a .NET Core replacement for the *System.Web.Optimization* library of classic ASP.NET, which provides these features at run-time. However, starting with version 2.0, *webpack*-like design-time usage mode is also supported in the form of a .NET Core CLI extension tool.

[![NuGet Release](https://img.shields.io/nuget/v/Karambolo.AspNetCore.Bundling.svg)](https://www.nuget.org/packages/Karambolo.AspNetCore.Bundling/)
[![Donate](https://img.shields.io/badge/-buy_me_a%C2%A0coffee-gray?logo=buy-me-a-coffee)](https://www.buymeacoffee.com/adams85)

### Main features
- **CSS minification and bundling** including support for CSS pre-processors:
  - **LESS compilation** (powered by [dotLess](https://github.com/dotless/dotless)).
  - **SASS/SCSS compilation** (powered by [LibSassHost](https://github.com/Taritsyn/LibSassHost)).
- **JavaScript minification and bundling**. Version 2.0 adds support for **rewriting and bundling ES6 (ECMAScript 2015) modules** (built on [Acornima](https://github.com/adams85/acornima)).
- Straightforward and flexible configuration:
  - Fluent API configuration.
  - Hierarchical configuration system (settings adjustable on general and more detailed levels).
  - **Compatibility with [bundleconfig.json](https://github.com/madskristensen/BundlerMinifier#bundleconfigjson "bundleconfig.json")**.
- Full control over server and client-side caching (including cache busting). **Memory** and **file system cache** implementations are included.
- Replaceable backing storage by leveraging [.NET Core file system abstractions](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers).
- **Automatic refresh of affected bundles** on change of input files. As of version 2.0, not only the directly referenced but **also the imported files are watched** (in the case of LESS, SASS/SCSS and ES6 module bundles).
- Razor tag helpers and the familiar, *System.Web.Optimization*-like API can be used, as well.
- Correct handling of URL path prefixes (app branch prefix, static files middleware prefix, etc.)
- Fully customizable transformation pipelines.
- **Dynamic content sources** and **query string parameterized bundles**.
- Modular design, extensibility.

### Table of Contents

[Quick Start](#quick-start)  

[Installation](#installation)  

[Run-time mode](#run-time-mode)  
[1. Register bundling services](#1-register-bundling-services)  
[2. Define bundles](#2-define-bundles)  
[3. Configure Razor views](#3-configure-razor-views)  
[4. Reference bundles in Razor views](#4-reference-bundles-in-razor-views)  

[Design-time mode](#design-time-mode)  
[1. Define bundles](#1-define-bundles)  
[2. Produce bundles](#2-produce-bundles)  

[Reference](#reference)  
[a. Bundling middleware settings](#a-bundling-middleware-settings)  
[b. Bundle settings](#b-bundle-settings)  

[Samples](#samples)  

### Quick Start

If you want to get a quick overview of the capabilities of the library, check out [this sample project](https://github.com/adams85/bundling/tree/master/samples/QuickStartTemplate). This is a slightly modified version of the [*ASP.NET Core Web App* template of the .NET SDK](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates), so you can even use it as a starting point for your web application projects.

### Installation

The *Karambolo.AspNetCore.Bundling* package contains the core components and interfaces but no actual implementation. Therefore you need to install additional packages depending on your preferences/requirements.

#### Minimizer

 You can choose between implementations using [NUglify](https://github.com/xoofx/NUglify) and [WebMarkupMin](https://github.com/Taritsyn/WebMarkupMin) currently:

    dotnet add package Karambolo.AspNetCore.Bundling.NUglify

or

    dotnet add package Karambolo.AspNetCore.Bundling.WebMarkupMin

#### CSS pre-processor

If you want to use CSS pre-processor features, you will also need one of the following packages:

- LESS

      dotnet add package Karambolo.AspNetCore.Bundling.Less

- SASS/SCSS

      dotnet add package Karambolo.AspNetCore.Bundling.Sass

  Note: The current implementation uses *LibSassHost* under the hood, which is a wrapper around [LibSass](https://sass-lang.com/libsass), an unmanaged library written in C/C++. Therefore you need to install an additional NuGet package which contains this native dependency compiled to your target platform. E.g. on Windows x64 systems: `dotnet add package LibSassHost.Native.win-x64`. For further details refer to the [documentation of LibSassHost](https://github.com/Taritsyn/LibSassHost#installation).

#### ES6 module bundler

Finally, if you want to bundle ES6 modules, you need to install an additional package:

    dotnet add package Karambolo.AspNetCore.Bundling.EcmaScript

Note: ES6 module bundling is built on [Acornima](https://github.com/adams85/acornima), which supports [language features](https://exploringjs.com/impatient-js/ch_new-javascript-features.html) up to ECMAScript 2023 currently. If you want to utilize even newer features (or you just want to target an older JavaScript version), you may use TypeScript for down-level compilation. (See the [TypeScriptDemo sample](https://github.com/adams85/bundling/tree/master/samples/TypeScriptDemo).)

 ### Run-time mode

Choosing this method, your bundles are built on-demand during the execution of your application. Of course, the produced bundles are cached so this is a one-time cost only. In return you get some unique features like dynamic bundles which are not possible when building bundles at design-time.

To set up run-time bundling, you need to walk through the following steps:

#### 1. Register bundling services

Add the following to the `ConfigureServices` method of your `Startup` class:

    services.AddBundling()
        .UseDefaults(Environment) // see below
        .UseNUglify() // or .UseWebMarkupMin() - whichever minifier you prefer
        .AddLess() // to enable LESS support
        .AddSass() // to enable SASS/SCSS support
        .AddEcmaScript() // to enable support for ES6 modules
        .EnableCacheHeader(TimeSpan.FromDays(1)); // to enable client-side caching

The `Environment` property should return the current hosting environment. You can inject it in the constructor like this:

    // In versions older than ASP.NET Core 3, use IHostingEnvironment instead of IWebHostEnvironment
    
    public IWebHostEnvironment Environment { get; }

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
    }

`UseDefaults` adds support for CSS and JavaScript bundles, sets up the default transformations and enables memory caching.

When hosting environment is set to *Development*, `UseDefaults`
* enables change detection (cache invalidation on change of source files) and
* enables including of source files instead of the actual bundled output (of course, this will apply only to bundles which allow this), 

otherwise
* enables minification.

If you want to switch to file system-backed caching, call `UseFileSystemCaching()` on the builder (after `UseDefaults`).

Besides that, there are some further settings available to tweak on the builder.

#### 2. Define bundles

Bundles are defined in the `Configure` method of the `Startup` class in the following manner:

    app.UseBundling(bundles =>
    {
        // loads bundles from a BundlerMinifier config file
        bundles.LoadFromConfigFile("/bundleconfig.json", _env.ContentRootFileProvider);

        // defines a CSS bundle (you can use globbing patterns to include/exclude files)
        bundles.AddCss("/virtual-path/to/bundle.css")
            .Include("/physical-path/to/include.css")
            .Include("/another/physical-path/to/pattern*.css")
            .Exclude("/**/*.min.css");

        // defines a LESS bundle (you should include entry point files only)
        bundles.AddLess("/virtual-path/to/less-bundle.css")
            .Include("/physical-path/to/main.less");

        // defines an SCSS bundle (you should include entry point files only)
        bundles.AddSass("/virtual-path/to/scss-bundle.css")
            .Include("/physical-path/to/main.scss");

        // defines a JavaScript bundle
        bundles.AddJs("/virtual-path/to/bundle.js")
            .Include("/physical-path/to/*.js");
            //.EnableEs6ModuleBundling(); - uncomment this line if you want the included files to be treated as ES6 modules (include only entry point file(s) in this case!)
    });

`UseBundling` adds a middleware to the ASP.NET Core request pipeline. You can consider it as a static files middleware, so you need to place it after the exception handler middleware but before the MVC middleware (and probably before authentication or authorization middlewares).

The behavior of the middleware can be customized by supplying a `BundlingOptions` instance to the `UseBundling` method. The [possible settings](#a-bundling-middleware-settings) are listed in the Reference section.

By default, the bundle URL paths will be prefixed with */bundles* (this can be changed through the `RequestPath` option though), so the bundle registered to */virtual-path/to/bundle.css* will be accessible at *~/bundles/virtual-path/to/bundle.css*. **However, you need to specify prefixed paths when you reference bundles in the Razor views!** (Since even multiple bundling middlewares can be registered with different prefixes.)

It's also important to **include the proper file extension** (which corresponds to the outputted content) in the bundle path, otherwise the file won't be served. (Or you may mess with the options and supply another `IContentTypeProvider`, but that would be quite an unusual use case.)

By default, the include (and exclude) file paths are relative to the *wwwroot* folder (to be precise, to the root path of the file provider specified by the `WebRootFileProvider` property of the current `IWebHostEnvironment`). It's important to keep in mind that **the file provider abstraction doesn't allow to access files located outside its root path**! In such cases you need to create another `PhysicalFileProvider` with the right root path and pass that in by the `SourceFileProvider` option.

When specifying includes/excludes, you can use the ordinary globbing patterns [supported by the .NET Core file providers](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers#globbing-patterns).

***Tip:*** instead of inefficiently adding excludes to remove some particular (like pre-minified) files, you may consider to implement an `IFileBundleSourceFilter` and add it as a global file filter.

#### 3. Configure Razor views

In order to enable the bundling tag helpers you need to include the following lines in your *_ViewImports.cshtml*:

    @using Karambolo.AspNetCore.Bundling.ViewHelpers
    @addTagHelper *, Karambolo.AspNetCore.Bundling

#### 4. Reference bundles in Razor views

Bundles can be referenced by tag helpers and static helper methods as well:

    <!DOCTYPE html>
    <html>
    <head>
        @* [...] *@

        @* references stylesheet bundle using tag helper *@
        <link rel="stylesheet" href="~/bundles/virtual-path/to/bundle.css" />

        @* references stylesheet bundle using static helper *@
        @await Styles.RenderAsync("~/bundles/virtual-path/to/less-bundle.css")
    </head>

    <body>
        @* [...] *@

        @* references script bundle using tag helper *@
        <script src="~/bundles/virtual-path/to/bundle.js"></script>

        @* references script bundle using static helper *@
        @await Scripts.RenderAsync("~/bundles/virtual-path/to/bundle.js")
    </body>
    </html>

### Design-time mode

Starting with version 2.0, it's possible to pre-build bundles at design-time. This is similar to how *webpack* or Mads Kristensen's [BundlerMinifer](https://github.com/madskristensen/BundlerMinifier) works.

Just like in the case of run-time bundling, install the required NuGet packages first. On top of that you need to **make sure that the core library is referenced** as well:

    dotnet add package Karambolo.AspNetCore.Bundling

(This package contains some files which are necessary to extend the build process and make the design-time method work.)

Prior to version 3.0 the step above automatically enabled the .NET Core CLI extension `dotnet bundle` by adding the necessary [DotNetCliToolReference](https://docs.microsoft.com/en-us/dotnet/core/tools/extensibility) to your project. However, [*DotNetCliToolReference* is more or less obsolete now](https://github.com/dotnet/sdk/issues/3115), so version 3.0 switches to [the newer approach introduced in NET Core 2.1](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools). Thus, **from version 3.0 on, you also need to install the .Net Core CLI extension manually**. In return, you have multiple options to choose from:

* You can make the tool globally available on your development machine.

      dotnet tool install -g dotnet-bundlingtools
       
* You can install the tool into a specific directory.

      dotnet tool install --tool-path .tools dotnet-bundlingtools
       
* You can make the tool available in your project only. (Please note that [Local Tools](https://stu.dev/dotnet-core-3-local-tools/) are available since .NET Core 3 only.)

      dotnet new tool-manifest
      dotnet tool install dotnet-bundlingtools

After installing the necessary components, check if everything has been set up correctly: issue the `dotnet bundle --version` command in your project folder.

#### 1. Define bundles

First you need to describe your bundles, which can be done in two ways currently. (These methods are not exclusive, you can even specify multiple configurations.)

##### 1.a. Defining bundles by configuration file

You may define your bundles by placing a *bundleconfig.json* file in the root folder of your project. Originally, this format was established by Mads Kristensen's *BundlerMinifier* library but it is supported here as well. For details, please refer to the [original documentation](https://github.com/madskristensen/BundlerMinifier#bundleconfigjson).

##### 1.b. Defining bundles by code

In many cases configuration files are sufficient to define your bundles, but there are some features and settings which are not accessible using this approach. When you need full control over configuration, you have the option to specify it by code. All you need to do is create a class (with a default constructor) inheriting from `DesignTimeBundlingConfiguration` in your ASP.NET Core application:

    public class MyBundles : DesignTimeBundlingConfiguration
    {
        public MyBundles() { }

        // this property should return an enumeration of the used modules
        public override IEnumerable<IBundlingModule> Modules => base.Modules // CSS and JavaScript modules are already added by the base class
            .Append(new NUglifyBundlingModule()) // or .Append(new WebMarkupMinBundlingModule()) - whichever minifier you prefer
            .Append(new LessBundlingModule()) // to enable LESS support
            .Append(new SassBundlingModule()) // to enable SASS/SCSS support
            .Append(new EcmaScriptBundlingModule()); // to enable support for ES6 modules

        // in this method you can define your bundles using fluent API
        public override void Configure(BundleCollectionConfigurer bundles)
        {
            // defines a CSS bundle (you can use globbing patterns to include/exclude files)
            bundles.AddCss("/virtual-path/to/bundle.css")
                .Include("/physical-path/to/include.css")
                .Include("/another/physical-path/to/pattern*.css")
                .Exclude("/**/*.min.css");

            // defines a LESS bundle (you should include entry point files only)
            bundles.AddLess("/virtual-path/to/less-bundle.css")
                .Include("/physical-path/to/main.less");

            // defines an SCSS bundle (you should include entry point files only)
            bundles.AddSass("/virtual-path/to/scss-bundle.css")
                .Include("/physical-path/to/main.scss");

            // defines a JavaScript bundle
            bundles.AddJs("/virtual-path/to/bundle.js")
                .Include("/physical-path/to/*.js");
            //.EnableEs6ModuleBundling(); - uncomment this line if you want the included files to be treated as ES6 modules (include only entry point file(s) in this case!)
        }
    }

When subclassing the abstract `DesignTimeBundlingConfiguration` class, you have to provide an implementation for the `Configure` method as shown in the example.

You usually need modules other than the core ones (CSS, JavaScript). If so, these modules need to be specified by overriding the `Modules` property.

There are additional properties which you can override to tweak global settings. For details, see the [Bundle settings](#b-bundle-settings) reference.

#### 2. Produce bundles

Depending on your workflow, you have the following three options to process the configuration(s) and create the specified bundles:

##### 2.a. Producing bundles manually

Execute the `dotnet bundle` command in your project folder and that's it.

By default, the CLI tool
- checks for a configuration file (*bundleconfig.json* in the project folder) and 
- looks for code configuration in your application as well. 

To be able to do the latter, the tool has to build your project first. If you don't use code configuration, you may rather use the `dotnet bundle --sources ConfigFile` command. This way, the tool skips building your application.

##### 2.b. Updating bundles on build

You can easily setup your application to automatically update your bundles when you build it. Just insert these several lines under the root element (*Project*) of your project (csproj) file:

    <PropertyGroup>
      <BundleOnBuild>true</BundleOnBuild>
    </PropertyGroup>

When (re)building your project the next time, you should see the processed bundle configuration(s) in the build output.

Actually, no magic happens under the hood, just the CLI tool is invoked as a part of the build process. Because of that, you can supply the same options as if you executed it manually, you just need to use MSBuild properties:

| MSBuild property | CLI tool option |
|---|---|
| BundlingConfigSources | `--sources` |
| BundlingConfigFile | `--config-file` |
| BundlingMode | `--mode` |

##### 2.c. Updating bundles on change of input files

This use case is not supported out-of-the-box currently, but you can make it work by the help of another CLI tool: `dotnet watch`.

Web assets are not monitored by default, so you need to add something like this to your project file first:

    <ItemGroup>
      <Watch Include="wwwroot\**\*" Exclude="wwwroot\bundles\**\*;$(DefaultExcludes)"  />
    </ItemGroup>

(For the exact configuration and capabilities of the *watch* tool, please refer to [its official documentation](https://docs.microsoft.com/en-us/aspnet/core/tutorials/dotnet-watch).)

Then you can start monitoring by issuing the `dotnet watch bundle --no-build` command in the project folder.

### Reference

#### a. Bundling middleware settings

The behavior of the bundling middleware can be tweaked by passing a `BundlingOptions` instance to the `UseBundling` method.

| | Description | Default value |
|---|---|---|
| SourceFileProvider | The file provider to use to access input files. | `IWebHostEnvironment.​WebRootFileProvider` |
| CaseSensitiveSourceFilePaths | Specifies if paths to input files should be treated as case-sensitive. | false when *SourceFileProvider* abstracts a physical file system on Windows, otherwise true |
| StaticFilesRequestPath | The path prefix to use when doing URL rebasing of other referenced web assets such as images, fonts, etc. | none |
| RequestPath | The path prefix to add to bundle URLs. It may be empty but that is not recommended as in that case identification of non-bundle requests requires some additional steps. | "/bundles" |
| ContentTypeProvider | Used to map files to content-types. | |
| DefaultContentType | The default content type for a request if *ContentTypeProvider* cannot determine one. | none |
| ServeUnknownFileTypes| Specifies if files of unrecognized content-type should be served. | false |
| OnPrepareResponse | This can be used to add or change the response headers. | |

#### b. Bundle settings

Bundle settings can be configured on multiple levels: globally, per bundle type, per bundle and per bundle item.

Settings made on a higher (more general) level is effective until it's overridden on a lower (more detailed) level. Technically, this means if a property is set to null, the corresponding setting value will be inherited from the higher level (if any). If it's set to a non-null value, the setting will be effective on the current and lower levels.

The configuration levels from high to low:

1. Global settings

   * In run-time mode, these settings can be configured when registering services using the `AddBundling` method. You may tweak them by a configuration delegate passed in to the mentioned method. Some of them can be specified using fluent API, as well. 

   * In design-time mode, you configure these settings by overriding the corresponding properties of the `DesignTimeBundlingConfiguration` class.

1. Per bundle type settings

   * In run-time mode, these settings can be set also using configuration delegates when calling the `AddCss`, `AddJs`, etc. extensions methods on the builder returned by `AddBundling`.

   * In design-time mode, the configuration delegates can be passed to the constructors of modules when overriding the `Modules` property of the `DesignTimeBundlingConfiguration` class.

1. Per bundle settings

   If you need more detailed control, you can specify settings for a single bundle using fluent API when defining them in `UseBundling` / `DesignTimeBundlingConfiguration.Configure`.

1. Per bundle item settings

   There are some settings which can even be tweaked for items of bundles. These can be set by the optional parameters of the `Include` method available on the builder returned by `AddCss`, `AddJs`, etc.

##### Overview of settings

|  | Description | Default value | Global | Per type | Per bundle | Per item |
|---|---|---|:---:|:---:|:---:|:---:|
| EnableMinification | Enables minification globally. If set to true, minification transformations will be added to the pipeline by default. | false | X |  |  |  |
| EnableChangeDetection| Enables change detection of the source files. On change the cache for the involved bundles will be invalidated. | false | X |  |  |  |
| EnableCacheBusting| Enables cache busting. If set to true, a version part will be added to the include URLs. (This global setting can be overrided for individual includes. Look for the `bundling-add-version` attribute or the `addVersion` parameter of the tag helper or static helper methods respectively.) | false when `IWebHostEnvironment.​EnvironmentName` is equal to "Development", otherwise true | X |  |  |  |
| EnableCacheHeader | Enables client-side caching. If set to true, the Cache-Control HTTP header will be sent automatically. | false | X |  |  |  |
| CacheHeaderMaxAge | Specifies the max-age value the Cache-Control HTTP header. | undefined | X |  |  |  |
| EnableSourceIncludes | Enables including of source files instead of the actual bundled output. This is useful during development, however it's only possible when the bundle has no substantial transformations (such as pre-processing). | false | X | X | X |  |
| SourceItemToUrlMapper | Used to map source items to URLs when rendering source includes. | [a default mapper](https://github.com/adams85/bundling/blob/b5c857658240c879caece8ea40cdc8839909485f/source/Bundling/BundleGlobalOptions.cs#L37) which is able to determine URLs of files located within the root directory of `IWebHostEnvironment.​WebRootFileProvider` | X | X | X |  |
| StaticFileUrlToFileMapper | Used to map URLs to file provider files when file versions for pre-bundled or non-bundle files need to be computed. You only need to consider this option if you want cache busting for such files and those are not accessible at the standard location (e.g. you use some prefix for your static files). | [a default mapper](https://github.com/adams85/bundling/blob/b5c857658240c879caece8ea40cdc8839909485f/source/Bundling/BundleGlobalOptions.cs#L43) which maps local URLs to files within `IWebHostEnvironment. WebRootFileProvider`. | X |  |  |  |
| Builder | The object responsible to produce the output of the bundle. | | X | X  | X |  |
| FileFilters | Objects that filters or sorts the input file list provided by file sources. | | X | X  | X |  |
| ItemTransforms | Transformations that are applied to each input item. (This can even be set on item level.) | | X | X  | X | X |
| Transforms | Transformations that are applied to the concatenated output of input item transformations. | | X | X  | X |  |
| ConcatenationToken | The string to use to concatenate the outputs of input item transformations. | "\n" for Css outputs<br/>";\n" for Js outputs |  | X  | X |  |
| CacheOptions | Options for bundle output caching. | cache with no expiration |  | | X |  |
| DependsOnParams | Declares that the query string is relevant for caching as the bundle use it to provide its output. | false |  | | X |  |
| OutputEncoding | Encoding of the output. | UTF-8 | | | X |  |
| InputEncoding | Encoding of the input. | auto-detect, fallback to UTF-8 | | | | X |

### Samples

If you're new to the library or want to learn more about its capabilities, I suggest looking around [here](https://github.com/adams85/bundling/tree/master/samples) where you find some simple demo applications like:

#### VueDemo

[This sample](https://github.com/adams85/bundling/tree/master/samples/VueDemo) shows how you can setup a component-based Vue.js application. (This one uses vanilla JavaScript and doesn't use ES6 modules but it can be easily re-configured by examining the setup of the [TypeScriptDemo app](https://github.com/adams85/bundling/tree/master/samples/TypeScriptDemo).)

#### DynamicBundle

Check out [this demo](https://github.com/adams85/bundling/tree/master/samples/DynamicBundle) to get an idea how dynamic parameterized bundles works and what they can be used for.

### *Any feedback appreciated, contributions are welcome!*
