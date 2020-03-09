using OpenTok;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace SimpleMultiparty
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string API_KEY = "46464702";
        //TCP
         public const string SESSION_ID = "1_MX40NjQ2NDcwMn5-MTU4Mzc3NDY5MTU4NH5pMXRqcXNkYlM5NVVGb1NIK1Y2QUdDMmR-fg";
        public const string TOKEN = "T1==cGFydG5lcl9pZD00NjQ2NDcwMiZzaWc9MjQyY2NmNmYyMDBjMzBiZGEzNThlN2Q2MWNjMTU4YWUzYTM5NDRhYjpzZXNzaW9uX2lkPTFfTVg0ME5qUTJORGN3TW41LU1UVTRNemMzTkRZNU1UVTROSDVwTVhScWNYTmtZbE01TlZWR2IxTklLMVkyUVVkRE1tUi1mZyZjcmVhdGVfdGltZT0xNTgzNzc0Nzk5Jm5vbmNlPTAuMDI3NjI1MDUzMDYyOTk0ODk4JnJvbGU9cHVibGlzaGVyJmV4cGlyZV90aW1lPTE1ODQzNzk1OTkmY29ubmVjdGlvbl9kYXRhPXh0cmVhbXJVc2VySWQlM0QwJmluaXRpYWxfbGF5b3V0X2NsYXNzX2xpc3Q9";
            //UDP
            //  public const string SESSION_ID = "1_MX40NjI3MDc3Mn5-MTU4Mzc1NDQ5Mjg0OH5tVUJaUXdIdWNjejZNeUpBSllseVY1NUN-UH4";
            //  public const string TOKEN = "T1==cGFydG5lcl9pZD00NjI3MDc3MiZzaWc9Y2IwNGMwMjY2NDc4MGQwNDA4MzI0NTBjOGQyZTlhNTE1NTAzMzU4NTpzZXNzaW9uX2lkPTFfTVg0ME5qSTNNRGMzTW41LU1UVTRNemMxTkRRNU1qZzBPSDV0VlVKYVVYZElkV05qZWpaTmVVcEJTbGxzZVZZMU5VTi1VSDQmY3JlYXRlX3RpbWU9MTU4Mzc1ODE2MyZub25jZT0wLjgxMzcyMzg2Njk2ODI4MDcmcm9sZT1wdWJsaXNoZXImZXhwaXJlX3RpbWU9MTU4NDM2Mjk2MyZjb25uZWN0aW9uX2RhdGE9eHRyZWFtclVzZXJJZCUzRDAmaW5pdGlhbF9sYXlvdXRfY2xhc3NfbGlzdD0=";

        Session Session;
        Session UDPSession;
        IList<VideoCapturer.VideoDevice> devices;
        List<Publisher> Publisers = new List<Publisher>();
        List<VideoCapturer> Capturers = new List<VideoCapturer>();

        //   Publisher Publisher;
        bool Disconnect = false;
        Dictionary<Stream, Subscriber> SubscriberByStream = new Dictionary<Stream, Subscriber>();

        public MainWindow()
        {
            InitializeComponent();
            devices = VideoCapturer.EnumerateDevices();
            FillSelectorWithWebCams();
            AddPublisherButton.IsEnabled = false;
            // We create the publisher here to show the preview when application starts
            // Please note that the PublisherVideo component is added in the xaml file
           // Publisher = new Publisher(Context.Instance, renderer: PublisherVideo, capturer: Capturer, name: "CAMERA");

            if (API_KEY == "" || SESSION_ID == "" || TOKEN == "")
            {
                MessageBox.Show("Please fill out the API_KEY, SESSION_ID and TOKEN variables in the source code " +
                    "in order to connect to the session", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ConnectDisconnectButton.IsEnabled = false;
            }
            else
            {
                Session = new Session(Context.Instance, API_KEY, SESSION_ID);

                Session.Connected += Session_Connected;
                Session.Disconnected += Session_Disconnected;
                Session.Error += Session_Error;
                Session.StreamReceived += Session_StreamReceived;
                Session.StreamDropped += Session_StreamDropped;
            }

            Closing += MainWindow_Closing;
        }


        private void FillSelectorWithWebCams()
        {
            foreach (VideoCapturer.VideoDevice device in devices) {
                WebcamSelector.Items.Add(device.Name);
            }
           
        }

        private void Publish_Click(object sender, RoutedEventArgs e)
        {
            int position = WebcamSelector.SelectedIndex;

            var Capturer = devices[position].CreateVideoCapturer(VideoCapturer.Resolution.High);

            var publisherRenderer = PublisherVideo_1;
            switch (Publisers.Count) {
                case 0:
                    publisherRenderer = PublisherVideo_1;
                    break;
                case 1:
                    publisherRenderer = PublisherVideo_2;
                    break;
                default:
                    publisherRenderer = PublisherVideo_3;
                    break;
            }

            var Publisher = new Publisher(Context.Instance, renderer: publisherRenderer, capturer: Capturer,name:"CAMERA");
            Publisers.Add(Publisher);
            Capturers.Add(Capturer);

            Session.Publish(Publisher);
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
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (Disconnect)
            {
                Trace.WriteLine("Disconnecting session");
                try
                {
                    //     UnpublishAll();
                    //     Session.Disconnect();
                    UnSuscribeAll();
                    Session?.Dispose();
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
                    Session.Connect(TOKEN);
                }
                catch (OpenTokException ex)
                {
                    Trace.WriteLine("OpenTokException " + ex.ToString());
                }
            }
            Disconnect = !Disconnect;
            ConnectDisconnectButton.Content = Disconnect ? "Disconnect" : "Connect";
        }
    }
}
