using System.Threading.Tasks;
using System.Net.Http;

namespace Sockets
{
    public class HttpClientHelper : IHttpClientHelper
    {
        public async Task<ResponseWrapper> Send(HttpMethod method, string uri)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(method, uri);
                var response = await client.SendAsync(request);

                var responseMsg = await response.Content.ReadAsStringAsync();
                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                return new ResponseWrapper(responseMsg, response.StatusCode, responseBytes);
            }
        }
    }
}
