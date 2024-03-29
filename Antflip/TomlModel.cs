// Copyright 2023-2024 lh317
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

using Tomlyn;

using Antflip.USBRelay;
using System;

namespace Antflip
{
    public record DirectionalBandModel {
        private static readonly HashSet<string> MODES = new(StringComparer.InvariantCultureIgnoreCase){
            "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest", "omni"
        };

        private string? startMode;
        public string[] North {get; set;} = Array.Empty<string>();
        public string[] Northeast {get; set;} = Array.Empty<string>();
        public string[] East {get; set;} = Array.Empty<string>();
        public string[] Southeast {get; set;} = Array.Empty<string>();
        public string[] South {get; set;} = Array.Empty<string>();
        public string[] Southwest {get; set;} = Array.Empty<string>();
        public string[] West {get; set;} = Array.Empty<string>();
        public string[] Northwest {get; set;} = Array.Empty<string>();
        public string[] Omni {get; set;} = Array.Empty<string>();
        public string[] Load {get; set;} = Array.Empty<string>();
        public List<bool> PSWAP {get; set;} = new();

        public string? Start {
            get => this.startMode;
            set {
                if (value == null) {
                    startMode = null;
                } else if (MODES.TryGetValue(value, out var actual)) {
                    startMode = actual;
                } else {
                    throw new ArgumentException($"{value} must be one of {MODES}");
                }
            }
        }

        public DirectionalBandData ToBandData(Band band, IDictionary<string, int> relays, SwitchData pswap, SwitchData unun) {
            var north = (from r in this.North select relays[r]).ToList();
            var northEast = (from r in this.Northeast select relays[r]).ToList();
            var east = (from r in this.East select relays[r]).ToList();
            var southEast = (from r in this.Southeast select relays[r]).ToList();
            var south = (from r in this.South select relays[r]).ToList();
            var southWest = (from r in this.Southwest select relays[r]).ToList();
            var west = (from r in this.West select relays[r]).ToList();
            var northWest = (from r in this.Northwest select relays[r]).ToList();
            var omni = (from r in this.Omni select relays[r]).ToList();
            var all = north.ConcatMany(
                northEast, east, southEast, south, southWest, west, northWest, omni
            );
            if (this.PSWAP.Count == 0) {
                this.PSWAP.Add(true);
            }
            if (this.PSWAP.Count == 1){
                this.PSWAP.Add(!this.PSWAP[0]);
            }
            if (this.PSWAP.Count != 2) {
                throw new InvalidOperationException($"PSWAP has {PSWAP.Count} members, need <= 2");
            }
            return new() {
                Band = band,
                North = new RelayActions() {
                    Open = north,
                    Close = all.Except(north).ToList(),
                    Default = (startMode == "north")
                },
                NorthEast = new RelayActions() {
                    Open = northEast,
                    Close = all.Except(northEast).ToList(),
                    Default = (startMode == "northeast")
                },
                East = new RelayActions() {
                    Open = east,
                    Close = all.Except(east).ToList(),
                    Default = (startMode == "east")
                },
                SouthEast = new RelayActions() {
                    Open = southEast,
                    Close = all.Except(southEast).ToList(),
                    Default = (startMode == "southeast")
                },
                South = new RelayActions() {
                    Open = south,
                    Close = all.Except(south).ToList(),
                    Default = (startMode == "south")
                },
                SouthWest = new RelayActions() {
                    Open = southWest,
                    Close = all.Except(southWest).ToList(),
                    Default = (startMode == "southwest")
                },
                West = new RelayActions() {
                    Open = west,
                    Close = all.Except(west).ToList(),
                    Default = (startMode == "west")
                },
                NorthWest = new RelayActions() {
                    Open = northWest,
                    Close = all.Except(northWest).ToList(),
                    Default = (startMode == "northwest")
                },
                Omni = new RelayActions() {
                    Open = omni,
                    Close = all.Except(omni).ToList(),
                    Default = (startMode == "omni")
                },
                PSWAP = pswap,
                PSWAPEnable = this.PSWAP.ToArray(),
                UNUN = unun,
                Load = new RelayActions() {
                    Open = (from r in this.Load select relays[r]).ToList()
                }
            };
        }
    }

    public record SwitchedBandModel {
        private static readonly HashSet<string> MODES = new() { "upper", "lower", "both"};

        private string? startMode;
        public string[]? Upper { get; set; }
        public string[]? Lower { get; set; }
        public string[]? Both { get; set; }
        public string[]? Load { get; set; }
        public List<bool> PSWAP {get; set;} = new();

        public string? Start {
            get => this.startMode;
            set {
                if (value == null) {
                    startMode = null;
                } else if (MODES.TryGetValue(value, out var actual)) {
                    startMode = actual;
                } else {
                    throw new ArgumentException($"{value} must be one of {MODES}");
                }
            }
        }

        public SwitchedBandData ToBandData(SwitchedBandModel defaults, IDictionary<string, int> relays, SwitchData pswap) {

            var upper = (from r in this.Upper ?? defaults.Upper select relays[r]).ToList();
            var lower = (from r in this.Lower ?? defaults.Lower select relays[r]).ToList();
            var both = (from r in this.Both ?? defaults.Both select relays[r]).ToList();
            var all = upper.ConcatMany(lower, both);
            var pswapEnabled = (this.PSWAP.Count > 0) ? this.PSWAP : defaults.PSWAP;
            if (pswapEnabled.Count == 0) {
                pswapEnabled.Add(false);
            }
            if (pswapEnabled.Count == 1){
                pswapEnabled.Add(!pswapEnabled[0]);
            }
            if (pswapEnabled.Count != 2) {
                throw new InvalidOperationException($"PSWAP has {PSWAP.Count} members, need <= 2");
            }
            return new() {
                UpperStack = new RelayActions() {
                    Open = upper,
                    Close = all.Except(upper).ToList(),
                    Default = ((this.Start ?? defaults.Start) == "upper")
                },
                LowerStack = new RelayActions() {
                    Open = lower,
                    Close = all.Except(lower).ToList(),
                    Default = ((this.Start ?? defaults.Start) == "lower")
                },
                BothStack = new RelayActions() {
                    Open = both,
                    Close = all.Except(both).ToList(),
                    Default = ((this.Start ?? defaults.Start) == "both")
                },
                PSWAP = pswap,
                PSWAPEnable = pswapEnabled.ToArray(),
                Load = new RelayActions() {
                    Open = (from r in this.Load ?? defaults.Load select relays[r]).ToList()
                }
            };
        }
    }

    public record WARCBandModel {
        public string[]? WARC { get; set; }
        public string[]? Load { get; set; }

        public List<bool> PSWAP {get; set;} = new();

        public WARCBandData ToBandData(WARCBandModel defaults, IDictionary<string, int> relays, SwitchData pswap) {
            var pswapEnabled = (this.PSWAP.Count > 0) ? this.PSWAP : defaults.PSWAP;
            if (pswapEnabled.Count == 0) {
                pswapEnabled.Add(true);
            }
            if (pswapEnabled.Count == 1){
                pswapEnabled.Add(!pswapEnabled[0]);
            }
            if (pswapEnabled.Count != 2) {
                throw new InvalidOperationException($"PSWAP has {PSWAP.Count} members, need <= 2");
            }
            return new() {
                WARC = new RelayActions() {
                    Open = (from r in this.WARC ?? defaults.WARC select relays[r]).ToList(),
                    Default = true
                },
                PSWAP = pswap,
                PSWAPEnable = pswapEnabled.ToArray(),
                Load = new RelayActions() {
                    Open = (from r in this.Load ?? defaults.Load select relays[r]).ToList()
                }
            };
        }
    }

    public record TomlModel
    {

        public const string DEFAULT = @"
relays = ['160N', 'WEST', 'SOUTH', '80N', 'UNUN', '40M', 'WARC', 'PSWAP', 'UPPER', 'LOWER', 'SINGLE']
[warc]
warc = ['WARC']
pswap = [true, false]

[switched]
upper = ['UPPER', 'SINGLE']
lower = ['LOWER', 'SINGLE']
start = 'upper'
pswap = [false, true]

[160m]
north = ['160N']
northeast = ['160N']
east = ['160N', 'SOUTH', 'UNUN']
southeast = ['SOUTH']
south = ['SOUTH']
southwest = ['WEST', 'SOUTH', 'UNUN']
west = ['WEST']
northwest = ['160N', 'WEST']
omni = ['160N', 'WEST', 'SOUTH', 'UNUN']
pswap = [true, false]
start = 'north'

[80m]
north = ['80N']
northeast = ['80N']
east = ['80N', 'SOUTH']
southeast = ['SOUTH']
south = ['SOUTH']
southwest = ['WEST', 'SOUTH']
west = ['WEST']
northwest = ['80N', 'WEST']
omni = ['80N', 'WEST', 'SOUTH', 'UNUN']
pswap = [true, false]
start = 'north'

[40m]
load = ['40M']
";
        public static readonly Lazy<RelayData> DefaultRelayData = new(
            () => Toml.ToModel<TomlModel>(DEFAULT).ToRelayData()
        );

        public string[] Relays { get; set; } = Array.Empty<string>();
        public SwitchedBandModel Switched {get; set;} = new();
        public WARCBandModel Warc{ get; set;} = new();
        [DataMember(Name = "160m")]
        public DirectionalBandModel Band160M { get; set; } = new();
        [DataMember(Name = "80m")]
        public DirectionalBandModel Band80M { get; set; } = new();
        [DataMember(Name = "40m")]
        public SwitchedBandModel Band40M { get; set; } = new();

        [DataMember(Name = "30m")]
        public WARCBandModel Band30M { get; set; } = new();

        [DataMember(Name = "20m")]
        public SwitchedBandModel Band20M { get; set; } = new();

        [DataMember(Name = "17m")]
        public WARCBandModel Band17M { get; set; } = new();

        [DataMember(Name = "15m")]
        public SwitchedBandModel Band15M { get; set; } = new();

        [DataMember(Name = "12m")]
        public WARCBandModel Band12M { get; set; } = new();

        [DataMember(Name = "10m")]
        public SwitchedBandModel Band10M { get; set; } = new();

        public RelayData ToRelayData() {
            var relays = new Dictionary<string, int>(
                Relays.Select((r, i) => new KeyValuePair<string, int>(r, i)), StringComparer.CurrentCultureIgnoreCase
            );
            var pswapList = relays.TryGetValue("pswap", out var i) ? new List<int> { i} : new List<int>();
            var pswap = new SwitchData {
                Enable = new RelayActions{
                    Open = pswapList
                },
                Disable = new RelayActions {
                    Close = pswapList
                }
            };
            var ununList = relays.TryGetValue("unun", out i) ? new List<int> { i } : new List<int>();
            var unun = new SwitchData {
                Enable = new RelayActions{
                    Open = ununList
                },
                Disable = new RelayActions {
                    Close = ununList
                }
            };
            this.Switched.Upper ??= Array.Empty<string>();
            this.Switched.Lower ??= Array.Empty<string>();
            this.Switched.Both ??= Array.Empty<string>();
            this.Switched.Load ??= Array.Empty<string>();
            this.Warc.WARC ??= Array.Empty<string>();
            this.Warc.Load ??= Array.Empty<string>();
            return new RelayData {
                Relays = this.Relays.ToList(),
                Band160M = this.Band160M.ToBandData(Band.Band160M, relays, pswap, unun),
                Band80M = this.Band80M.ToBandData(Band.Band80M, relays, pswap, unun) ,
                Band40M = this.Band40M.ToBandData(this.Switched, relays, pswap),
                Band30M = this.Band30M.ToBandData(this.Warc, relays, pswap),
                Band20M = this.Band20M.ToBandData(this.Switched, relays, pswap),
                Band17M = this.Band17M.ToBandData(this.Warc, relays, pswap),
                Band15M = this.Band15M.ToBandData(this.Switched, relays, pswap),
                Band12M = this.Band12M.ToBandData(this.Warc, relays, pswap),
                Band10M = this.Band10M.ToBandData(this.Switched, relays, pswap),
            };
        }
    }
}
