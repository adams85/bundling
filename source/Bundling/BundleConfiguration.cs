﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IBundleGlobalConfiguration
    {
        IBundleBuilder Builder { get; }
        IReadOnlyList<IFileBundleSourceFilter> FileFilters { get; }
        IReadOnlyList<IBundleItemTransform> ItemTransforms { get; }
        IReadOnlyList<IBundleTransform> Transforms { get; }
    }

    public abstract class BundleGlobalDefaultsOptions : IBundleGlobalConfiguration
    {
        public IBundleBuilder Builder { get; set; }
        public IReadOnlyList<IFileBundleSourceFilter> FileFilters { get; set; }
        public IReadOnlyList<IBundleItemTransform> ItemTransforms { get; set; }
        public IReadOnlyList<IBundleTransform> Transforms { get; set; }
    }

    public interface IBundleConfiguration : IBundleGlobalConfiguration
    {
        IBundleGlobalConfiguration GlobalDefaults { get; }
        string Type { get; }
        string ConcatenationToken { get; }

        IConfigurationHelper ConfigurationHelper { get; }
    }

    public class BundleDefaultsOptions : BundleGlobalDefaultsOptions, IBundleConfiguration
    {
        public IBundleGlobalConfiguration GlobalDefaults { get; set; }
        public string Type { get; set; }
        public string ConcatenationToken { get; set; }

        public IConfigurationHelper ConfigurationHelper { get; set; }
    }

    public abstract class BundleDefaultsConfigurerBase<TDefaults> : IConfigureNamedOptions<TDefaults>
        where TDefaults : BundleGlobalDefaultsOptions
    {
        private readonly Action<TDefaults, IServiceProvider> _action;

        protected BundleDefaultsConfigurerBase(Action<TDefaults, IServiceProvider> action, IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            _action = action;
            ServiceProvider = serviceProvider;
        }

        protected IServiceProvider ServiceProvider { get; }

        protected abstract string Name { get; }

        public void Configure(TDefaults options)
        {
            Configure(Options.DefaultName, options);
        }

        protected abstract void SetDefaults(TDefaults options);

        public void Configure(string name, TDefaults options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (name == Name)
            {
                SetDefaults(options);
                _action?.Invoke(options, ServiceProvider);
            }
        }
    }
}
