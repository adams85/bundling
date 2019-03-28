using System;
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
        private readonly Func<IChangeToken> _factory;

        public FactoryChangeSource(Func<IChangeToken> factory)
        {
            _factory = factory ?? (() => NullChangeToken.Singleton);
        }

        public IChangeToken CreateChangeToken()
        {
            return _factory();
        }

        public bool Equals(IChangeSource other)
        {
            return
                other is FactoryChangeSource otherSource ?
                _factory == otherSource._factory :
                false;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FactoryChangeSource);
        }

        public override int GetHashCode()
        {
            return _factory.GetHashCode();
        }
    }
}
