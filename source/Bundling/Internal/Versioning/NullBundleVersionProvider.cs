namespace Karambolo.AspNetCore.Bundling.Internal.Versioning
{
    public class NullBundleVersionProvider : IBundleVersionProvider
    {
        public static readonly NullBundleVersionProvider Instance = new NullBundleVersionProvider();

        NullBundleVersionProvider() { }

        public void Provide(IBundleVersionProviderContext context) { }
    }
}
