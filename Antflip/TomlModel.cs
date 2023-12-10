// Copyright 2023 lh317
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
        private static HashSet<string> MODES = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase){
            "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest", "omni"
        };
        private string? startMode;
        public string[] North {get; set;} = new string[0];
        public string[] Northeast {get; set;} = new string[0];
        public string[] East {get; set;} = new string[0];
        public string[] Southeast {get; set;} = new string[0];
        public string[] South {get; set;} = new string[0];
        public string[] Southwest {get; set;} = new string[0];
        public string[] West {get; set;} = new string[0];
        public string[] Northwest {get; set;} = new string[0];
        public string[] Omni {get; set;} = new string[0];
        public string[] Load {get; set;} = new string[0];
        public bool PSWAP {get; set;} = false;

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
                PSWAP = pswap with {
                    Load = this.PSWAP
                },
                UNUN = unun,
                Load = new RelayActions() {
                    Open = (from r in this.Load select relays[r]).ToList()
                }
            };
        }
    }

    public record SwitchedBandModel {
        private static HashSet<string> MODES = new HashSet<string>{"upper", "lower", "both"};
        private string? startMode;
        public string[]? Upper { get; set; }
        public string[]? Lower { get; set; }
        public string[]? Both { get; set; }
        public string[]? Load { get; set; }
        public bool PSWAP {get; set;} = false;

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

        public SwitchedBandData ToBandData(SwitchedBandModel defaults, IDictionary<string, int> relays, SwitchData pswap, SwitchData unun) {

            var upper = (from r in this.Upper ?? defaults.Upper select relays[r]).ToList();
            var lower = (from r in this.Lower ?? defaults.Lower select relays[r]).ToList();
            var both = (from r in this.Both ?? defaults.Both select relays[r]).ToList();
            var all = upper.ConcatMany(lower, both);
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
                PSWAP = pswap with {
                    Load = this.PSWAP || defaults.PSWAP
                },
                Load = new RelayActions() {
                    Open = (from r in this.Load ?? defaults.Load select relays[r]).ToList()
                }
            };
        }
    }

    public record WARCBandModel {
        public string[]? WARC { get; set; }
        public string[]? Load { get; set; }

        public bool PSWAP {get; set;} = false;

        public WARCBandData ToBandData(WARCBandModel defaults, IDictionary<string, int> relays, SwitchData pswap, SwitchData unun) {
            return new() {
                WARC = new RelayActions() {
                    Open = (from r in this.WARC ?? defaults.WARC select relays[r]).ToList(),
                    Default = true
                },
                PSWAP = pswap with {
                    Load = this.PSWAP || defaults.PSWAP
                },
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
pswap = true

[switched]
upper = ['UPPER', 'SINGLE']
lower = ['LOWER', 'SINGLE']
start = 'upper'

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
pswap = true
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
pswap = true
start = 'north'

[40m]
load = ['40M']
";
        public static readonly Lazy<RelayData> DefaultRelayData = new Lazy<RelayData>(
            () => Toml.ToModel<TomlModel>(DEFAULT).ToRelayData()
        );

        public string[] Relays { get; set; } = new string[0];
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
            this.Switched.Upper ??= new string[0];
            this.Switched.Lower ??= new string[0];
            this.Switched.Both ??= new string[0];
            this.Switched.Load ??= new string[0];
            this.Warc.WARC ??= new string[0];
            this.Warc.Load ??= new string[0];
            return new RelayData {
                Relays = this.Relays.ToList(),
                Band160M = this.Band160M.ToBandData(Band.Band160M, relays, pswap, unun),
                Band80M = this.Band80M.ToBandData(Band.Band80M, relays, pswap, unun) ,
                Band40M = this.Band40M.ToBandData(this.Switched, relays, pswap, unun),
                Band30M = this.Band30M.ToBandData(this.Warc, relays, pswap, unun),
                Band20M = this.Band20M.ToBandData(this.Switched, relays, pswap, unun),
                Band17M = this.Band17M.ToBandData(this.Warc, relays, pswap, unun),
                Band15M = this.Band15M.ToBandData(this.Switched, relays, pswap, unun),
                Band12M = this.Band12M.ToBandData(this.Warc, relays, pswap, unun),
                Band10M = this.Band10M.ToBandData(this.Switched, relays, pswap, unun),
            };
        }
    }
}
