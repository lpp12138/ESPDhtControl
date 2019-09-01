using System;
namespace DHTControl.AdditionalCodeFloder
{
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