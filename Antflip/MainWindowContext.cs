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
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;

using CodeTiger.Threading;
using ModernWpf.Controls;

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
            return new StackedBandContext(context.ActuateCommand, this.Data);
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
            return new WARCBandContext(context.ActuateCommand, this.Data);
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


    public class ChangeDirectionEventArgs : EventArgs
    {
        public Band Band { get; init; }
        public double Azimuth { get; init; }

        public ChangeDirectionEventArgs(Band band, double azimuth)
         => (this.Band, this.Azimuth) = (band, azimuth);
    }

    public class BandChangingEventArgs : EventArgs
    {
        public Band Band { get; init; }

        public BandChangingEventArgs(Band band) => this.Band = band;
    }

    public sealed class MainWindowContext : BindableBase, IDisposable
    {

        private static readonly string[] labels = new string[] {
            "160N",
            "WEST",
            "SOUTH",
            "80N",
            "UNUN",
            "40M",
            "WARC",
            "PSWAP",
            "UPPER",
            "LOWER",
            "SINGLE",
        };

        private static readonly IReadOnlyList<Relay> relays = labels.Select(l => new Relay(l)).ToList();

        private readonly USBRelayDriver usbDriver = new();
        private IUSBRelayControl? usbRelay;
        private readonly SettingsContext settings;
        private AsyncManualResetEvent contextCreated = new(false);
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Task? remoteLoop = null;
        // Must be an object to support settings page.
        private object? selectedItem = null;

        public MainWindowContext() {
            this.settings = new SettingsContext(usbDriver);
            this.settings.Boards.CollectionChanged += ((s, e) => this.DoBoardCollectionChanged());
            this.settings.Interface.PropertyChanged += this.DoInterfacePropertyChanged;
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
            this.DoBoardCollectionChanged();
            var address = this.settings.Interface.Address;
            if (address != null) {
                this.remoteLoop = this.RemoteControlAsync(address, cancellationTokenSource.Token);
            }
        }

        public event EventHandler<BandChangingEventArgs>? BandChanging;
        public event EventHandler<ChangeDirectionEventArgs>? ChangeDirection;

        public object? SelectedItem {
            get { return this.selectedItem; }
            set { this.Set(ref this.selectedItem, value); }
        }

        public IReadOnlyList<Relay> Relays { get; } = relays;

        [SuppressMessage("Microsoft.Design", "CA1822", Justification = "WPF Binding")]
        public RelayData RelayData => RelayData.DefaultRelayData;

        public IReadOnlyList<MenuItem> MenuItems { get; }

        [SuppressMessage("Microsoft.Design", "CA1822", Justification = "WPF Binding")]
        public Type SettingsPage => typeof(Pages.Settings);

        public MenuItemToPageConverter MenuItemToPage { get; } = new MenuItemToPageConverter();

        public ICommand ActuateCommand { get; }

        private void DoInterfacePropertyChanged(object? source, PropertyChangedEventArgs e) {
            if (e.PropertyName == "Address") {
                var address = this.settings.Interface.Address;
                if (address != null) {
                    if (this.remoteLoop != null) {
                        this.cancellationTokenSource.Cancel();
                        this.cancellationTokenSource.Dispose();
                        this.cancellationTokenSource = new CancellationTokenSource();
                    }
                    var token = this.cancellationTokenSource.Token;
                    this.remoteLoop = this.RemoteControlAsync(address, token);
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

        private bool ChangeBand(Band band) {
            var bandItem = this.MenuItems[(int)band];
            if (bandItem != this.SelectedItem) {
                this.BandChanging?.Invoke(this, new(band));
                this.SelectedItem = bandItem;
                return true;
            }
            return false;
        }
        private async Task RemoteControlAsync(IPAddress address, CancellationToken token) {
            using (var rotorClient = new N1MMRotorClient(address))
            using (var n1mmClient = new N1MMUdpClient(address)) {
                var rotorTask = rotorClient.ReceiveAsync();
                var udpTask = n1mmClient.ReceiveAsync();
                while (true) {
                    await Task.WhenAny(rotorTask, udpTask).WaitAsync(token);
                    if (rotorTask.IsCompletedSuccessfully) {
                        var message = rotorTask.Result;
                        if (message.Name == this.settings.RotorName) {
                            if (this.ChangeBand(message.Band)) {
                                await this.contextCreated.WaitOneAsync();
                            }
                            this.ChangeDirection?.Invoke(this, new(message.Band, message.Azimuth));
                        }
                    }
                    if (udpTask.IsCompletedSuccessfully) {
                        var message = udpTask.Result;
                        if (message.Radio == this.settings.SelectedRadio) {
                            this.ChangeBand(message.Band);
                        }
                    }
                    if (rotorTask.IsCompleted) {
                        rotorTask = rotorClient.ReceiveAsync();
                    }
                    if (udpTask.IsCompleted) {
                        udpTask = n1mmClient.ReceiveAsync();
                    }
                }
            }
        }

        public void OnNavigated(object sender, NavigationEventArgs e) {
            this.usbDriver.CloseAll();
            foreach (var relay in this.Relays) {
                relay.IsOn = false;
            }
            var page = (Page)e.Content;
            if (page.GetType() == this.SettingsPage) {
                page.DataContext = this.settings;
            } else {
                page.DataContext = (this.selectedItem as MenuItem)?.MakeContext(this) ?? throw new InvalidOperationException();
                this.contextCreated.Set();
                this.contextCreated.Reset();
            }
        }

        public void Dispose() {
            this.cancellationTokenSource.Cancel();
            this.usbDriver.Dispose();
            this.cancellationTokenSource.Dispose();
        }
    }
}
