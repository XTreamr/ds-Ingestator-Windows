using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using System.Diagnostics;
using System;

namespace HttpRequest
{
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

    class ApiRequest
    {

        public static async Task<RequestSession> GetTokboxSession(string path)
        {
            RequestSession session = null;
            try
            {
                var client = new RestClient(path);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(request);
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
                return session;
            }
        }
    }
}
