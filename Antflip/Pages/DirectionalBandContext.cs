using System.Windows.Input;

using Antflip.USBRelay;

namespace Antflip.Pages {
    public class DirectionalBandContext
    {
        private readonly DirectionalBandData data;

        public DirectionalBandContext(ICommand actuate, DirectionalBandData data)
            => (this.ActuateCommand, this.data) = (actuate, data);

        public ICommand ActuateCommand { get; init; }

        public RelayActions Load => this.data.Load;
        public RelayActions North => this.data.North;
        public RelayActions NorthEast => this.data.NorthEast;
        public RelayActions East => this.data.East;
        public RelayActions SouthEast => this.data.SouthEast;
        public RelayActions South => this.data.South;
        public RelayActions SouthWest => this.data.SouthWest;
        public RelayActions West => this.data.West;
        public RelayActions NorthWest => this.data.NorthWest;
        public RelayActions Omni => this.data.Omni;
    }
}
