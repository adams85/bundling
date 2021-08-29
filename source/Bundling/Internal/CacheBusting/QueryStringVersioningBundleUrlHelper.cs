using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.CacheBusting
{
    public class QueryStringVersioningBundleUrlHelper : IBundleUrlHelper
    {
        private const string VersionKey = "v";

        public void AddVersion(string version, ref PathString path, ref QueryString query)
        {
            var builder = new QueryBuilder();

            Dictionary<string, StringValues> parsedQuery;
            if (query.HasValue && (parsedQuery = QueryHelpers.ParseNullableQuery(query.ToString())) != null)
            {
                foreach ((string key, StringValues values) in parsedQuery)
                    if (key != VersionKey)
                        for (int i = 0, n = values.Count; i < n; i++)
                            builder.Add(key, values[i]);
            }

            builder.Add(VersionKey, version);

            query = builder.ToQueryString();
        }

        public string RemoveVersion(ref PathString path, ref QueryString query)
        {
            string version = null;

            var builder = new QueryBuilder();

            Dictionary<string, StringValues> parsedQuery;
            if (query.HasValue && (parsedQuery = QueryHelpers.ParseNullableQuery(query.ToString())) != null)
            {
                foreach ((string key, StringValues values) in parsedQuery)
                    if (key != VersionKey)
                    {
                        for (int i = 0, n = values.Count; i < n; i++)
                            builder.Add(key, values[i]);
                    }
                    else if (values.Count > 0)
                        version = values[0];
            }

            query = builder.ToQueryString();

            return version;
        }
    }
}
