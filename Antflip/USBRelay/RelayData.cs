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
using System.Collections.Generic;

namespace Antflip.USBRelay {
    public interface ICustomLabels {
        public IReadOnlyDictionary<int, string> Labels { get; }
    }

    public record RelayActions {
        public IEnumerable<int> Open {get; init;} = new List<int>();
        public IEnumerable<int> Close {get; init;} = new List<int>();
        public bool Default { get; init; } = false;
    }

    public record RelayData
    {
        private static readonly SwitchData DefaultAmpSwap = new() {
            Enable = new RelayActions {
                Open = new List<int> { 7 },
            },
            Disable = new RelayActions {
                Close = new List<int> { 7 },
            }
        };

        private static readonly SwitchedBandData DefaultSwitchedBandData = new() {
            UpperStack = new RelayActions {
                Open = new List<int> { 8, 10 },
                Close = new List<int> { 9 },
                Default = true
            },
            LowerStack = new RelayActions {
                Open = new List<int> { 9, 10 },
                Close = new List<int> { 8 },
            },
            BothStack = new RelayActions {
                Close = new List<int> {8, 9, 10 },
            },
            // Default is not wired up in the UI for this action.
            DisableAmpSwap = DefaultAmpSwap.Disable,
            EnableAmpSwap = DefaultAmpSwap.Enable
        };

        private static readonly WARCBandData DefaultWARCBandData = new() {
            WARC = new RelayActions {
                Open = new List<int> { 6 },
                Close = new List<int> { 5 },
                Default = true,
            },
            EnableAmpSwap = DefaultAmpSwap.Enable with {
                Default = true
            },
            DisableAmpSwap = DefaultAmpSwap.Disable
        };

        public static RelayData DefaultRelayData => new() {
            Band160M = new DirectionalBandData {
                North = new RelayActions {
                    Close = new List<int> {1,2,3,5},
                    Open = new List<int> {0},
                    Default = true
                },
                NorthEast = new RelayActions {
                    Close = new List<int> {1,2,3,5},
                    Open = new List<int> {0}
                },
                East = new RelayActions {
                    Close = new List<int> {1,3,5},
                    Open = new List<int> {0, 2}
                },
                SouthEast = new RelayActions {
                    Close = new List<int> {0,1,3,5},
                    Open = new List<int> {2}
                },
                South = new RelayActions {
                    Close = new List<int> {0,1,3,5},
                    Open = new List<int> {2}
                },
                SouthWest = new RelayActions {
                    Close = new List<int> {0,3,5},
                    Open = new List<int> {1, 2}
                },
                West = new RelayActions {
                    Close = new List<int> {0,2,3,5},
                    Open = new List<int> {1}
                },
                NorthWest = new RelayActions {
                    Close = new List<int> {2,3,5},
                    Open = new List<int> {0, 1}
                },
                Omni = new RelayActions {
                    Close = new List<int> {3,5},
                    Open = new List<int> {0, 1, 2}
                },
                // Default is not wired up in the UI for this action.
                DisableAmpSwap = DefaultAmpSwap.Disable,
                EnableAmpSwap = DefaultAmpSwap.Enable,
                EnableUNUN = new RelayActions {
                    Open = new List<int> { 4 },
                },
                DisableUNUN = new RelayActions {
                    Close = new List<int> { 4 },
                }
            },
            Band80M = new DirectionalBandData {
                North = new RelayActions {
                    Close = new List<int> {0,1,2,5},
                    Open = new List<int> {3},
                    Default = true
                },
                NorthEast = new RelayActions {
                    Close = new List<int> {0,1,2,5},
                    Open = new List<int> {3}
                },
                East = new RelayActions {
                    Close = new List<int> {0,1,5},
                    Open = new List<int> {3, 2}
                },
                SouthEast = new RelayActions {
                    Close = new List<int> {0,1,3,5},
                    Open = new List<int> {2}
                },
                South = new RelayActions {
                    Close = new List<int> {0,1,3,5},
                    Open = new List<int> {2}
                },
                SouthWest = new RelayActions {
                    Close = new List<int> {0,3,5},
                    Open = new List<int> {1, 2}
                },
                West = new RelayActions {
                    Close = new List<int> {0,2,3,5},
                    Open = new List<int> {1}
                },
                NorthWest = new RelayActions {
                    Close = new List<int> {0,2,5},
                    Open = new List<int> {3, 1}
                },
                Omni = new RelayActions {
                    Close = new List<int> {0, 5},
                    Open = new List<int> {3, 1, 2}
                },
                // Default is not wired up in the UI for this action.
                DisableAmpSwap = DefaultAmpSwap.Disable,
                EnableAmpSwap = DefaultAmpSwap.Enable,
                EnableUNUN = new RelayActions {
                    Open = new List<int> { 4 },
                },
                DisableUNUN = new RelayActions {
                    Close = new List<int> { 4 },
                }
            },
            Band40M = DefaultSwitchedBandData with {
                Load = new RelayActions {
                    Open = new List<int> {5},

                }
            },

            Band30M = DefaultWARCBandData,
            Band20M = DefaultSwitchedBandData,
            Band17M = DefaultWARCBandData,
            Band15M = DefaultSwitchedBandData,
            Band12M = DefaultWARCBandData,
            Band10M = DefaultSwitchedBandData
        };

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

    public record DirectionalBandData : ICustomLabels {
        public IReadOnlyDictionary<int, string> Labels { get; init; } = new Dictionary<int, string>();

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

        public RelayActions EnableUNUN { get; init; } = new();
        public RelayActions DisableUNUN { get; init; } = new();

        public RelayActions EnableAmpSwap { get; init; } = new();
        public RelayActions DisableAmpSwap { get; init; } = new();
    }

    public record SwitchData {
        public RelayActions Load { get; init; } = new();
        public RelayActions Enable {get; init;} = new();
        public RelayActions Disable {get; init;} = new();
    }

    public record SwitchedBandData : ICustomLabels
    {
        public IReadOnlyDictionary<int, string> Labels { get; init; } = new Dictionary<int, string>();

        public RelayActions Load { get; init; } = new();

        public RelayActions UpperStack {get; init;} = new();
        public RelayActions LowerStack {get; init;} = new();
        public RelayActions BothStack {get; init;} = new();

        public RelayActions EnableAmpSwap {get; init;} = new();
        public RelayActions DisableAmpSwap {get; init;} = new();
    }

    public record WARCBandData
    {
        public RelayActions Load { get; init; } = new();

        public RelayActions WARC { get; init; } = new();

        public RelayActions EnableAmpSwap { get; init; } = new();
        public RelayActions DisableAmpSwap { get; init; } = new();

    }
}
