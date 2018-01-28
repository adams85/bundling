namespace Karambolo.AspNetCore.Bundling.Css
{
    public interface ICssMinifier
    {
        string Process(string content, string filePath);
    }
}
