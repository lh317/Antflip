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
using System.Collections.Generic;

namespace Antflip.USBRelay
{
    public record RelayActions
    {
        public IEnumerable<int> Open {get; init;} = new List<int>();
        public IEnumerable<int> Close {get; init;} = new List<int>();
        public bool Default { get; init; } = false;
    }

    public record RelayData
    {
        public IReadOnlyList<string> Relays {get; init;} = new List<string>();
        public DirectionalBandData Band160M {get; init;} = new();
        public DirectionalBandData Band80M {get; init;} = new();
        public SwitchedBandData Band40M { get; init; } = new();
        public WARCBandData Band30M { get; init; } = new();
        public SwitchedBandData Band20M { get; init; } = new();
        public WARCBandData Band17M { get; init; } = new();
        public SwitchedBandData Band15M { get; init; } = new();
        public WARCBandData Band12M { get; init; } = new();
        public SwitchedBandData Band10M { get; init; } = new();
    }

    public record DirectionalBandData
    {
        public RelayActions Load { get; init; } = new();

        public RelayActions North {get; init;} = new();
        public RelayActions NorthEast {get; init;} = new();
        public RelayActions East {get; init;} = new();
        public RelayActions SouthEast {get; init;} = new();
        public RelayActions South {get; init;} = new();
        public RelayActions SouthWest {get; init;} = new();
        public RelayActions West {get; init;} = new();
        public RelayActions NorthWest {get; init;} = new();
        public RelayActions Omni {get; init;} = new();

        public SwitchData PSWAP {get; init;} = new();
        public SwitchData UNUN {get; init;} = new();
    }

    public record SwitchData
    {
        public RelayActions Enable {get; init;} = new();
        public RelayActions Disable {get; init;} = new();

        public bool Load {get; init; }= false;
    }

    public record SwitchedBandData
    {
        public RelayActions Load { get; init; } = new();

        public RelayActions UpperStack {get; init;} = new();
        public RelayActions LowerStack {get; init;} = new();
        public RelayActions BothStack {get; init;} = new();

        public SwitchData PSWAP {get; init;} = new();
    }

    public record WARCBandData
    {
        public RelayActions Load { get; init; } = new();

        public RelayActions WARC { get; init; } = new();

        public SwitchData PSWAP {get; init;} = new();
    }
}
