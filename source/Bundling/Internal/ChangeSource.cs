﻿using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public interface IChangeSource : IEquatable<IChangeSource>
    {
        IChangeToken CreateChangeToken();
    }

    public class FactoryChangeSource : IChangeSource
    {
        private static readonly Func<IChangeToken> s_nullChangeTokenFactory = () => NullChangeToken.Singleton;

        private readonly Func<IChangeToken> _factory;

        public FactoryChangeSource(Func<IChangeToken> factory)
        {
            _factory = factory ?? s_nullChangeTokenFactory;
        }

        public IChangeToken CreateChangeToken()
        {
            return _factory();
        }

        public bool Equals(IChangeSource other)
        {
            return Equals((object)other);
        }

        public override bool Equals(object obj)
        {
            return obj is FactoryChangeSource otherSource && _factory == otherSource._factory;
        }

        public override int GetHashCode()
        {
            return _factory.GetHashCode();
        }
    }
}
