﻿#pragma checksum "C:\Users\lpp12138\source\repos\DHTControl\DHTControl\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "3423D01A74E2521D5B4D39394EA04143"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DHTControl
{
    partial class MainPage : 
        global::Windows.UI.Xaml.Controls.Page, 
        global::Windows.UI.Xaml.Markup.IComponentConnector,
        global::Windows.UI.Xaml.Markup.IComponentConnector2
    {
        /// <summary>
        /// Connect()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 10.0.17.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1: // MainPage.xaml line 1
                {
                    this.MainPage1 = (global::Windows.UI.Xaml.Controls.Page)(target);
                    ((global::Windows.UI.Xaml.Controls.Page)this.MainPage1).Loaded += this.MainPage_Loaded;
                }
                break;
            case 2: // MainPage.xaml line 31
                {
                    global::Windows.UI.Xaml.Controls.AppBarButton element2 = (global::Windows.UI.Xaml.Controls.AppBarButton)(target);
                    ((global::Windows.UI.Xaml.Controls.AppBarButton)element2).Click += this.AppBarButton_Click;
                }
                break;
            case 3: // MainPage.xaml line 32
                {
                    this.StartListeningButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.StartListeningButton).Click += this.StartListeningButton_Click;
                }
                break;
            case 4: // MainPage.xaml line 33
                {
                    this.ModeComboBox = (global::Windows.UI.Xaml.Controls.ComboBox)(target);
                }
                break;
            case 5: // MainPage.xaml line 39
                {
                    this.PowerToggleSwitch = (global::Windows.UI.Xaml.Controls.ToggleSwitch)(target);
                }
                break;
            case 6: // MainPage.xaml line 40
                {
                    this.FanSpeedComboBox = (global::Windows.UI.Xaml.Controls.ComboBox)(target);
                }
                break;
            case 7: // MainPage.xaml line 49
                {
                    this.StopListeningButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.StopListeningButton).Click += this.StopListeningButton_Click;
                }
                break;
            case 8: // MainPage.xaml line 50
                {
                    this.ClearLogButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.ClearLogButton).Click += this.ClearLogButton_Click;
                }
                break;
            case 9: // MainPage.xaml line 51
                {
                    this.SendCommandButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.SendCommandButton).Click += this.SendCommandButton_Click;
                }
                break;
            case 10: // MainPage.xaml line 52
                {
                    this.TempSlider = (global::Windows.UI.Xaml.Controls.Slider)(target);
                }
                break;
            case 11: // MainPage.xaml line 53
                {
                    this.TempSliderValueTextBlock = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 12: // MainPage.xaml line 54
                {
                    this.OnlineDevicesTextBlock = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 13: // MainPage.xaml line 55
                {
                    this.AvrDhtTextBlock = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 14: // MainPage.xaml line 56
                {
                    this.SetUpNewDeviceButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.SetUpNewDeviceButton).Click += this.SetUpNewDeviceButton_Click;
                }
                break;
            case 15: // MainPage.xaml line 57
                {
                    this.dhtRngMinTextBox = (global::Windows.UI.Xaml.Controls.TextBox)(target);
                }
                break;
            case 16: // MainPage.xaml line 58
                {
                    this.dhtRngMaxTextBox = (global::Windows.UI.Xaml.Controls.TextBox)(target);
                }
                break;
            case 17: // MainPage.xaml line 61
                {
                    this.autoControlToggleSwitch = (global::Windows.UI.Xaml.Controls.ToggleSwitch)(target);
                    ((global::Windows.UI.Xaml.Controls.ToggleSwitch)this.autoControlToggleSwitch).Toggled += this.autoControlToggleSwitch_Toggled;
                    ((global::Windows.UI.Xaml.Controls.ToggleSwitch)this.autoControlToggleSwitch).Loaded += this.autoControlToggleSwitch_loaded;
                }
                break;
            case 18: // MainPage.xaml line 62
                {
                    this.infoAppBarButton = (global::Windows.UI.Xaml.Controls.AppBarButton)(target);
                    ((global::Windows.UI.Xaml.Controls.AppBarButton)this.infoAppBarButton).Click += this.InfoAppBarButton_Click;
                }
                break;
            case 19: // MainPage.xaml line 47
                {
                    this.LogTextBox = (global::Windows.UI.Xaml.Controls.TextBox)(target);
                }
                break;
            default:
                break;
            }
            this._contentLoaded = true;
        }

        /// <summary>
        /// GetBindingConnector(int connectionId, object target)
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 10.0.17.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Windows.UI.Xaml.Markup.IComponentConnector GetBindingConnector(int connectionId, object target)
        {
            global::Windows.UI.Xaml.Markup.IComponentConnector returnValue = null;
            return returnValue;
        }
    }
}

