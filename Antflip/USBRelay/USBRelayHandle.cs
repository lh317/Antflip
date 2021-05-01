using System;
using System.Runtime.InteropServices;

namespace Antflip.USBRelay
{
    public class USBRelayHandle : SafeHandle
    {
        [DllImport("usb-relay-device")]
        private extern static USBRelayHandle usb_relay_device_enumerate();

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
