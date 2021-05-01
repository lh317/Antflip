using System.Windows.Input;

using Antflip.USBRelay;

namespace Antflip.Pages {
    public class WARCBandContext {

        public WARCBandContext(ICommand actuate, RelayActions actions)
            => (this.ActuateCommand, this.Actions) = (actuate, actions);

        public ICommand ActuateCommand { get; init; }

        public RelayActions Actions { get; init; }
    }
}
