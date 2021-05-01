namespace Antflip.USBRelay
{
    public record USBRelayEventData
    {
        public int Index { get; }

        public USBRelayEventData(int index) => (this.Index) = index;
    }
}
