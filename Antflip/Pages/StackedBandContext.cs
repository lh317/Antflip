using System.Windows.Input;

using Antflip.USBRelay;

namespace Antflip.Pages {
    public class StackedBandContext {
        private readonly SwitchedBandData data;

        public StackedBandContext(ICommand actuate, SwitchedBandData data)
            => (this.ActuateCommand, this.data) = (actuate, data);

        public ICommand ActuateCommand { get; init; }
        public RelayActions Load => this.data.Load;
        public RelayActions UpperStack => this.data.UpperStack;
        public RelayActions LowerStack => this.data.LowerStack;
        public RelayActions BothStack => this.data.BothStack;
        public RelayActions Antenna1 => this.data.Antenna1;
        public RelayActions Antenna2 => this.data.Antenna2;
        public RelayActions EnableAmpSwap => this.data.EnableAmpSwap;
        public RelayActions DisableAmpSwap => this.data.DisableAmpSwap;
    }
}
