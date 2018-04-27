using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RightpointLabs.BotLib.Services
{
    public abstract class SimpleServiceBase : IDisposable
    {
        protected abstract Uri Url { get; }
        private HttpClient _client = null;

        protected virtual async Task<T> MakeRequest<T>(Func<HttpClient, Task<HttpResponseMessage>> makeRequest, Func<HttpResponseMessage, Task<T>> handleResponse)
        {
            if (null == _client)
            {
                var h = new HttpClientHandler()
                {
                    AllowAutoRedirect = false,
                    CookieContainer = await GetCookieContainer(),
                    Credentials = await GetCredentials()
                };
                var c = new HttpClient(h);
                AddAuthentication(c);
                if (null != Interlocked.CompareExchange(ref _client, c, null))
                {
                    c.Dispose();
                    h.Dispose();
                }
            }

            return await handleResponse(await makeRequest(_client));
        }

        /// <summary>
        /// Makes the actual GET request to the server.
        /// </summary>
        protected virtual async Task<T> GetJson<T>(string url) where T : JToken
        {
            return await MakeRequest(client => client.GetAsync(new Uri(Url, url).AbsoluteUri), async resp => 
            {
                using (var s = await resp.Content.ReadAsStreamAsync())
                {
                    using (var tr = new StreamReader(s))
                    {
                        using (var jr = new JsonTextReader(tr))
                        {
                            return (T)JToken.Load(jr);
                        }
                    }
                }
            });
        }

        protected virtual async Task<(string, byte[])> GetData(string url)
        {
            return await MakeRequest(client => client.GetAsync(new Uri(Url, url).AbsoluteUri), async resp =>
            {
                resp.EnsureSuccessStatusCode();

                var contentType = resp.Content.Headers.ContentType.MediaType;
                var data = await resp.Content.ReadAsByteArrayAsync();

                return (contentType, data);
            });
        }

        /// <summary>
        /// Override this to provide credentials for all calls.
        /// </summary>
        protected virtual async Task<ICredentials> GetCredentials()
        {
            return null;
        }

        /// <summary>
        /// Override this to provide the initial cookies for all calls.
        /// </summary>
        protected virtual async Task<CookieContainer> GetCookieContainer()
        {
            return new CookieContainer();
        }
        
        /// <summary>
        /// Override to provide any pre-request authentication for all calls.
        /// </summary>
        protected virtual void AddAuthentication(HttpClient c)
        {
        }

        protected async Task<T> Get<T>(string url)
        {
            return (await GetJson<JToken>(url)).ToObject<T>();
        }

        public void Dispose()
        {
            if (null != _client)
            {
                _client.Dispose();
                _client = null;
            }
        }
    }
}