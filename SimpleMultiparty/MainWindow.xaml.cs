using Newtonsoft.Json;
using OpenTok;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace HttpRequest {
    public class RequestSession {
        public TokboxSession tokbox { get; set; }
        public TokboxSession tokboxUDP { get; set; }
    }

    public class TokboxSession {
        public string sessionId { get; set; }
        public string token { get; set; }
        public string apiKey { get; set; }
    }

    class ApiRequest {
        
        public static async Task<RequestSession> GetTokboxSession(string path)
        {
            var hardcoded = "https://api.dev.dstudio.live/xtreamr/v2/public/xtreams/75a8c3a";
            Trace.WriteLine("HAcer request!!"+path);
            Trace.WriteLine("HAcer -------!!" + hardcoded);

            var client = new RestClient(path);
            client.Timeout = -1;
            RequestSession session = null;
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            if (response.IsSuccessful) { 
            /*   HttpClient client = new HttpClient();
               HttpResponseMessage response = await client.GetAsync(path);
               Trace.WriteLine("primer await");

               if (response.IsSuccessStatusCode)
               {
                   Trace.WriteLine("segundo await");
                   var stringResult = await response.Content.ReadAsStringAsync();*/
            Trace.WriteLine("--->>>" + response.Content);
                session = JsonConvert.DeserializeObject<RequestSession>(response.Content);
            }
            else {
                Trace.WriteLine("Error con peticion!" + response.StatusCode);
            }
            return session;
        }
    }
}

namespace SimpleMultiparty
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        Session Session;
        Session UDPSession;


        HttpRequest.RequestSession ReceivedSession;
        String[] Urls = new String[3] { "https://api.dstudio.live/xtreamr/v2/public/xtreams/",
            "https://api.pre.dstudio.live/xtreamr/v2/public/xtreams/",
            "https://api.dev.dstudio.live/xtreamr/v2/public/xtreams/" };

        IList<VideoCapturer.VideoDevice> devices;
        List<Publisher> Publisers = new List<Publisher>();
        List<VideoCapturer> Capturers = new List<VideoCapturer>();

        int ActualPublisherAvailable = 0;
        //   Publisher Publisher;
        bool Disconnect = false;
        Dictionary<Stream, Subscriber> SubscriberByStream = new Dictionary<Stream, Subscriber>();

        public MainWindow()
        {
            //TODO aniadir llamada rest para obtener los datos de la sesion
            InitializeComponent();
            InitViews();
            devices = VideoCapturer.EnumerateDevices();
            FillSelectorWithWebCams();
            AddPublisherButton.IsEnabled = false;
            // We create the publisher here to show the preview when application starts
            // Please note that the PublisherVideo component is added in the xaml file
           // Publisher = new Publisher(Context.Instance, renderer: PublisherVideo, capturer: Capturer, name: "CAMERA");

            Closing += MainWindow_Closing;
        }

        private void InitViews() {
            EnvSelector.Items.Add("PRO");
            EnvSelector.Items.Add("PRE");
            EnvSelector.Items.Add("DEV");
            EnvSelector.SelectedIndex = 2;
            IdTextBox.Text = "75a8c3a";
            ConnectDisconnectButton.IsEnabled = false;
            ConnectDisconnectNoUDPButton.IsEnabled = false;

            StatusText.Content = "Introduzca Id y dele a obtener sesión";
        }

        private void FillSelectorWithWebCams()
        {
            foreach (VideoCapturer.VideoDevice device in devices) {
                WebcamSelector.Items.Add(device.Name);
            }
            WebcamSelector.SelectedIndex = 1;
        }

        private async void Session_Click(object sender, RoutedEventArgs e)
        {
            string SessionCode = IdTextBox.Text;
            var BaseUrl = Urls[EnvSelector.SelectedIndex] + SessionCode;


            ReceivedSession =await HttpRequest.ApiRequest.GetTokboxSession(BaseUrl);
            ConnectDisconnectButton.IsEnabled = true;
            if (ReceivedSession?.tokboxUDP != null) {
                SetStatus("Sesion recibida con UDP");
            }
            else {
                if (ReceivedSession?.tokbox != null)
                {
                    SetStatus("Sesion recibida sencilla");
                }
                else {
                    ConnectDisconnectButton.IsEnabled = false;
                    SetStatus("Sesion invalida, intentelo de nuevo");
                    return;
                }
            }
           
            Session = new Session(Context.Instance, ReceivedSession.tokbox.apiKey, ReceivedSession.tokbox.sessionId);

            Session.Connected += Session_Connected;
            Session.Disconnected += Session_Disconnected;
            Session.Error += Session_Error;
            Session.StreamReceived += Session_StreamReceived;
            Session.StreamDropped += Session_StreamDropped;
            ConnectDisconnectNoUDPButton.IsEnabled = false;
            if (ReceivedSession.tokboxUDP != null) {
                UDPSession = new Session(Context.Instance, ReceivedSession.tokboxUDP.apiKey, ReceivedSession.tokboxUDP.sessionId);

                UDPSession.Connected += Session_Connected;
                UDPSession.Disconnected += Session_Disconnected;
                UDPSession.Error += Session_Error;
                UDPSession.StreamReceived += Session_StreamReceived;
                UDPSession.StreamDropped += Session_StreamDropped;
                ConnectDisconnectNoUDPButton.IsEnabled = true;
            }


        }

        private void SetStatus(string Status) {
            StatusText.Content = Status;
        }

        private void Publish_Click(object sender, RoutedEventArgs e)
        {
            int position = WebcamSelector.SelectedIndex;
            if (UDPSession != null) {
                var CapturerUDP = devices[position].CreateVideoCapturer(VideoCapturer.Resolution.High);
                var PublisherUDP = new Publisher(Context.Instance,  capturer: CapturerUDP, name: "CAMERA");
                Publisers.Add(PublisherUDP);
                Capturers.Add(CapturerUDP);
                UDPSession.Publish(PublisherUDP);
            }
            var Capturer = devices[position].CreateVideoCapturer(VideoCapturer.Resolution.High);
            var Publisher = new Publisher(Context.Instance, renderer: GetNextPublisherView(), capturer: Capturer, name: "CAMERA");
            Publisers.Add(Publisher);
            Capturers.Add(Capturer);

            Session.Publish(Publisher);
        }

        private VideoRenderer GetNextPublisherView() {
            ActualPublisherAvailable += 1;
            switch (ActualPublisherAvailable) {
                case 1:
                    return PublisherVideo_1;
                case 2:
                    return PublisherVideo_2;
                default:
                    return PublisherVideo_3;
            }
        }

        private void UnSuscribeAll() {
            foreach (var myPublisers in Publisers)
            {
                myPublisers?.Dispose();
            }
            Publisers.Clear();
            foreach (var myCapturers in Capturers)
            {
                myCapturers?.Dispose();
            }
            Capturers.Clear();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var subscriber in SubscriberByStream.Values)
            {
                subscriber.Dispose();
            }
            UnSuscribeAll();
            Session?.Dispose();
            if (UDPSession != null)
            {
                UDPSession?.Dispose();
            }
        }

        private void Session_Connected(object sender, EventArgs e)
        {
            try
            {
                AddPublisherButton.IsEnabled = true;
            }
            catch (OpenTokException ex)
            {
                Trace.WriteLine("OpenTokException " + ex.ToString());
            }
        }

        private void Session_Disconnected(object sender, EventArgs e)
        {
            Trace.WriteLine("Session disconnected");
            SubscriberByStream.Clear();
            SubscriberGrid.Children.Clear();
        }

        private void Session_Error(object sender, Session.ErrorEventArgs e)
        {
            Trace.WriteLine("Session error:" + e.ErrorCode);
            MessageBox.Show("Session error:" + e.ErrorCode, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void UpdateGridSize(int numberOfSubscribers)
        {
            int rows = Convert.ToInt32(Math.Round(Math.Sqrt(numberOfSubscribers)));
            int cols = rows == 0 ? 0 : Convert.ToInt32(Math.Ceiling(((double)numberOfSubscribers) / rows));
            SubscriberGrid.Columns = cols;
            SubscriberGrid.Rows = rows;
        }

        private void Session_StreamReceived(object sender, Session.StreamEventArgs e)
        {
            Trace.WriteLine("Session stream received");

            VideoRenderer renderer = new VideoRenderer();
            SubscriberGrid.Children.Add(renderer);
            UpdateGridSize(SubscriberGrid.Children.Count);
            Subscriber subscriber = new Subscriber(Context.Instance, e.Stream, renderer);
            SubscriberByStream.Add(e.Stream, subscriber);

            try
            {
                Session.Subscribe(subscriber);
                if (UDPSession != null) {
                    UDPSession.Subscribe(subscriber);
                }
            }
            catch (OpenTokException ex)
            {
                Trace.WriteLine("OpenTokException " + ex.ToString());
            }
        }

        private void Session_StreamDropped(object sender, Session.StreamEventArgs e)
        {
            Trace.WriteLine("Session stream dropped");
            var subscriber = SubscriberByStream[e.Stream];
            if (subscriber != null)
            {
                SubscriberByStream.Remove(e.Stream);
                try
                {
                    Session.Unsubscribe(subscriber);
                    if (UDPSession != null)
                    {
                        UDPSession.Unsubscribe(subscriber);
                    }
                }
                catch (OpenTokException ex)
                {
                    Trace.WriteLine("OpenTokException " + ex.ToString());
                }

                SubscriberGrid.Children.Remove((UIElement)subscriber.VideoRenderer);
                UpdateGridSize(SubscriberGrid.Children.Count);
            }
        }

        private void UnpublishAll() {
            foreach (var myPublisers in Publisers)
            {
                Session.Unpublish(myPublisers);
                if (UDPSession != null)
                {
                    UDPSession.Unpublish(myPublisers);
                }
            }
        }


        private void Connect_NOUDP_Click(object sender, RoutedEventArgs e)
        {
            Connect(false);
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            Connect(true);

        }

        private void Connect(Boolean withUDP) {

            if (Disconnect)
            {
                Trace.WriteLine("Disconnecting session");
                try
                {
                    //     UnpublishAll();
                    //     Session.Disconnect();
                    UnSuscribeAll();
                    Session?.Dispose();
                    if ((UDPSession != null)&&(withUDP))
                    {
                        UDPSession?.Dispose();
                    }
                }
                catch (OpenTokException ex)
                {
                    Trace.WriteLine("OpenTokException " + ex.ToString());
                }
            }
            else
            {
                Trace.WriteLine("Connecting session");
                try
                {
                    Session.Connect(ReceivedSession.tokbox.token);
                    if ((UDPSession != null)&&(withUDP))
                    {
                        UDPSession?.Connect(ReceivedSession.tokboxUDP.token);
                    }
                }
                catch (OpenTokException ex)
                {
                    Trace.WriteLine("OpenTokException " + ex.ToString());
                }
            }
            Disconnect = !Disconnect;
            ConnectDisconnectButton.Content = Disconnect ? "Desconectar" : "Connectar";
            ConnectDisconnectNoUDPButton.Content = Disconnect ? "Desconectar" : "Connectar con UDP";
        }

    }
}
