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
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using CodeTiger.Threading;

namespace Antflip
{

    public class DirectionChangedEventArgs : EventArgs
    {
        public Band Band { get; init; }
        public double Azimuth { get; init; }

        public DirectionChangedEventArgs(Band band, double azimuth)
         => (this.Band, this.Azimuth) = (band, azimuth);
    }

    public class BandChangedEventArgs : EventArgs
    {
        public bool Cancel {get; set; } = false;

        public Band Band { get; init; }

        public BandChangedEventArgs(Band band) => this.Band = band;
    }

    public class N1MMRemoteControl : AsyncLoop<IPAddress>
    {
        public N1MMRemoteControl(String rotorName, Radio radio) => (this.RotorName, this.Radio) = (rotorName, radio);

        public event EventHandler<BandChangedEventArgs>? BandChanged;
        public event EventHandler<DirectionChangedEventArgs>? DirectionChanged;

        public String RotorName {get; set;}
        public Radio Radio {get; set;}
        public AsyncAutoResetEvent BandChangeDone {get;} = new AsyncAutoResetEvent(false);

        protected override async Task Start(IPAddress address, CancellationToken token) {
            using (var rotorClient = new N1MMRotorClient(address))
            using (var n1mmClient = new N1MMUdpClient(address)) {
                var rotorTask = rotorClient.ReceiveAsync();
                var udpTask = n1mmClient.ReceiveAsync();
                while (true) {
                    await Task.WhenAny(rotorTask, udpTask).WaitAsync(token);
                    if (rotorTask.IsCompletedSuccessfully) {
                        var message = rotorTask.Result;
                        if (message.Name == this.RotorName) {
                            var ev = new BandChangedEventArgs(message.Band);
                            if (null != this.BandChanged) {
                                this.BandChangeDone.Reset();
                                this.BandChanged.Invoke(this, ev);
                                await this.BandChangeDone.WaitOneAsync();
                            }
                            if (!ev.Cancel) {
                                this.DirectionChanged?.Invoke(this, new(message.Band, message.Azimuth));
                            }
                        }
                    }
                    if (udpTask.IsCompletedSuccessfully) {
                        var message = udpTask.Result;
                        if (message.Radio == this.Radio && null != this.BandChanged) {
                            this.BandChanged.Invoke(this, new(message.Band));
                        }
                    }
                    if (rotorTask.IsCompleted) {
                        rotorTask = rotorClient.ReceiveAsync();
                    }
                    if (udpTask.IsCompleted) {
                        udpTask = n1mmClient.ReceiveAsync();
                    }
                }
            }
        }
    }
}
