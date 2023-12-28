// Copyright 2021,2023 lh317
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
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

using Antflip.USBRelay;

namespace Antflip.Pages {
    public class WARCBandContext : BindableBase {
        private readonly MainWindowContext context;
        private readonly WARCBandData data;
        private bool pswapChecked;

        public WARCBandContext(MainWindowContext context, WARCBandData data) {
            this.context = context;
            this.data = data;
            context.PropertyChanged += this.DoAntennaChanged;
            this.pswapChecked = this.data.PSWAPEnable[Convert.ToInt32(context.Antenna ?? 0)];
        }

        public ICommand ActuateCommand => this.context.ActuateCommand;

        public RelayActions Load => this.data.Load;
        public RelayActions WARC => this.data.WARC;
        public SwitchData PSWAP => this.data.PSWAP;

        public bool PSWAPChecked {
            get => this.pswapChecked;
            set => Set(ref this.pswapChecked, value);
        }


        public void DoUnloaded(object? source, RoutedEventArgs? e) {
            this.context.PropertyChanged -= this.DoAntennaChanged;
        }

        protected void DoAntennaChanged(object? source, PropertyChangedEventArgs e) {
            if (e.PropertyName == "Antenna") {
                this.PSWAPChecked = this.data.PSWAPEnable[Convert.ToInt32(context.Antenna ?? 0)];
            }
        }
    }
}
