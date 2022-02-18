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
using System.Threading;
using System.Threading.Tasks;


namespace Antflip {
    public static class TaskExtensions
    {
        public static async Task<T> WaitAsync<T>(this Task<T> task, CancellationToken token) {
            var source = new TaskCompletionSource<T>();
            token.Register(() => source.TrySetCanceled());
            var completed = await Task.WhenAny(task, source.Task);
            return await completed;
        }
    }
}
