// Copyright 2021, 2024 lh317
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;

namespace Antflip.USBRelay
{
    public record USBRelay(USBRelayHandle Handle, int Index);

    public interface IUSBRelayControl {
        public event EventHandler<USBRelayEventData>? Opened;
        public event EventHandler<USBRelayEventData>? Closed;

        public bool Actuate(RelayActions actions);

        public IList<bool> GetOpenRelays();
    }

    public class USBRelayControl : IUSBRelayControl
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

    public class VirtualUSBRelayControl : IUSBRelayControl
    {
        private readonly int count;

        public VirtualUSBRelayControl(int relayCount) => this.count = relayCount;

        public event EventHandler<USBRelayEventData>? Opened;
        public event EventHandler<USBRelayEventData>? Closed;

        public bool Actuate(RelayActions actions) {
            foreach(var linearIdx in actions.Close) {
                if (linearIdx >= 0 && linearIdx < this.count) {
                    this.Closed?.Invoke(this, new(linearIdx));
                }
            }
            foreach(var linearIdx in actions.Open) {
                if (linearIdx >= 0 && linearIdx < this.count) {
                    this.Opened?.Invoke(this, new(linearIdx));
                }
            }
            return true;
        }

        public IList<bool> GetOpenRelays() {
            return Enumerable.Repeat(false, this.count-1).ToList();
        }
    }
}
