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
using System.Windows.Input;

using Antflip.USBRelay;

namespace Antflip.Pages {
    public class WARCBandContext {
        private readonly WARCBandData data;

        public WARCBandContext(ICommand actuate, WARCBandData data)
            => (this.ActuateCommand, this.data) = (actuate, data);

        public ICommand ActuateCommand { get; init; }

        public RelayActions Load => this.data.Load;
        public RelayActions WARC => this.data.WARC;

        public RelayActions EnableAmpSwap => this.data.EnableAmpSwap;
        public RelayActions DisableAmpSwap => this.data.DisableAmpSwap;
    }
}
