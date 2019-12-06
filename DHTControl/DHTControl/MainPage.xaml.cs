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
        public bool ACisOn = false;
        StreamSocketListener listener;
        public MainPage()
        {
            this.InitializeComponent();
        }
        private async void BroadCastUdpData(String Data)
        {
            DatagramSocket datagramSocket = new DatagramSocket();
            IOutputStream outputStream = await datagramSocket.GetOutputStreamAsync(new HostName("255.255.255.255"), "2333");
            DataWriter dataWriter = new DataWriter(outputStream);
            dataWriter.WriteString(Data);
            var dataWriterStoreOperation= await dataWriter.StoreAsync();
            Debug.WriteLine(dataWriterStoreOperation.ToString());
            if(datagramSocket!=null)
            {
                datagramSocket.Dispose();
                datagramSocket = null;
            }
        }
        private async void Timer_Tick(object sender, object e)
        {
            string data = "{\"protocol\":\"myEspNet\",\"command\":\"ping\"}";
            BroadCastUdpData(data);
            if (LogTextBox.Text.Count() > 3000) LogTextBox.Text = "";
            int avrDht = 0;
            List<string> OnlineDevicesKeysList = OnlineDevices.Keys.ToList();
            for (int i = OnlineDevicesKeysList.Count - 1; i >= 0; i--)
            {
                var jsondata=OnlineDevices[OnlineDevicesKeysList[i]]["data"].GetObject();
                if (jsondata.ToString() == "{}") return;
                avrDht += int.Parse(jsondata.GetNamedString("dht"));
            }
            if (OnlineDevices.Count == 0) return;
            avrDht = avrDht / OnlineDevices.Count;
            OnlineDevicesTextBlock.Text = $"OnlineDevices:{OnlineDevices.Count}";
            AvrDhtTextBlock.Text = $"AvrDht:{avrDht}";
            if(autoControlToggleSwitch.IsOn)
            {
                int minDht = -1, maxDht = 999;
                try
                {
                    minDht = int.Parse(dhtRngMinTextBox.Text);
                    maxDht = int.Parse(dhtRngMaxTextBox.Text);
                }
                catch (Exception ex)
                {
                    MessageDialog messageDialog = new MessageDialog("range wrong input"+ex.Message);
                    await messageDialog.ShowAsync();
                }
                if (avrDht > maxDht && !ACisOn)
                {
                    //turn on
                    BroadCastUdpData("{\"protocol\":\"myEspNet\",\"command\":\"set\",\"data\":{\"power\":true,\"fanSpeed\":\"auto\",\"Mode\":\"dry\",\"temp\":26}}");
                }
                else if (avrDht<minDht)
                {
                    //turn off
                    ACisOn = false;
                    BroadCastUdpData("{\"protocol\":\"myEspNet\",\"command\":\"set\",\"data\":{\"power\":false,\"fanSpeed\":\"auto\",\"Mode\":\"dry\",\"temp\":26}}");
                }
            }
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = new System.TimeSpan(0, 0, 10)
            };
            timer.Start();
            timer.Tick += Timer_Tick;
            Thread thread = new Thread(SeeIfOnlineDevicesChanged);
            thread.Start();
        }

        private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
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
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => LogTextBox.Text += TcpData+"\r\n");
                        Debug.WriteLine(TcpData);
                        SolveTcpData(TcpData);
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
            BroadCastUdpData(sendData.ToString());
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Text = "";
        }

        private async void SeeIfOnlineDevicesChanged()
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
                Thread.Sleep(500);
            }
        }

        private async void SetUpNewDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog contentDialog = new SetUpNewDeviceContentDialog();
            await contentDialog.ShowAsync();
        }

        private void autoControlToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if(autoControlToggleSwitch.IsOn)
            {
                dhtRngMaxTextBox.IsEnabled = true;
                dhtRngMinTextBox.IsEnabled = true;
            }
            else
            {
                dhtRngMaxTextBox.IsEnabled = false;
                dhtRngMinTextBox.IsEnabled = false;
            }
        }

        private void autoControlToggleSwitch_loaded(object sender, RoutedEventArgs e)
        {
            autoControlToggleSwitch_Toggled(sender, e);
        }

        private async void InfoAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog message = new MessageDialog("Open sourced at: https://github.com/lpp12138/ESPDhtControl");
            await message.ShowAsync();
        }
    }
}
