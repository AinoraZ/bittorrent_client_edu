using System.Net.Http;
using System.Threading.Tasks;

namespace Sockets
{
    public interface IHttpClientHelper
    {
        Task<ResponseWrapper> Send(HttpMethod method, string uri);
    }
}