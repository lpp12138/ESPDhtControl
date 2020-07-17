using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Data.Json;
using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using Windows.UI.Popups;
using Windows.Storage.Streams;
using System.Diagnostics;

//这里定义了设置新设备页面的弹出窗口

namespace DHTControl
{
    public sealed partial class SetUpNewDeviceContentDialog : ContentDialog
    {
        public SetUpNewDeviceContentDialog()
        {
            this.InitializeComponent();
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) //用户按下弹窗确定时
        {
            SerialDevice serialDevice;
            string portNames = SerialDevice.GetDeviceSelector();
            var deviceInformation = await DeviceInformation.FindAllAsync(portNames);
            JsonObject deviceData = new JsonObject();
            {
                if (ssidTextBox.Text == null || wifiPassWordBox == null ||deviceNameTextBox ==null) //检查用户输入的数据
                {
                    await new MessageDialog("element not filled").ShowAsync();
                }
                else //编码json数据
                {
                    deviceData["ssid"] = JsonValue.CreateStringValue(ssidTextBox.Text);
                    deviceData["password"] = JsonValue.CreateStringValue(wifiPassWordBox.Password);
                    deviceData["deviceName"] = JsonValue.CreateStringValue(deviceNameTextBox.Text);
                }
            }

            foreach (var i in deviceInformation) //找到连接的开发板并发送数据(通过115200的波特率判断)
            {
                serialDevice = await SerialDevice.FromIdAsync(i.Id);
                if (serialDevice != null)
                {
                    Debug.WriteLine(i);
                    if (serialDevice.BaudRate == 115200)
                    {
                        Debug.WriteLine(serialDevice.PortName);
                        Debug.WriteLine(deviceData.ToString());
                        DataWriter dataWriter = new DataWriter(serialDevice.OutputStream);
                        dataWriter.WriteString(deviceData.ToString());
                        await dataWriter.StoreAsync();
                        dataWriter.Dispose();
                        serialDevice.Dispose();
                    }
                }
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            return;
        }
    }
}
