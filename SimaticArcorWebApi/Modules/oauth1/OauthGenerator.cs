using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace SimaticWebApi.Modules.oauth1
{
    public class NSOAuth1
    {
        public string method;
        public string url;
        public string ck;
        public string cs;
        public string tk;
        public string ts;
        public string realm;
        public string timestamp;
        public string nonce;
        public bool debugMode;

        public NSOAuth1(string method, string url, string ck, string cs, string tk, string ts, string realm, string timestamp = null, string nonce = null, bool debugMode = false)
        {
            if (string.IsNullOrEmpty(method) || !new[] { "GET", "POST", "PUT", "DELETE" }.Contains(method.ToUpper()))
                throw new ArgumentException("Invalid method, only allowed GET, PUT, POST, DELETE");

            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Invalid URL");

            if (string.IsNullOrEmpty(ck) || string.IsNullOrEmpty(cs) || string.IsNullOrEmpty(tk) || string.IsNullOrEmpty(ts))
                throw new ArgumentException("Invalid Tokens");

            if (string.IsNullOrEmpty(realm))
                throw new ArgumentException("Account must not be empty, it should be in the form of #####_SB# or ######");

            this.method = method;
            this.url = url;
            this.ck = ck;
            this.cs = cs;
            this.tk = tk;
            this.ts = ts;
            this.realm = realm;
            this.timestamp = timestamp;
            this.nonce = nonce;
            this.debugMode = debugMode;
        }

        public string GenerateOAuth()
        {
            try
            {
                string timestamp = this.timestamp ?? GenerateTimeStamp();
                string nonce = this.nonce ?? GenerateNonce();
                string baseString = GetBaseString(new
                {
                    method = this.method,
                    url = this.url,
                    oauth_data = new
                    {
                        consumerKey = this.ck,
                        tokenKey = this.tk,
                        timestamp = timestamp,
                        nonce = nonce
                    },
                    debug_mode = this.debugMode
                });

                string key = $"{Encode(this.cs)}&{Encode(this.ts)}";
                string signature = Encode(Convert.ToBase64String(new HMACSHA256(Encoding.UTF8.GetBytes(key)).ComputeHash(Encoding.UTF8.GetBytes(baseString))));

                string authHeader = $"OAuth realm=\"{this.realm}\", " +
                                    $"oauth_consumer_key=\"{this.ck}\", " +
                                    $"oauth_nonce=\"{nonce}\", " +
                                    $"oauth_signature=\"{signature}\", " +
                                    $"oauth_signature_method=\"HMAC-SHA256\", " +
                                    $"oauth_timestamp=\"{timestamp}\", " +
                                    $"oauth_token=\"{this.tk}\", " +
                                    $"oauth_version=\"1.0\"";

                return authHeader;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string GetBaseString(dynamic data)
        {
            var httpMethod = data.method;
            var url = data.url;
            var oauthData = data.oauth_data;
            var debugMode = data.debug_mode;

            var baseUrl = url.Split('?')[0];
            var querystring = url.Contains('?') ? url.Split('?')[1] : string.Empty;
            var paramsList = querystring.Split('&');

            var parameters = new Dictionary<string, string>();
            foreach (var param in paramsList)
            {
                var keyValue = param.Split('=');
                var key = keyValue[0];
                var value = keyValue[1];
                if (!string.IsNullOrEmpty(key))
                    parameters[key] = value;
            }

            if (debugMode)
                Console.WriteLine(string.Join(", ", parameters));

            var dataDict = new Dictionary<string, string>(parameters)
        {
            { "oauth_consumer_key", oauthData.consumerKey },
            { "oauth_nonce", oauthData.nonce },
            { "oauth_signature_method", "HMAC-SHA256" },
            { "oauth_timestamp", oauthData.timestamp },
            { "oauth_token", oauthData.tokenKey },
            { "oauth_version", "1.0" }
        };

            var sortedKeys = dataDict.Keys.OrderBy(k => k).ToList();

            if (debugMode)
                Console.WriteLine(string.Join(", ", sortedKeys));

            var baseStringBuilder = new StringBuilder($"{httpMethod}&{Encode(baseUrl)}&");

            foreach (var key in sortedKeys)
            {
                var str = $"{key}={dataDict[key]}&";
                if (key == sortedKeys.Last())
                    str = str.TrimEnd('&');
                baseStringBuilder.Append(Encode(str));
            }

            return baseStringBuilder.ToString();
        }

        private string Encode(string str)
        {
            return Uri.EscapeDataString(str)
                .Replace("!", "%21")
                .Replace("*", "%2A")
                .Replace("'", "%27")
                .Replace("(", "%28")
                .Replace(":", "%3A")
                .Replace(")", "%29");
        }

        private string GenerateNonce()
        {
            var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var result = new StringBuilder();
            var random = new Random();
            for (int i = 0; i < 32; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }
            return result.ToString();
        }

        private string GenerateTimeStamp()
        {
            return ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();
        }
    }

}
