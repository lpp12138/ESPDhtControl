using System;
namespace DHTControl.AdditionalCodeFloder
{
    //定义sListViewDataTemple供应用内数据传输
    public class OnlineDevicesListViewDataTemple : IEquatable<OnlineDevicesListViewDataTemple>
    {
        public String name { get; set; }
        public String data { get; set; }
        public bool Equals(OnlineDevicesListViewDataTemple other)
        {
            return other.name == this.name;
        }
    }
}