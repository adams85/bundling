namespace Karambolo.AspNetCore.Bundling.Internal.Versioning
{
    public class NullBundleVersionProvider : IBundleVersionProvider
    {
        public void Provide(IBundleVersionProviderContext context) { }
    }
}
