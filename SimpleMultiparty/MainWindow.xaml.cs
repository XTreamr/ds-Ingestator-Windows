using OpenTok;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows;


namespace DSIngestator
{

    public partial class MainWindow : Window
    {

        Session Session;
        Session UDPSession;

        HttpRequest.RequestSession ReceivedSession;
        String[] Urls = new String[3] { "https://api.dstudio.live/xtreamr/v2/public/xtreams/",
            "https://api.pre.dstudio.live/xtreamr/v2/public/xtreams/",
            "https://api.dev.dstudio.live/xtreamr/v2/public/xtreams/" };

        Bitmap EmptyBitmap = CreateEmptyBitmap(114,184);
        IList<VideoCapturer.VideoDevice> devices;
        List<Publisher> Publisers = new List<Publisher>();
        List<Publisher> UDPPublisers = new List<Publisher>();

        List<VideoCapturer> Capturers = new List<VideoCapturer>();

        int ActualPublisherAvailable = 0;
        bool Disconnect = false;

        public MainWindow()
        {
            InitializeComponent();
            InitViews();
            devices = VideoCapturer.EnumerateDevices();
            FillSelectorWithWebCams();
            AddPublisherButton.IsEnabled = false;

            Closing += MainWindow_Closing;
        }

        private void InitViews() {
            EnvSelector.Items.Add("PRO");
            EnvSelector.Items.Add("PRE");
            EnvSelector.Items.Add("DEV");
            EnvSelector.SelectedIndex = 1;
            IdTextBox.Text = "089ef02";
            ConnectDisconnectButton.IsEnabled = false;
            ConnectDisconnectNoUDPButton.IsEnabled = false;
            SetDefaultsImages();
            StatusText.Content = "Introduzca Id y dele a obtener sesión";
        }

        private void SetDefaultsImages() {
            PublisherVideo_1.RenderFrame(VideoFrame.CreateYuv420pFrameFromBitmap(EmptyBitmap));
            PublisherVideo_2.RenderFrame(VideoFrame.CreateYuv420pFrameFromBitmap(EmptyBitmap));
            PublisherVideo_3.RenderFrame(VideoFrame.CreateYuv420pFrameFromBitmap(EmptyBitmap));
        }

        private void FillSelectorWithWebCams()
        {
            foreach (VideoCapturer.VideoDevice device in devices) {
                WebcamSelector.Items.Add(device.Name);
                Capturers.Add(null);

            }
            WebcamSelector.SelectedIndex = 0;
        }

        private async void Session_Click(object sender, RoutedEventArgs e)
        {
            SetStatus("Obteniendo sesión, por favor, espere....");
            string SessionCode = IdTextBox.Text;
            var BaseUrl = Urls[EnvSelector.SelectedIndex] + SessionCode;

            ReceivedSession =await HttpRequest.ApiRequest.GetTokboxSession(BaseUrl);

            if (ReceivedSession?.tokboxUDP != null) {
                SetStatus("Sesion recibida con UDP");
                ConnectDisconnectButton.IsEnabled = true;
                ConnectDisconnectNoUDPButton.IsEnabled = true;
            }
            else {
                if (ReceivedSession?.tokbox != null)
                {
                    ConnectDisconnectButton.IsEnabled = false;
                    ConnectDisconnectNoUDPButton.IsEnabled = true;
                    SetStatus("Sesion recibida sencilla");
                }
                else {
                    ConnectDisconnectButton.IsEnabled = false;
                    ConnectDisconnectNoUDPButton.IsEnabled = false;
                    SetStatus("Sesion invalida, intentelo de nuevo");
                    return;
                }
            }
        }

        private void SetStatus(string Status) {
            StatusText.Content = Status;
        }

        private VideoCapturer GetCapturer(int position) {
            if (Capturers[position] == null) {
                Capturers[position] = devices[position].CreateVideoCapturer(VideoCapturer.Resolution.High);
            }
            return Capturers[position];
        }

        private void Publish_Click(object sender, RoutedEventArgs e)
        {
            int position = WebcamSelector.SelectedIndex;
            var Capturer = GetCapturer(position);
            if (UDPSession != null) {
                var PublisherUDP = new Publisher(Context.Instance,  capturer: Capturer, name: "CAMERA");
                UDPPublisers.Add(PublisherUDP);
                UDPSession.Publish(PublisherUDP);
            }
            var Publisher = new Publisher(Context.Instance, renderer: GetNextPublisherView(), capturer: Capturer, name: "CAMERA");
            Publisers.Add(Publisher);
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
            foreach (var myPublisers in UDPPublisers)
            {
                myPublisers?.Dispose();
            }
            UDPPublisers.Clear();
            Publisers.Clear();
            foreach (var myCapturers in Capturers)
            {
                myCapturers?.Dispose();
            }
            for (int i = 0; i < Capturers.Count;i++) {
                Capturers[i] = null;
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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
            AddPublisherButton.IsEnabled = false;
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

        private void UnpublishAll() {
            foreach (var myPublisers in Publisers)
            {
                Session.Unpublish(myPublisers);

            }
            foreach (var myPublisers in UDPPublisers)
            {
                UDPSession.Unpublish(myPublisers);

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
                DisconnectProcess(withUDP);
            }
            else
            {
                ConnectProcess(withUDP);
            }
            Disconnect = !Disconnect;
            ConnectDisconnectButton.Content = Disconnect ? "Desconectar" : "Connectar";
            ConnectDisconnectNoUDPButton.Content = Disconnect ? "Desconectar" : "Connectar SIN UDP";
        }

        private void ConnectProcess(bool withUDP)
        {
            Session = new Session(Context.Instance, ReceivedSession.tokbox.apiKey, ReceivedSession.tokbox.sessionId);
            Session.Connected += Session_Connected;
            Session.Disconnected += Session_Disconnected;
            Session.Error += Session_Error;
            if ((ReceivedSession.tokboxUDP != null) && (withUDP == true))
            {
                UDPSession = new Session(Context.Instance, ReceivedSession.tokboxUDP.apiKey, ReceivedSession.tokboxUDP.sessionId);
                UDPSession.Connected += Session_Connected;
                UDPSession.Disconnected += Session_Disconnected;
                UDPSession.Error += Session_Error;
                ConnectDisconnectNoUDPButton.IsEnabled = true;
            }
            Trace.WriteLine("Connecting session");
            try
            {
                Session.Connect(ReceivedSession.tokbox.token);
                if ((UDPSession != null) && (withUDP))
                {
                    UDPSession?.Connect(ReceivedSession.tokboxUDP.token);
                }
            }
            catch (OpenTokException ex)
            {
                Trace.WriteLine("OpenTokException " + ex.ToString());
            }
        }

        private void DisconnectProcess(bool withUDP)
        {
            Trace.WriteLine("Disconnecting session");
            ActualPublisherAvailable = 0;
            AddPublisherButton.IsEnabled = false;
            try
            {
                ;
                UnSuscribeAll();
                Session?.Dispose();
                Session?.Disconnect();
                Session = null;
                if ((UDPSession != null) && (withUDP))
                {
                    UDPSession?.Dispose();
                    UDPSession?.Disconnect();
                    UDPSession = null;
                }
            }
            catch (OpenTokException ex)
            {
                Trace.WriteLine("OpenTokException " + ex.ToString());
            }
            SetDefaultsImages();
        }

        private static Bitmap CreateEmptyBitmap(int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics graph = Graphics.FromImage(bmp))
            {
                Rectangle ImageSize = new Rectangle(0, 0, width, height);
                graph.FillRectangle(Brushes.White, ImageSize);
            }
            return bmp;
        }
    }
}
