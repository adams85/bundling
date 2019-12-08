namespace Karambolo.AspNetCore.Bundling.Tools.Infrastructure
{
    public partial class ProcessRunner
    {
        public static readonly ProcessRunner Default = new ProcessRunner(ReporterAdapter.Default);
    }
}
