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
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

using Antflip.USBRelay;

using ModernWpf.Controls;

using ItemsControl = System.Windows.Controls.ItemsControl;

namespace Antflip.Pages {
    public class SettingsContext : BindableBase {

        private readonly USBRelayDriver usbDriver;

        public SettingsContext(USBRelayDriver usbDriver, ObservableCollection<USBRelayBoard> boards)
            => (this.usbDriver, this.Boards) = (usbDriver, boards);

        public ObservableCollection<USBRelayBoard> Boards { get; }

        private int _selectedIndex = -1;
        public int SelectedIndex {
            get => _selectedIndex;
            set => this.Set(ref _selectedIndex, value);
        }

        public ICommand MouseDownCommand => new RelayCommand<ListViewItem>(
            (lvi) => {
                var lv = (ListView)ItemsControl.ItemsControlFromItemContainer(lvi);
                this.SelectedIndex = lv.ItemContainerGenerator.IndexFromContainer(lvi);
            }
        );

        public ICommand MoveUpCommand => new RelayCommand<ListViewItem>(
            (lvi) => {
                // var lv = (ListView)ItemsControl.ItemsControlFromItemContainer(lvi);
                // var index = lv.ItemContainerGenerator.IndexFromContainer(lvi);
                // lv.SelectedIndex = index;
                var index = this.SelectedIndex;
                if (index > 0) {
                    USBRelayBoard temp = this.Boards[index - 1];
                    this.Boards[index - 1] = this.Boards[index];
                    this.Boards[index] = temp;
                    this.SelectedIndex = index - 1;
                }
            }
        );

        public ICommand MoveDownCommand => new RelayCommand<ListViewItem>(
            (lvi) => {
                // var lv = (ListView)ItemsControl.ItemsControlFromItemContainer(lvi);
                // var index = lv.ItemContainerGenerator.IndexFromContainer(lvi);
                // lv.SelectedIndex = index;
                var index = this.SelectedIndex;
                if (index + 1 < this.Boards.Count) {
                    USBRelayBoard temp = this.Boards[index + 1];
                    this.Boards[index + 1] = this.Boards[index];
                    this.Boards[index] = temp;
                    this.SelectedIndex = index + 1;
                }
            }
        );

        public ICommand AllOpenCommand => new RelayCommand<object>(
            (_) => this.usbDriver.OpenChannels(this.Boards[this.SelectedIndex])
        );

        public ICommand AllCloseCommand => new RelayCommand<object>(
            (_) => this.usbDriver.CloseChannels(this.Boards[this.SelectedIndex])
        );
    }
}
