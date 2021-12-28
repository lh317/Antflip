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
using System.Collections.Generic;
using System.Windows.Input;

using Antflip.USBRelay;

namespace Antflip.Pages
{
    public class DirectionalBandContext : BindableBase
    {
        private readonly DirectionalBandData data;
        private bool ununChecked;
        private bool ununEnabled;

        // public DirectionalBandContext(ICommand actuate, DirectionalBandData data)
        //     => (this.actuate, this.data, this.current) = (actuate, data, null);

        public DirectionalBandContext(ICommand actuate, DirectionalBandData data) {
            this.data = data;
            this.UNUNChecked = this.data.EnableUNUN.Default;
            this.UNUNEnabled = true;
            this.ActuateCommand = actuate;
        }

        public ICommand ActuateCommand { get; init; }
        public ICommand BandActuateCommand => new RelayCommand<RelayActions>(a => {
            bool check = false;
            bool enabled = true;
            if (null != a) {
                var open = new HashSet<int>(a.Open);
                if (open.Count == 1) {
                    enabled = false;
                } else if (open.IsSupersetOf(this.EnableUNUN.Open)) {
                    check = true;
                }
            }
            this.ActuateCommand.Execute(a);
            this.UNUNEnabled = enabled;
            this.UNUNChecked = check;
        });

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
        public RelayActions EnableUNUN => this.data.EnableUNUN;
        public RelayActions DisableUNUN => this.data.DisableUNUN;
        public RelayActions EnableAmpSwap => this.data.EnableAmpSwap;
        public RelayActions DisableAmpSwap => this.data.DisableAmpSwap;

        public bool UNUNChecked {
            get => this.ununChecked;
            set => Set(ref this.ununChecked, value);
        }

        public bool UNUNEnabled {
            get => this.ununEnabled;
            set => Set(ref this.ununEnabled, value);
        }
    }
}
