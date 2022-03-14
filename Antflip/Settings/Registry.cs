// Copyright 2022 lh317
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
using System.IO;
using System.Linq;
using System.Globalization;
using System.Security;

using Antflip.USBRelay;

namespace Antflip.Settings
{
    public static class Registry
    {
        public static string RotorName {
            get {
                try {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Antflip")) {
                        return key.GetValue("RotorName") as string ?? "antflip";
                    }
                } catch (Exception e) when (e is SecurityException || e is IOException || e is UnauthorizedAccessException) {
                }
                return "antflip";
            }
            set {
                try {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Antflip")) {
                        key.SetValue("RotorName", value);
                    }
                } catch (Exception e) when (e is SecurityException || e is IOException || e is UnauthorizedAccessException) {
                }
            }
        }

        public static Radio Radio {
            get {
                try {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Antflip")) {
                        var value = key.GetValue("Radio");
                        if (value != null) {
                            return (Radio)value;
                        }
                    }
                } catch (Exception e) when (e is SecurityException || e is IOException || e is UnauthorizedAccessException || e is InvalidCastException) {
                }
                return Radio.Radio1;
            }
            set {
                try {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Antflip")) {
                        key.SetValue("Radio", (int)value);
                    }
                } catch (Exception e) when (e is SecurityException || e is IOException || e is UnauthorizedAccessException) {
                }
            }
        }

        public static string? Interface {
            get {
                try {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Antflip")) {
                        return key.GetValue("Interface") as string;
                    }
                } catch (Exception e) when (e is SecurityException || e is IOException || e is UnauthorizedAccessException) {
                }
                return null;
            }
            set {
                if (value == null) {
                    return;
                }
                try {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Antflip")) {
                        key.SetValue("Interface", value);
                    }
                } catch (Exception e) when (e is SecurityException || e is IOException || e is UnauthorizedAccessException) {
                }
            }
        }


        public static void SortSavedBoardOrder(this IList<USBRelayBoard> boards) {
            var map = new Dictionary<string, int>();
            try {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Antflip\Boards")) {
                    foreach (var name in key.GetValueNames()) {
                        if (int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out var index)) {
                            var boardName = key.GetValue(name) as string;
                            if (boardName != null) {
                                map.Add(boardName, index);
                            }
                        }
                    }
                }
            } catch (Exception e) when (e is SecurityException || e is IOException || e is UnauthorizedAccessException) {

            }

            for (int index = 0; index < boards.Count; ++index) {
                var board = boards[index];
                if (map.TryGetValue(board.Name, out var newIndex)) {
                    if (newIndex >= 0 && newIndex < boards.Count) {
                        boards[index] = boards[newIndex];
                        boards[newIndex] = board;
                    }
                }
            }

        }

        public static void SaveBoardOrder(this IEnumerable<USBRelayBoard> boards) {
            try {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Antflip\Boards")) {
                    var indexes = boards.Select((board, index) => {
                        var indexStr = index.ToString(CultureInfo.InvariantCulture);
                        key.SetValue(indexStr, board.Name);
                        return indexStr;
                    }).ToHashSet();
                    foreach (var name in key.GetValueNames()) {
                        if (!indexes.Contains(name)) {
                            key.DeleteValue(name);
                        }
                    }
                }
            } catch (Exception e) when (e is ArgumentException || e is SecurityException || e is IOException || e is UnauthorizedAccessException) { }
        }


    }
}
