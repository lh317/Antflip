// Copyright 2021 lh317
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;

namespace Antflip.USBRelay {

    public record USBRelayBoard(int Index, string Name, int NumDevices);

    public sealed class USBRelayDriver : IReadOnlyCollection<USBRelayBoard>, IDisposable
    {

        [SuppressMessage("Microsoft.Design", "IDE1006", Justification = "Native Method")]
        [DllImport("usb-relay-device")]
        private extern static USBRelayInnerHandle usb_relay_device_next_dev(USBRelayHandle handle);

        [SuppressMessage("Microsoft.Design", "IDE1006", Justification = "Native Method")]
        [DllImport("usb-relay-device")]
        private extern static int usb_relay_device_get_num_relays(USBRelayHandle handle);

        [SuppressMessage("Microsoft.Design", "IDE1006", Justification = "Native Method")]
        [DllImport("usb-relay-device")]
        private extern static IntPtr usb_relay_device_get_id_string(USBRelayHandle handle);

        [SuppressMessage("Microsoft.Design", "IDE1006", Justification = "Native Method")]
        [DllImport("usb-relay-device")]
        private extern static int usb_relay_device_open_all_relay_channel(USBRelayHandle hHandle);

        [SuppressMessage("Microsoft.Design", "IDE1006", Justification = "Native Method")]
        [DllImport("usb-relay-device")]
        private extern static int usb_relay_device_close_all_relay_channel(USBRelayHandle hHandle);

        private readonly USBRelayHandle root;
        private readonly List<USBRelayBoard> boards = new(2);
        private readonly List<USBRelayHandle> handles = new(2);

        public USBRelayDriver() {
            this.RelayCount = 0;
            this.root = USBRelayHandle.Create();
            try {
                var handle = root;
                for (int i=0; !handle.IsInvalid; handle = usb_relay_device_next_dev(handle), ++i) {
                    this.handles.Add(handle);
                    var num = usb_relay_device_get_num_relays(handle);
                    this.RelayCount += num;
                    var idPtr = usb_relay_device_get_id_string(handle);
                    var name = Marshal.PtrToStringAnsi(idPtr) ?? throw new InvalidOperationException();
                    var dupes = this.boards.Where(b => b.Name.StartsWith(name)).Count();
                    if (dupes > 0) {
                        name = string.Format("{0} ({1})", name, dupes + 1);
                    }
                    this.boards.Add(new(i, name, num));
                }
            } catch {
                root.Dispose();
                throw;
            }
        }

        public int RelayCount { get; }

        public USBRelayControl ControlRelays(IEnumerable<USBRelayBoard> boards) {
            var list = new List<USBRelay>(this.RelayCount);
            foreach(var b in boards) {
                var handle = this.handles[b.Index];
                for (var i = 0; i < b.NumDevices; i++) {
                    list.Add(new(handle, i+1));
                }
            }
            return new(list);
        }

        public bool CloseAll() {
            bool ret = true;
            foreach(var handle in handles) {
                ret &= usb_relay_device_close_all_relay_channel(handle) == 0;
            }
            return ret;
        }

        public bool OpenChannels(USBRelayBoard board) {
            return usb_relay_device_open_all_relay_channel(this.handles[board.Index]) == 0;
        }

        public bool CloseChannels(USBRelayBoard board) {
            return usb_relay_device_close_all_relay_channel(this.handles[board.Index]) == 0;
        }

        public int Count => this.boards.Count;

        public IEnumerator<USBRelayBoard> GetEnumerator() {
            return this.boards.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        public void Dispose() {
            this.root.Dispose();
            GC.SuppressFinalize(this);
        }

    }
}
