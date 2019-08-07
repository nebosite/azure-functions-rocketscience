using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    public class RSHttpRequest: IRocketScienceRequest
    {
        public string Host => _request.Host.Host;
        public string LocalPath => _request.Path;
        public string Key => $"{_request.Method}>{LocalPath}";
        public string Content => _content.Value;
        public KeyValuePair<string, string>[] QueryParts => _queryParts.Value;
        public KeyValuePair<string, string>[] Headers => _headerParts.Value;

        Lazy<KeyValuePair<string, string>[]> _queryParts;
        Lazy<KeyValuePair<string, string>[]> _headerParts;
        Lazy<string> _content;

        HttpRequest _request;

        //--------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        //--------------------------------------------------------------------------------
        public RSHttpRequest(HttpRequest request)
        {
            _request = request;
            _queryParts = new Lazy<KeyValuePair<string, string>[]>(ConstructQueryParts);
            _headerParts = new Lazy<KeyValuePair<string, string>[]>(ConstructHeaderParts);
            _content = new Lazy<string>(() => new StreamReader(_request.Body).ReadToEnd());
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Get at all of the header keyValue pairs
        /// </summary>
        //--------------------------------------------------------------------------------
        KeyValuePair<string, string>[] ConstructHeaderParts()
        {
            var outputParts = new List<KeyValuePair<string, string>>();
            foreach (var header in _request.Headers)
            {
                // concatenate headers that might have been split apart
                outputParts.Add(new KeyValuePair<string, string>(header.Key, string.Join("", header.Value)));
            }
            return outputParts.ToArray();
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Break the query up into keyvalue pairs.
        /// Yes, I know this is provided in system.web.http, but the point of this library
        /// is to avoid that dependency
        /// </summary>
        //--------------------------------------------------------------------------------
        KeyValuePair<string, string>[] ConstructQueryParts()
        {
            /// Yes, I know this is provided in system.web.http, but the point of this library
            /// is to avoid that dependency
            var outputParts = new List<KeyValuePair<string, string>>();
            foreach(var queryItem in _request.Query)
            {
                outputParts.Add(new KeyValuePair<string, string>(queryItem.Key, string.Join("", queryItem.Value)));
            }
            return outputParts.ToArray();
        }

    }
}
