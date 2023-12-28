// Copyright 2021-2023 lh317
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
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;

using CodeTiger.Threading;
using ModernWpf.Controls;
using Tomlyn;

using Antflip.Pages;
using Antflip.USBRelay;

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
            return new DirectionalBandContext(context, this.Data);
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
            return new StackedBandContext(context, this.Data);
        }
    }

    public class WARCMenuItem : MenuItem
    {
        public override Type Page => typeof(WARCBand);
        public override WARCBandData Data { get; }

        public WARCMenuItem(string content, WARCBandData data)
            : base(content)
            => (this.Data) = (data);

        public override object MakeContext(MainWindowContext context) {
            return new WARCBandContext(context, this.Data);
        }
    }

    public class MenuItemToPageConverter : IValueConverter
    {

        // This method gets called when selecting (navigating) bands and maps the selected MenuItem object
        // to the Page to be displayed for the selected band.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (targetType.Equals(typeof(Page))) {
                return ((MenuItem)value).Page;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public sealed class MainWindowContext : BindableBase, IDisposable
    {
        private readonly USBRelayDriver usbDriver = new();
        private IUSBRelayControl? usbRelay;
        private readonly SettingsContext settings;
        private IReadOnlyList<Relay> relays = null!;
        private IReadOnlyList<MenuItem> menuItems = null!;
        // Must be an object to support settings page.
        private object? selectedItem = null;
        private bool isEnabled = true;
        private Antenna? antenna = null;
        private readonly K3SSerialControl serialControl = new();
        private DateTime lastTransmitTimestamp = DateTime.Now;


        public MainWindowContext() {
            this.settings = new SettingsContext(usbDriver);
            this.settings.PropertyChanged += this.DoConfigPropertyChanged;
            this.settings.Boards.CollectionChanged += ((s, e) => this.DoBoardCollectionChanged());
            this.settings.Interface.PropertyChanged += this.DoInterfacePropertyChanged;
            this.settings.ComPort.PropertyChanged += this.DoComPortPropertyChanged;
            DoConfigPropertyChanged(this, new PropertyChangedEventArgs("RelayData"));
            this.ActuateCommand = new RelayCommand<RelayActions>(
                a => this.usbRelay?.Actuate(a ?? throw new ArgumentNullException("Relay Actions Were null"))
            );
            this.DoBoardCollectionChanged();
            this.RemoteControl = new N1MMRemoteControl(this.settings.RotorName, this.settings.SelectedRadio);
            this.RemoteControl.BandChanged += this.DoBandChanged;
            var address = this.settings.Interface.Address;
            if (address != null) {
                this.RemoteControl.Restart(address);
            }
            this.serialControl.MessageReceived += this.DoMessageReceived;
            var comPort = this.settings.ComPort.Text;
            if (null != comPort && comPort.Length > 0) {
                this.serialControl.Restart(comPort);
            }
        }

        public object? SelectedItem {
            get { return this.selectedItem; }
            set { this.Set(ref this.selectedItem, value); }
        }

        public IReadOnlyList<Relay> Relays {
            get => this.relays;
            set => this.Set(ref this.relays, value);
        }

        public IReadOnlyList<MenuItem> MenuItems {
            get => this.menuItems;
            set => this.Set(ref this.menuItems, value);
        }

        public bool IsEnabled {
            get => this.isEnabled;
            set => this.Set(ref this.isEnabled, value);
        }

        public ICommand ActuateCommand { get; }

        public N1MMRemoteControl RemoteControl {get; init;}

        public Antenna? Antenna {
            get => this.antenna;
            set => this.Set(ref this.antenna, value);
        }

        private void DoConfigPropertyChanged(object? source, PropertyChangedEventArgs e) {
            if (e.PropertyName == "RotorName") {
                this.RemoteControl.RotorName = this.settings.RotorName;
            }
            if (e.PropertyName == "RadioIndex") {
                this.RemoteControl.Radio = this.settings.SelectedRadio;
            }
            if (e.PropertyName == "RelayData") {
                var relayData = this.settings.RelayData;
                this.Relays = relayData.Relays.Select(l => new Relay(l)).ToList();
                this.MenuItems = new List<MenuItem>{
                    new DirectionalMenuItem("160M", relayData.Band160M),
                    new DirectionalMenuItem("80M", relayData.Band80M),
                    new SwitchedMenuItem("40M", relayData.Band40M),
                    new WARCMenuItem("30M", relayData.Band30M),
                    new SwitchedMenuItem("20M", relayData.Band20M),
                    new WARCMenuItem("17M", relayData.Band17M),
                    new SwitchedMenuItem("15M", relayData.Band15M),
                    new WARCMenuItem("12M", relayData.Band12M),
                    new SwitchedMenuItem("10M", relayData.Band10M)
                };
                DoBoardCollectionChanged();
            }
        }

        private void DoInterfacePropertyChanged(object? source, PropertyChangedEventArgs e) {
            if (e.PropertyName == "Address") {
                var address = this.settings.Interface.Address;
                if (address != null) {
                    this.RemoteControl.Restart(address);
                } else {
                    this.RemoteControl.Cancel();
                }
            }
        }

        private void DoComPortPropertyChanged(object? source, PropertyChangedEventArgs e) {
            if (e.PropertyName == "Text") {
                var comPort = this.settings.ComPort.Text;
                if (comPort != null && comPort.Length > 0) {
                    this.serialControl.Restart(comPort);
                } else {
                    this.serialControl.Cancel();
                }
            }
        }

        private void DoBoardCollectionChanged() {
            if (null != this.usbRelay) {
                this.usbRelay.Opened -= this.DoRelayOpened;
                this.usbRelay.Closed -= this.DoRelayClosed;
            }
#if DEBUG
            if (usbDriver.Count == 0) {
                this.usbRelay = new VirtualUSBRelayControl(16);
            } else {
                this.usbRelay = usbDriver.ControlRelays(this.settings.Boards);
            }
#else
            this.usbRelay = usbDriver.ControlRelays(this.settings.Boards);
#endif
            var opened = this.usbRelay.GetOpenRelays().Where((o, i) => i < this.Relays.Count).Select((o, i) => (this.Relays[i], o));
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

        private void DoBandChanged(object? source, BandChangedEventArgs e) {
            if (this.isEnabled) {
                var bandItem = this.MenuItems[(int)e.Band];
                if (bandItem != this.SelectedItem) {
                    this.SelectedItem = bandItem;
                } else {
                    this.RemoteControl.BandChangeDone.Set();
                }
            } else {
                e.Cancel = true;
                this.RemoteControl.BandChangeDone.Set();
            }
        }

        private async void DoMessageReceived(object? source, K3SMessageReceivedEventArgs e) {
            if (e.Message.Transmit == true) {
                this.lastTransmitTimestamp = DateTime.Now;
                this.IsEnabled = false;
            } else if(e.Message.Transmit == false) {
                await Task.Delay(100);
                if (this.lastTransmitTimestamp.AddMilliseconds(100) <= DateTime.Now) {
                    this.IsEnabled = true;
                }
            } else if (e.Message.Antenna is Antenna ant) {
                this.Antenna = ant;
            }
        }

        public void OnNavigating(object? sender, NavigatingCancelEventArgs e) {
            var frame = sender as Frame;
            var page = (Page)e.Content;
            if (page.GetType() == typeof(Pages.Settings)) {
                frame?.NavigationService.RemoveBackEntry();
                page.DataContext = this.settings;
            } else if(page.GetType() != typeof(Pages.Transmitting)) {
                if (e.NavigationMode != NavigationMode.Back || frame?.CurrentSourcePageType != typeof(Pages.Transmitting)) {
                    frame?.NavigationService.RemoveBackEntry();
                    this.usbDriver.CloseAll();
                    foreach (var relay in this.Relays) {
                        relay.IsOn = false;
                    }
                }
                page.DataContext ??= (this.selectedItem as MenuItem)?.MakeContext(this) ?? throw new InvalidOperationException();
                page.Loaded += this.OnPageLoaded;
            }
        }

        private void OnPageLoaded(object? sender, RoutedEventArgs e) {
            this.RemoteControl.BandChangeDone.Set();
            if (sender is Page page) {
                page.Loaded -= this.OnPageLoaded;
            }
        }

        public void Dispose() {
            this.RemoteControl.Dispose();
            this.usbDriver.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
