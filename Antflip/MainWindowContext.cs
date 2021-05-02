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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;

using Antflip.USBRelay;

using ModernWpf.Controls;
using Antflip.Pages;
using System.Collections.Specialized;

namespace Antflip
{
    public abstract class MenuItem
    {
        public string Content { get; }
        public abstract Type Page { get; }
        public abstract object Data { get; }

        public MenuItem(string content)
            => Content = content;

        public abstract object MakeContext(MainWindowContext context);
    }

    public class DirectionalMenuItem : MenuItem
    {
        public override Type Page => typeof(DirectionalBand);

        public override DirectionalBandData Data { get; }

        public DirectionalMenuItem(string content, DirectionalBandData data)
            : base(content)
            => this.Data = data;

        public override object MakeContext(MainWindowContext context) {
            return new DirectionalBandContext(context.ActuateCommand, this.Data);
        }
    }

    public class SwitchedMenuItem : MenuItem
    {
        public override Type Page => typeof(StackedBand);

        public override SwitchedBandData Data { get; }

        public SwitchedMenuItem(string content, SwitchedBandData data)
            : base(content)
            => this.Data = data;

        public override object MakeContext(MainWindowContext context) {
            return new StackedBandContext(context.ActuateCommand, this.Data);
        }
    }

    public class WARCMenuItem : MenuItem
    {
        public override Type Page => typeof(WARCBand);
        public override RelayActions Data { get; }

        public WARCMenuItem(string content, RelayActions data)
            : base(content)
            => this.Data = data;

        public override object MakeContext(MainWindowContext context) {
            return new WARCBandContext(context.ActuateCommand, this.Data);
        }
    }

    public class MenuItemToPageConverter : IValueConverter
    {
        private readonly MainWindowContext context;

        public MenuItemToPageConverter(MainWindowContext context) => this.context = context;

        // This method gets called when selecting (navigating) bands and maps the selected MenuItem object
        // to the Page to be displayed for the selected band.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (targetType.Equals(typeof(Page))) {
                context.selectedItem = (MenuItem)value;
                return context.selectedItem.Page;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public sealed class MainWindowContext : BindableBase, IDisposable
    {

        private static readonly string[] labels = new string[] {
            "160N",
            "WEST",
            "SOUTH",
            "80N",
            "UNUN",
            "ANT1",
            "WARC",
            "PSWAP",
            "UPPER",
            "LOWER"
        };

        private static readonly USBRelayBoard[] TEST_BOARDS = new USBRelayBoard[] {
            new("Board 1", 8),
            new("Board 2", 8)
        };

        private static readonly IReadOnlyList<Relay> relays = labels.Select(l => new Relay(l)).ToList();

        private readonly USBRelayDriver usbDriver = new();
        private readonly ObservableCollection<USBRelayBoard> boards;
        private USBRelayControl? usbRelay;

        internal MenuItem? selectedItem = null;

        public MainWindowContext() {
            this.MenuItemToPage = new(this);
            this.MenuItems = new List<MenuItem>{
                new DirectionalMenuItem("160M", this.RelayData.Band160M),
                new DirectionalMenuItem("80M", this.RelayData.Band80M),
                new SwitchedMenuItem("40M", this.RelayData.Band40M),
                new WARCMenuItem("30M", this.RelayData.Band30M),
                new SwitchedMenuItem("20M", this.RelayData.Band20M),
                new WARCMenuItem("17M", this.RelayData.Band17M),
                new SwitchedMenuItem("15M", this.RelayData.Band15M),
                new WARCMenuItem("12M", this.RelayData.Band12M),
                new SwitchedMenuItem("10M", this.RelayData.Band10M)
            };
            this.ActuateCommand = new RelayCommand<RelayActions>(
                a => this.usbRelay?.Actuate(a ?? throw new ArgumentNullException("Relay Actions Were null"))
            );
            this.boards = new(usbDriver);
            this.boards.CollectionChanged += ((s,e) => this.DoBoardCollectionChanged());
            this.DoBoardCollectionChanged();
        }

        public IReadOnlyList<Relay> Relays { get; } = relays;

        public RelayData RelayData => RelayData.DefaultRelayData;

        public IReadOnlyList<MenuItem> MenuItems { get; }

        public Type SettingsPage => typeof(Pages.Settings);

        public MenuItemToPageConverter MenuItemToPage { get; }

        public ICommand ActuateCommand { get; }

        private Visibility _ununVisible = Visibility.Hidden;
        public Visibility UNUNVisible {
            get => _ununVisible;
            set {
                this.Set(ref _ununVisible, value);
            }
        }

        private bool ununChecked = false;
        public bool UNUNChecked {
            get => ununChecked;
            set => this.Set(ref ununChecked, value);
        }

        private void DoBoardCollectionChanged() {
            if (null != this.usbRelay) {
                this.usbRelay.Opened -= this.DoRelayOpened;
                this.usbRelay.Closed -= this.DoRelayClosed;
            }
            this.usbRelay = usbDriver.ControlRelays(this.boards);
             var opened = this.usbRelay.GetOpenRelays().Select((o, i) => (this.Relays[i], o));
            foreach (var (relay, open) in opened) {
                relay.IsConnected = true;
                relay.IsOn = open;
            }
            this.usbRelay.Opened += this.DoRelayOpened;
            this.usbRelay.Closed += this.DoRelayClosed;
        }

        private void DoRelayOpened(object? source, USBRelayEventData e) {
            this.Relays[e.Index].IsOn = true;
        }

        private void DoRelayClosed(object? source, USBRelayEventData e) {
            this.Relays[e.Index].IsOn = false;
        }

        public void OnNavigated(object sender, NavigationEventArgs e) {
            this.usbDriver.CloseAll();
            foreach(var relay in this.Relays) {
                relay.IsOn = false;
            }
            this.UNUNChecked = false;
            var page = (Page)e.Content;
            if (page.GetType() == this.SettingsPage) {
                foreach (var (item, label) in this.Relays.Zip(labels)) {
                    item.Label = label;
                }
                page.DataContext = new SettingsContext(this.usbDriver, this.boards);
            } else {
                var customLabels = (this.selectedItem?.Data as ICustomLabels)?.Labels;
                foreach (var (item, i) in this.Relays.Select((value, i) => (value, i))) {
                    string? label = "";
                    if (customLabels?.TryGetValue(i, out label) == true) {
                        item.Label = label ?? "";
                    } else {
                        item.Label = labels[i];
                    }
                }
                var isDirectional = selectedItem?.Page == typeof(Pages.DirectionalBand);
                this.UNUNVisible = (isDirectional == true) ? Visibility.Visible : Visibility.Hidden;
                page.DataContext = this.selectedItem?.MakeContext(this) ?? throw new InvalidOperationException();
            }
        }

        public void Dispose() {
            this.usbDriver.Dispose();
        }
    }
}
