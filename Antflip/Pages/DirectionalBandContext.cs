// Copyright 2021-2022 lh317
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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

using Antflip.USBRelay;

namespace Antflip.Pages
{
    public class DirectionalBandContext : BindableBase
    {
        private readonly DirectionalBandData data;
        private bool ununChecked;
        private bool ununEnabled;
        private bool northChecked;
        private bool northEastChecked;
        private bool eastChecked;
        private bool southEastChecked;
        private bool southChecked;
        private bool southWestChecked;
        private bool westChecked;
        private bool northWestChecked;
        private bool omniChecked;


        // public DirectionalBandContext(ICommand actuate, DirectionalBandData data)
        //     => (this.actuate, this.data, this.current) = (actuate, data, null);

        public DirectionalBandContext(MainWindowContext context, DirectionalBandData data) {
            this.data = data;
            this.northChecked = this.data.North.Default;
            this.northEastChecked = this.data.NorthEast.Default;
            this.eastChecked = this.data.East.Default;
            this.southEastChecked = this.data.SouthEast.Default;
            this.southChecked = this.data.South.Default;
            this.southWestChecked = this.data.SouthWest.Default;
            this.westChecked = this.data.West.Default;
            this.northWestChecked = this.data.NorthWest.Default;
            this.omniChecked = this.data.Omni.Default;
            this.UNUNChecked = this.data.EnableUNUN.Default;
            this.UNUNEnabled = true;
            this.ActuateCommand = context.ActuateCommand;
            WeakEventManager<MainWindowContext, BandChangingEventArgs>.AddHandler(context, "BandChanging", DoBandChanging);
            WeakEventManager<MainWindowContext, ChangeDirectionEventArgs>.AddHandler(context, "ChangeDirection", DoChangeDirection);
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

        public bool NorthChecked {
            get => this.northChecked;
            set => Set(ref this.northChecked, value);
        }

        public bool NorthEastChecked {
            get => this.northEastChecked;
            set => Set(ref this.northEastChecked, value);
        }

        public bool EastChecked {
            get => this.eastChecked;
            set => Set(ref this.eastChecked, value);
        }

        public bool SouthEastChecked {
            get => this.southEastChecked;
            set => Set(ref this.southEastChecked, value);
        }

        public bool SouthChecked {
            get => this.southChecked;
            set => Set(ref this.southChecked, value);
        }

        public bool SouthWestChecked {
            get => this.southWestChecked;
            set => Set(ref this.southWestChecked, value);
        }

        public bool WestChecked {
            get => this.westChecked;
            set => Set(ref this.westChecked, value);
        }

        public bool NorthWestChecked {
            get => this.northWestChecked;
            set => Set(ref this.northWestChecked, value);
        }

        public bool OmniChecked {
            get => this.omniChecked;
            set => Set(ref this.omniChecked, value);
        }

        public bool UNUNChecked {
            get => this.ununChecked;
            set => Set(ref this.ununChecked, value);
        }

        public bool UNUNEnabled {
            get => this.ununEnabled;
            set => Set(ref this.ununEnabled, value);
        }

        protected void DoBandChanging(object? source, BandChangingEventArgs? e) {
            var context = source as MainWindowContext ?? throw new ArgumentNullException();
            WeakEventManager<MainWindowContext, ChangeDirectionEventArgs>.RemoveHandler(
                context, "ChangeDirection", DoChangeDirection
            );
            WeakEventManager<MainWindowContext, BandChangingEventArgs>.RemoveHandler(
                context, "BandChanging", DoBandChanging
            );
        }

        protected void DoChangeDirection(object? source, ChangeDirectionEventArgs? e) {
            switch (e?.Azimuth) {
                case 360.0:
                    this.OmniChecked = true;
                    break;
                case >= 337.5 or < 22.5:
                    this.NorthChecked = true;
                    break;
                case >= 22.5 and < 67.5:
                    this.NorthEastChecked = true;
                    break;
                case >= 67.5 and < 112.5:
                    this.EastChecked = true;
                    break;
                case >= 112.5 and < 157.5:
                    this.SouthEastChecked = true;
                    break;
                case >= 157.5 and < 202.5:
                    this.SouthChecked = true;
                    break;
                case >= 202.5 and < 247.5:
                    this.SouthWestChecked = true;
                    break;
                case >= 247.5 and < 292.5:
                    this.WestChecked = true;
                    break;
                case >= 292.5 and < 337.5:
                    this.NorthWestChecked = true;
                    break;
            }
        }
    }
}
