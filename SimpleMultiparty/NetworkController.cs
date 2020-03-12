using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using System.Diagnostics;
using System;

namespace HttpRequest
{
    class NetworkController
    {

        public static async Task<RequestSession> GetTokboxSession(string path)
        {
            RequestSession session = null;
            try
            {
                RestClient client = new RestClient(path) { Timeout = -1 };
                RestRequest request = new RestRequest(Method.GET);
                IRestResponse response = await client.ExecuteAsync(request);
                if (response.IsSuccessful)
                {
                    Trace.WriteLine("--->>>" + response.Content);
                    session = JsonConvert.DeserializeObject<RequestSession>(response.Content);
                }
                else
                {
                    Trace.WriteLine("Error con peticion!" + response.StatusCode);
                }
                return session;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("ERROR--->>>" + ex.ToString());

                return session;
            }
        }
    }

    public class RequestSession
    {
        public TokboxSession tokbox { get; set; }
        public TokboxSession tokboxUDP { get; set; }
    }

    public class TokboxSession
    {
        public string sessionId { get; set; }
        public string token { get; set; }
        public string apiKey { get; set; }
    }

}
