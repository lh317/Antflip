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
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Antflip
{
    public abstract class AsyncLoop<T> : IDisposable
    {
        private CancellationTokenSource cancellationTokenSource = new();

        private Task? loop = null;

        public void Dispose() {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
            GC.SuppressFinalize(this);
        }

        public Task Restart(T value) {
            this.Cancel();
            this.loop = this.Start(value, this.cancellationTokenSource.Token);
            return this.loop;
        }

        public void Cancel() {
            if (loop != null) {
                this.cancellationTokenSource.Cancel();
                this.cancellationTokenSource.Dispose();
                this.cancellationTokenSource = new CancellationTokenSource();
                this.loop = null;
            }
        }

        protected abstract Task Start(T value, CancellationToken token);
    }
}
