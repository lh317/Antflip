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
