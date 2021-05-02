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
using System.Linq;
using System.Runtime.InteropServices;

namespace Antflip.USBRelay {

    public record USBRelayBoard(string Name, int NumDevices);

    public sealed class USBRelayDriver : IReadOnlyCollection<USBRelayBoard>, IDisposable
    {

        [DllImport("usb-relay-device")]
        private extern static USBRelayInnerHandle usb_relay_device_next_dev(USBRelayHandle handle);

        [DllImport("usb-relay-device")]
        private extern static int usb_relay_device_get_num_relays(USBRelayHandle handle);

        [DllImport("usb-relay-device")]
        private extern static IntPtr usb_relay_device_get_id_string(USBRelayHandle handle);

        [DllImport("usb-relay-device")]
        private extern static int usb_relay_device_open_all_relay_channel(USBRelayHandle hHandle);

        [DllImport("usb-relay-device")]
        private extern static int usb_relay_device_close_all_relay_channel(USBRelayHandle hHandle);

        private readonly USBRelayHandle root;

        private readonly Dictionary<USBRelayBoard, USBRelayHandle> boards = new();

        public USBRelayDriver() {
            this.RelayCount = 0;
            this.root = USBRelayHandle.Create();
            var handle = root;
            try {
                for (; !handle.IsInvalid; handle = usb_relay_device_next_dev(handle)) {
                    var num = usb_relay_device_get_num_relays(root);
                    this.RelayCount += num;
                    var idPtr = usb_relay_device_get_id_string(root);
                    var id = Marshal.PtrToStringAnsi(idPtr) ?? throw new InvalidOperationException();
                    boards.Add(new(id, num), handle);
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
                var handle = this.boards[b];
                for (var i = 0; i < b.NumDevices; i++) {
                    list.Add(new(handle, i+1));
                }
            }
            return new(list);
        }

        public bool CloseAll() {
            bool ret = true;
            for (var handle = this.root; !handle.IsInvalid; handle = usb_relay_device_next_dev(handle)) {
                ret &= usb_relay_device_close_all_relay_channel(handle) == 0;
            }
            return ret;
        }

        public bool OpenChannels(USBRelayBoard board) {
            return usb_relay_device_open_all_relay_channel(this.boards[board]) == 0;
        }

        public bool CloseChannels(USBRelayBoard board) {
            return usb_relay_device_close_all_relay_channel(this.boards[board]) == 0;
        }

        public int Count => this.boards.Count;

        public IEnumerator<USBRelayBoard> GetEnumerator() {
            return this.boards.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        public void Dispose() {
            root.Dispose();
            GC.SuppressFinalize(this);
        }

    }
}
