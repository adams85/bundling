namespace Karambolo.AspNetCore.Bundling
{
    public interface IExtensionMapper
    {
        IBundleConfiguration MapInput(string extension);
        IBundleConfiguration MapOutput(string extension);
    }
}
