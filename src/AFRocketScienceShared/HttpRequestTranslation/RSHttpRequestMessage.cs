using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    public class RSHttpRequestMessage : IRocketScienceRequest
    {
        public string Host => _request.RequestUri.Host;
        public string LocalPath => _request.RequestUri.LocalPath;
        public string Key => $"{_request.Method}>{_request.RequestUri.ToString()}";
        public string Content => _content.Value;
        public KeyValuePair<string, string>[] QueryParts => _queryParts.Value;
        public KeyValuePair<string, string>[] Headers => _headerParts.Value;

        Lazy<KeyValuePair<string, string>[]> _queryParts;
        Lazy<KeyValuePair<string, string>[]> _headerParts;
        Lazy<string> _content;

        private HttpRequestMessage _request;

        //--------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        //--------------------------------------------------------------------------------
        public RSHttpRequestMessage(HttpRequestMessage request)
        {
            _request = request;
            _queryParts = new Lazy<KeyValuePair<string, string>[]>(ConstructQueryParts);
            _headerParts = new Lazy<KeyValuePair<string, string>[]>(ConstructHeaderParts);
            _content = new Lazy<string>(() => _request.Content?.ReadAsStringAsync().Result);

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
                outputParts.Add(new KeyValuePair<string,string>(header.Key, string.Join("", header.Value)));  
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
            var queryParts = _request.RequestUri.Query.TrimStart('?').Split('&');
            foreach (var part in queryParts)
            {
                var trimmed = part.Trim();
                if (trimmed == "") continue;
                var subParts = trimmed.Split(new[] { '=' }, 2);
                var processedPart = new KeyValuePair<string, string>(subParts[0], subParts.Length > 1 ? Uri.UnescapeDataString(subParts[1]).Trim() : null);
                outputParts.Add(processedPart);
            }
            return outputParts.ToArray();
        }
    }
}
