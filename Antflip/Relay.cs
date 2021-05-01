
namespace Antflip {

    public class Relay : BindableBase {
        private string _label;
        public string Label {
            get => _label;
            set => Set(ref _label, value);
        }

        private bool _isOn = false;
        public bool IsOn {
            get => _isOn;
            set => Set(ref _isOn, value);
        }

        private bool _isConnected = false;
        public bool IsConnected {
            get => _isConnected;
            set => Set(ref _isConnected, value);
        }

        public Relay(string label) => this.Label = label;
    }
}
