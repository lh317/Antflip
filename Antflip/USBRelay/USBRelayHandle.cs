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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Antflip.USBRelay
{
    public class USBRelayHandle : SafeHandle
    {
        [SuppressMessage("Microsoft.Design", "IDE1006", Justification = "Native Method")]
        [DllImport("usb-relay-device")]
        private extern static USBRelayHandle usb_relay_device_enumerate();

        [SuppressMessage("Microsoft.Design", "IDE1006", Justification = "Native Method")]
        [DllImport("usb-relay-device")]
        private extern static void usb_relay_device_free_enumerate(USBRelayHandle handle);

        public static USBRelayHandle Create() {
            try {
                return usb_relay_device_enumerate();
            } catch (DllNotFoundException) {
                var handle = new USBRelayHandle();
                handle.SetHandleAsInvalid();
                return handle;
            }
        }

        // There must be an actual default constructor (one with default
        // arguments does not work) in order for the runtime interop with
        // IntPtr to work.
        private USBRelayHandle() : base(IntPtr.Zero, true) { }

        protected USBRelayHandle(bool owned) : base(IntPtr.Zero, owned) { }

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        override protected bool ReleaseHandle() {
            if (!this.IsClosed && !this.IsInvalid) {
                usb_relay_device_free_enumerate(this);
            }
            return true;
        }
    }

    public class USBRelayInnerHandle : USBRelayHandle
    {
        private USBRelayInnerHandle() : base(false) { }
    }

}
