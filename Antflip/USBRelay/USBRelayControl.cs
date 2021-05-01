using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Antflip.USBRelay
{
    public record USBRelay(USBRelayHandle Handle, int Index);

    public class USBRelayControl
    {
        [DllImport("usb-relay-device")]
        private extern static int usb_relay_device_get_status_bitmap(USBRelayHandle handle);

        [DllImport("usb-relay-device")]
        private extern static int usb_relay_device_open_one_relay_channel(USBRelayHandle handle, int index);

        [DllImport("usb-relay-device")]
        private extern static int usb_relay_device_close_one_relay_channel(USBRelayHandle handle, int index);

        private readonly IList<USBRelay> relays;

        public USBRelayControl(IList<USBRelay> relays) => this.relays = relays;

        public event EventHandler<USBRelayEventData>? Opened;
        public event EventHandler<USBRelayEventData>? Closed;

        public bool Actuate(RelayActions actions) {
            bool result = true;
            foreach(var linearIdx in actions.Close) {
                if (linearIdx >= 0 && linearIdx < this.relays.Count) {
                    var (handle, index) = this.relays[linearIdx];
                    var success = usb_relay_device_close_one_relay_channel(handle, index);
                    if (success == 0) {
                        result &= true;
                        this.Closed?.Invoke(this, new(linearIdx));
                    } else {
                        result &= false;
                    }
                }
            }
            foreach(var linearIdx in actions.Open) {
                if (linearIdx >= 0 && linearIdx < this.relays.Count) {
                    var (handle, index) = this.relays[linearIdx];
                    var success = usb_relay_device_open_one_relay_channel(handle, index);
                    if (success == 0) {
                        result &= true;
                        this.Opened?.Invoke(this, new(linearIdx));
                    } else {
                        result &= false;
                    }
                }
            }
            return result;
        }

        public IList<bool> GetOpenRelays() {
            List<bool> ret = new(this.relays.Count);
            USBRelayHandle? old = null;
            int status = 0;
            foreach(var (handle, i) in this.relays) {
                if (!Object.ReferenceEquals(old, handle)) {
                    old = handle;
                    status = usb_relay_device_get_status_bitmap(handle);
                }
                ret.Add(((status >> i) & 0x1) == 1);
            }
            return ret;
        }
    }
}
