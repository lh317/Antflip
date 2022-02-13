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
using System.Collections.Generic;
using Microsoft.Win32;
using Antflip.USBRelay;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;
using System.Globalization;
using System.Security;
using System.IO;
using System;

namespace Antflip.Settings {
    public static class BoardRegistry {
        public static void SortSavedBoardOrder(this IList<USBRelayBoard> boards) {
            var map = new Dictionary<string, int>();
            try {
                using(var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Antflip\Boards")) {
                    foreach(var name in key.GetValueNames()) {
                        if (int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out var index)) {
                            var boardName = key.GetValue(name) as string;
                            if (boardName != null) {
                                map.Add(boardName, index);
                            }
                        }
                    }
                }
            } catch(Exception e) when (e is SecurityException || e is IOException || e is UnauthorizedAccessException) {

            }

            for (int index=0; index<boards.Count; ++index) {
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
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Antflip\Boards")) {
                    var indexes = boards.Select((board, index) => {
                        var indexStr = index.ToString(CultureInfo.InvariantCulture);
                        key.SetValue(indexStr, board.Name);
                        return indexStr;
                    }).ToHashSet();
                    foreach(var name in key.GetValueNames()) {
                        if (!indexes.Contains(name)) {
                            key.DeleteValue(name);
                        }
                    }
                }
            } catch(Exception e) when (e is ArgumentException || e is SecurityException || e is IOException || e is UnauthorizedAccessException) {}
        }
    }
}