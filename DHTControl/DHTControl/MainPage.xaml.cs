using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Data.Json;
using DHTControl.AdditionalCodeFloder;
using System.Threading;
// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace DHTControl
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public Dictionary<String, JsonObject> OnlineDevices = new Dictionary<String, JsonObject>();
        public bool NeedOnlineDevicesRefresh=false;
        StreamSocketListener listener;
        public MainPage()
        {
            this.InitializeComponent();

        }
        private async void BrodaCastUdpData(String Data)
        {
            DatagramSocket datagramSocket = new DatagramSocket();
            IOutputStream outputStream = await datagramSocket.GetOutputStreamAsync(new HostName("255.255.255.255"), "2333");
            DataWriter dataWriter = new DataWriter(outputStream);
            dataWriter.WriteString(Data);
            await dataWriter.StoreAsync();
            datagramSocket.Dispose();
            datagramSocket = null;
        }
        private void Timer_Tick(object sender, object e)
        {
            string data = "{\"protocol\":\"myEspNet\",\"command\":\"ping\"}";
            BrodaCastUdpData(data);
            OnlineDevicesTextBlock.Text = $"OnlineDevices:{OnlineDevices.Count}";
            int avrDht = 0;
            List<string> OnlineDevicesKeysList = OnlineDevices.Keys.ToList();
            for (int i = OnlineDevicesKeysList.Count - 1; i >= 0; i--)
            {
                avrDht += (int)OnlineDevices[OnlineDevicesKeysList[i]].GetNamedNumber("dht");
            }
            avrDht = avrDht / OnlineDevices.Count;
            AvrDhtTextBlock.Text = $"AvrDht:{avrDht}";
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = new System.TimeSpan(0, 0, 5)
            };
            timer.Tick += Timer_Tick;
            Thread thread = new Thread(seeIfOnlineDevicesChanged);
            thread.Start();
        }

        private void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                //获取tcp输入流，当tcp连接结束时，流也随之结束
                String TcpData="";
                using (StreamReader streamReader = new StreamReader(args.Socket.InputStream.AsStreamForRead()))
                {
                    while (!streamReader.EndOfStream)
                    {
                        TcpData += streamReader.ReadLine();
                        Debug.WriteLine(TcpData);
                    }
                }

                args.Socket.Dispose();
            }
            catch (Exception ex)
            {
                listener.Dispose();
                listener = null;
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 处理TCP数据
        /// </summary>
        /// <param name="TcpData"></param>
        private void SolveTcpData(String TcpData)
        {
            JsonObject jsonData;
            if (JsonObject.TryParse(TcpData, out jsonData))
            {
                Debug.WriteLine("JsonObjet parse succeed");
                //await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,async ()=> await new MessageDialog(jsonData.ToString()).ShowAsync());
                OnlineDevices[jsonData["deviceName"].GetString()] = jsonData;
                NeedOnlineDevicesRefresh = true;
            }
        }

        private async void StartListeningButton_Click(object sender, RoutedEventArgs e)
        {
            if (listener != null)
            {
                await new MessageDialog("listening has been started").ShowAsync();
                return;
            }
            listener = new StreamSocketListener();
            listener.ConnectionReceived += Listener_ConnectionReceived;
            try
            {
                await listener.BindServiceNameAsync("2334");
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => LogTextBox.Text += "Listening started \r\n");
                StopListeningButton.IsEnabled = true;
                StartListeningButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void StopListeningButton_Click(object sender, RoutedEventArgs e)
        {
            listener.Dispose();
            listener = null;
            LogTextBox.Text += "Listening has been stopped \r\n";
            StartListeningButton.IsEnabled = true;
            StopListeningButton.IsEnabled = false;
        }

        private void SendCommandButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModeComboBox.SelectedItem == null) ModeComboBox.SelectedItem = "Auto";
            if (FanSpeedComboBox.SelectedItem == null) FanSpeedComboBox.SelectedItem = "Auto";
            JsonObject commandData = new JsonObject
            {
                ["power"] = JsonValue.CreateBooleanValue(PowerToggleSwitch.IsOn),
                ["fanSpeed"] = JsonValue.CreateStringValue(FanSpeedComboBox.SelectedItem.ToString().ToLower()),
                ["Mode"] = JsonValue.CreateStringValue(ModeComboBox.SelectedItem.ToString().ToLower()),
                ["temp"] = JsonValue.CreateNumberValue(Convert.ToInt32(TempSlider.Value))
            };
            JsonObject sendData = new JsonObject
            {
                ["protocol"] = JsonValue.CreateStringValue("myEspNet"),
                ["command"] = JsonValue.CreateStringValue("set"),
                ["data"] = commandData
            };
            LogTextBox.Text += sendData.ToString()+"\r\n";
            BrodaCastUdpData(sendData.ToString());
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Text = "";
        }

        private async void seeIfOnlineDevicesChanged()
        {
            while (true)
            {
                if (NeedOnlineDevicesRefresh)
                {
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        var onlineDevices = OnlineDevices;
                        List<string> OnlineDevicesKeysList = onlineDevices.Keys.ToList();
                        for (int i = OnlineDevicesKeysList.Count - 1; i >= 0; i--)
                        {
                            OnlineDevicesListViewDataTemple onlineDevicesListViewData = new OnlineDevicesListViewDataTemple();
                            onlineDevicesListViewData.name = onlineDevices[OnlineDevicesKeysList[i]]["deviceName"].GetString();
                            switch (onlineDevices[OnlineDevicesKeysList[i]]["deviceType"].GetString())
                            {
                                case "AC":
                                    {
                                        String sensorData = "";
                                        JsonObject sensorDatas = onlineDevices[OnlineDevicesKeysList[i]]["data"].GetObject();
                                        foreach (KeyValuePair<string, IJsonValue> item in sensorDatas)
                                        {
                                            sensorData += item.Key + ":" + item.Value.ToString();
                                        }
                                        OnlineDevicesTextBlock.Text = $"OnlineDevices:{OnlineDevices.Count}";
                                        LogTextBox.Text += sensorData + "\r\n";
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }

                            }

                        }
                         NeedOnlineDevicesRefresh = false;
                    });
                }
                //小睡一会免得占用资源
                Thread.Sleep(250);
            }
        }

        private async void SetUpNewDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog contentDialog = new SetUpNewDeviceContentDialog();
            await contentDialog.ShowAsync();
        }
    }
}
