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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security;
using System.Windows;
using System.Windows.Input;

using ModernWpf.Controls;
using Tomlyn;

using Antflip.USBRelay;
using Antflip.Settings;

using ItemsControl = System.Windows.Controls.ItemsControl;
using System.Threading;

namespace Antflip.Pages
{

    public class ComboBoxContext : BindableBase
    {
        protected int selectedIndex = -1;
        public virtual int SelectedIndex {
            get => this.selectedIndex;
            set => this.Set(ref this.selectedIndex, value);
        }

        protected string text = "";
        public virtual string Text {
            get => this.text;
            set => this.Set(ref this.text, value);
        }

    }

    public class Interface : ComboBoxContext
    {
        private static bool IsIpv4ServerAddress(UnicastIPAddressInformation u) {
            return !u.IsTransient && u.Address.AddressFamily == AddressFamily.InterNetwork;
        }

        public Interface() {
            var ifaceId = Registry.Interface;
            if (ifaceId == null) {
                this.selectedIndex = GetDefaultInterfaceIndex();
            } else {
                this.selectedIndex = Array.FindIndex(Interfaces, i => i.Id == ifaceId);
                if (this.selectedIndex == -1) {
                    this.text = ifaceId;
                }
            }
        }

        public NetworkInterface[] Interfaces { get; } =
            NetworkInterface.GetAllNetworkInterfaces()
                            .Where(n => n.OperationalStatus == OperationalStatus.Up)
                            .Where(n => {
                                var uni = n.GetIPProperties()?.UnicastAddresses;
                                if (uni != null) {
                                    return uni.Any(IsIpv4ServerAddress);
                                }
                                return false;
                            })
                            .ToArray();

        private int GetDefaultInterfaceIndex() {
            try {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\N1MM Logger+")) {
                    if (key != null) {
                        var index = NetworkInterface.LoopbackInterfaceIndex;
                        var loopId = NetworkInterface.GetAllNetworkInterfaces()[index].Id;
                        return Array.FindIndex(Interfaces, i => i.Id == loopId);
                    }
                }
            } catch (Exception e) when (e is SecurityException) { }
            return 0;
        }

        public override int SelectedIndex {
            get => base.SelectedIndex;
            set {
                base.SelectedIndex = value;
                this.OnPropertyChanged("Address");
                if (-1 != value) {
                    Registry.Interface = this.Interfaces[value].Id;
                }
            }
        }

        public override string Text {
            get => base.Text;
            set {
                base.Text = value;
                if (this.SelectedIndex == -1) {
                    if (IPAddress.TryParse(value, out var _)) {
                        Registry.Interface = value;
                        this.OnPropertyChanged("Address");
                    }
                }
            }
        }

        public IPAddress? Address {
            get {
                if (this.selectedIndex != -1) {
                    var iface = this.Interfaces[this.SelectedIndex];
                    if (iface.NetworkInterfaceType == NetworkInterfaceType.Loopback) {
                        return IPAddress.Parse("127.0.0.2");
                    }
                    return iface.GetIPProperties().UnicastAddresses.First(IsIpv4ServerAddress).Address;
                }
                if (IPAddress.TryParse(this.Text, out var ret)) {
                    return ret;
                }
                return null;
            }
        }
    }

    public class ComPort : ComboBoxContext
    {
        public ComPort() {
            var portName = Registry.K3ComPort;
            if (portName == null) {
                this.selectedIndex = -1;
            } else {
                this.selectedIndex = Array.FindIndex(ComPorts, p => p == portName);
                this.text = portName;
            }
        }

        public String[] ComPorts { get; } = SerialPort.GetPortNames();

        public override int SelectedIndex {
            get => base.SelectedIndex;
            set {
                base.SelectedIndex = value;
                if (-1 != value) {
                    Registry.K3ComPort = this.ComPorts[value];
                }
            }
        }

        public override string Text {
            get => base.Text;
            set {
                base.Text = value;
                if (-1 == this.SelectedIndex) {
                    Registry.K3ComPort = value;
                }
            }
        }
    }

    public enum ConfigSource
    {
        [Display(Name="Built In")]
        BuiltIn = 1,
        [Display(Name="File")]
        File = 2
    }

    public class SettingsContext : BindableBase
    {
        private static readonly USBRelayBoard[] TEST_BOARDS = new USBRelayBoard[] {
            new(0, "Board 1", 8),
            new(1, "Board 2", 8)
        };

        private readonly USBRelayDriver usbDriver;
        private int configIndex;
        private RelayData relayData;
        private string rotorName = Registry.RotorName;
        private int radioIndex;

        public SettingsContext(USBRelayDriver usbDriver) {
            this.usbDriver = usbDriver;
            this.Boards = new(usbDriver);
#if DEBUG
            if (usbDriver.Count == 0) {
                this.Boards = new(TEST_BOARDS);
            }
#endif
            this.Boards.CollectionChanged += ((s, e) => this.DoBoardCollectionChanged());
            this.DoBoardCollectionChanged();
            this.configIndex = Array.IndexOf(Enum.GetValues<ConfigSource>(), Registry.ConfigSource);
            try {
                this.relayData = Toml.ToModel<TomlModel>(Registry.Config).ToRelayData();
            } catch(Exception e) when (e is TomlException || e is KeyNotFoundException) {
                this.relayData = TomlModel.DefaultRelayData.Value;
            }
            this.radioIndex = Array.IndexOf(this.Radios, Registry.Radio);
            var converter = new EnumToDisplayConverter();
            this.ConfigSourceNames = Enum.GetValues<ConfigSource>().Select(
                (v) => (string)converter.Convert(v, typeof(string), null, CultureInfo.InvariantCulture)!
            ).ToArray();
        }

        public ObservableCollection<USBRelayBoard> Boards { get; }

        public string[] ConfigSourceNames {get;}

        public int ConfigIndex {
            get => this.configIndex;
            set => this.Set(ref this.configIndex, value);
        }

        public RelayData RelayData {
            get => this.relayData;
            set => this.Set(ref this.relayData, value);
        }

        // Use DropDownClosed event as it indicates the user made a selection.
        // SelectionChanged fires even when code changes the selected value.
        public RelayCommand<object> ConfigDropDownClosedCommand => new(
            (_) => {
                if (-1 != this.configIndex) {
                    var source = Enum.GetValues<ConfigSource>()[this.configIndex];
                    Registry.ConfigSource = source;
                    var model = Registry.Config;
                    try {
                        this.RelayData = Toml.ToModel<TomlModel>(model).ToRelayData();
                    } catch(Exception e) when (e is TomlException || e is KeyNotFoundException) {}
                }
            }
        );

        public RelayCommand<object> BrowseCommand => new(
            (_) => {
                var dialog = new Microsoft.Win32.OpenFileDialog() {
                    Filter = "TOML Files (*.toml)|*.toml"
                };
                var result = dialog.ShowDialog();
                if (result == true) {
                    var content = File.ReadAllText(dialog.FileName);
                    try {
                        this.RelayData = Toml.ToModel<TomlModel>(content).ToRelayData();
                        Registry.Config = content;
                        this.ConfigIndex = Array.IndexOf(Enum.GetValues<ConfigSource>(), ConfigSource.File);
                        Registry.ConfigSource = ConfigSource.File;
                    } catch(TomlException e) {
                        MessageBox.Show(e.Diagnostics.ToString());
                    } catch(KeyNotFoundException e) {
                        MessageBox.Show(e.Message);
                    }
                }
            }
        );

        public Radio[] Radios { get; } = Enum.GetValues<Radio>();

        public int RadioIndex {
            get => this.radioIndex;
            set {
                this.Set(ref this.radioIndex, value);
                if (-1 != value) {
                    Registry.Radio = this.Radios[value];
                }
            }
        }

        public Radio SelectedRadio {
            get => this.Radios[RadioIndex];
        }


        public string RotorName {
            get => this.rotorName;
            set {
                this.Set(ref this.rotorName, value);
                Registry.RotorName = value;
            }
        }

        public Interface Interface { get; } = new Interface();

        public ComPort ComPort { get; } = new ComPort();

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

        private void DoBoardCollectionChanged() {
            this.Boards.SaveBoardOrder();
        }
    }
}
