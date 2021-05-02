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
