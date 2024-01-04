// Copyright 2021-2024 lh317
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
    public class StackedBandContext : BindableBase {
        private readonly MainWindowContext context;
        private readonly SwitchedBandData data;
        private bool upperChecked;
        private bool bothChecked;
        private bool lowerChecked;
        private bool pswapChecked;

        public StackedBandContext(MainWindowContext context, SwitchedBandData data) {
            this.context = context;
            this.data = data;
            this.upperChecked = this.data.UpperStack.Default;
            this.bothChecked = this.data.BothStack.Default;
            this.lowerChecked = this.data.LowerStack.Default;
            // register now to ensure changes are seen during loading
            context.PropertyChanged += this.DoAntennaChanged;
            this.pswapChecked = this.data.PSWAPEnable[Convert.ToInt32(context.Antenna ?? 0)];
        }

        public ICommand ActuateCommand => this.context.ActuateCommand;
        public RelayActions Load => this.data.Load;
        public RelayActions UpperStack => this.data.UpperStack;
        public RelayActions LowerStack => this.data.LowerStack;
        public RelayActions BothStack => this.data.BothStack;
        public SwitchData PSWAP => this.data.PSWAP;

        public bool UpperChecked {
            get => this.upperChecked;
            set => Set(ref this.upperChecked, value);
        }
        public bool BothChecked {
            get => this.bothChecked;
            set => Set(ref this.bothChecked, value);
        }
        public bool LowerChecked {
            get => this.lowerChecked;
            set => Set(ref this.lowerChecked, value);
        }

        public bool PSWAPChecked {
            get => this.pswapChecked;
            set => Set(ref this.pswapChecked, value);
        }

        public void DoLoaded(object? source, RoutedEventArgs? e) {
            // Loaded may fire multiple times, so remove event handlers before installing.
            this.context.PropertyChanged -= this.DoAntennaChanged;
            this.context.PropertyChanged += this.DoAntennaChanged;
            if (this.context.UpCommand == null) {
                this.context.UpCommand = new RelayCommand<object>((_) => {
                    if (this.LowerChecked) {
                        this.BothChecked = true;
                    } else if (this.bothChecked) {
                        this.UpperChecked = true;
                    }
                });
                this.context.DownCommand = new RelayCommand<object>((_) => {
                    if (this.UpperChecked) {
                        this.BothChecked = true;
                    } else if (this.BothChecked) {
                        this.LowerChecked = true;
                    }
                });
            }
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
