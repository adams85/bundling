using System;
using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    internal static class ErrorHelper
    {
        public static ArgumentException ArrayCannotContainNull(string paramName)
        {
            return new ArgumentException($"The array cannot contain null.", paramName);
        }

        public static ArgumentException PropertyCannotBeNull(string paramName, string propertyName)
        {
            return new ArgumentException($"{propertyName} cannot be null.", paramName);
        }

        public static ArgumentException ValueCannotBeEmpty(string paramName)
        {
            return new ArgumentException($"The value cannot be empty.", paramName);
        }

        public static InvalidOperationException HttpContextNotAvailable()
        {
            return new InvalidOperationException("Http context is not available.");
        }

        public static ArgumentException PathMappingNotPossible(string path, string paramName)
        {
            return new ArgumentException($"The specified path mapper cannot map the path '{path}'. The prefix of the path to map may not correspond to your configuration.", paramName);
        }

        public static InvalidOperationException ExtensionNotRecognized(string extension)
        {
            return new InvalidOperationException($"File extension '{extension}' is not recognized.");
        }

        public static InvalidOperationException BundleInfoNotAvailable(PathString path, QueryString query)
        {
            return new InvalidOperationException($"Bundle information for request {path + query} is not available.");
        }

        public static InvalidOperationException ModelFactoryNotAvailable(Type modelType)
        {
            return new InvalidOperationException($"Model factory is not available for {modelType}");
        }

        public static InvalidOperationException PropertyNotSpecifed(string className, string propertyName)
        {
            return new InvalidOperationException($"{className}.{propertyName} is not specified.");
        }

        public static InvalidOperationException ChangeDetectionNotEnabled()
        {
            return new InvalidOperationException("Change detection is not enabled.");
        }

        public static ArgumentException ContentRootNotPhysical(string paramName)
        {
            return new ArgumentException($"When no file provider is supplied, content root file provider must be physical.", paramName);
        }
    }
}
