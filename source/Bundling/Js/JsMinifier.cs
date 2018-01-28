namespace Karambolo.AspNetCore.Bundling.Js
{
    public interface IJsMinifier
    {
        string Process(string content, string filePath);
    }
}
