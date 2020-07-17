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

    public sealed partial class MainPage : Page 
    {
        
        public Dictionary<String, JsonObject> OnlineDevices = new Dictionary<String, JsonObject>(); //建立储存在线设备数据的字典
        public bool NeedOnlineDevicesRefresh=false; //判断是否需要刷新在线设备的旗标
        StreamSocketListener listener;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void BroadCastUdpData(String Data) //广播udp数据
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
        private async void Timer_Tick(object sender, object e)  //处理计时器打点事件
        {
            string data = "{\"protocol\":\"myEspNet\",\"command\":\"ping\"}";  //向客户端发送ping指令
            BroadCastUdpData(data);
            if (LogTextBox.Text.Count() > 3000) LogTextBox.Text = ""; //检查log textbox字符数，防止溢出使程序崩溃
            int avrDht = 0; //储存平均湿度的变量
            List<string> OnlineDevicesKeysList = OnlineDevices.Keys.ToList(); //获取在线设备数据字典，并转化为列表计算数据
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
           
            if(autoControlToggleSwitch.IsOn) //检查自动控制开关的状态
            {
                int minDht = -1, maxDht = 999; //初始化湿度阈值，这样的初始化数值能防止用户在未输入的情况下错误发送指令
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
                if (avrDht > maxDht)
                {
                    //turn on
                    BroadCastUdpData("{\"protocol\":\"myEspNet\",\"command\":\"set\",\"data\":{\"power\":true,\"fanSpeed\":\"auto\",\"Mode\":\"dry\",\"temp\":26}}");
                }
                else if (avrDht<minDht)
                {
                    //turn off
                    BroadCastUdpData("{\"protocol\":\"myEspNet\",\"command\":\"set\",\"data\":{\"power\":false,\"fanSpeed\":\"auto\",\"Mode\":\"dry\",\"temp\":26}}");
                }
            }
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e) //退出按钮
        {
            Application.Current.Exit();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e) //在主页面加载完后开启定时器
        {
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = new System.TimeSpan(0, 0, 10)
            };
            timer.Start();
            timer.Tick += Timer_Tick;
            Thread thread = new Thread(seeIfOnlineDevicesChanged);
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

        private void ClearLogButton_Click(object sender, RoutedEventArgs e) //清除log窗口
        {
            LogTextBox.Text = "";
        }

        private async void seeIfOnlineDevicesChanged() //检查在线设备是否需要更新
        {
            while (true)
            {
                if (NeedOnlineDevicesRefresh) //如果需要
                {
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => //利用析构器跨线程调整主窗口内显示的信息
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

        private async void SetUpNewDeviceButton_Click(object sender, RoutedEventArgs e) //设置新设备
        {
            ContentDialog contentDialog = new SetUpNewDeviceContentDialog();
            await contentDialog.ShowAsync();
        }

        private void autoControlToggleSwitch_Toggled(object sender, RoutedEventArgs e) //关于用户窗口显示的控制
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

        private async void InfoAppBarButton_Click(object sender, RoutedEventArgs e) //应用信息
        {
            MessageDialog message = new MessageDialog("Open sourced at: https://github.com/lpp12138/ESPDhtControl");
            await message.ShowAsync();
        }
    }
}
