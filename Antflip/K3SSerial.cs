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
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Antflip
{
    public enum Antenna
    {
        [Display(Name = "ANT1")]
        Antenna1,
        [Display(Name = "ANT2")]
        Antenna2
    }

    public record K3SMessage()
    {
        private static K3SMessage FromDS(byte[] buffer, int start) {
            var antenna = (buffer[start + 10] & 0x20) == 0 ? Antflip.Antenna.Antenna1 : Antflip.Antenna.Antenna2;
            var display = new byte[8];
            var grouping = 0;
            for (var i = 0; i < 8; i++) {
                byte b = buffer[start + 2 + i];
                if ((b & 0x80) == 0x80) {
                    grouping = 0;
                }
                display[i] = (byte)(b & 0x7F);
                if (display[i] == (byte)'@') {
                    display[i] = (byte)' ';
                } else {
                    grouping += 1;
                }
            }
            var scale = grouping switch {
                2 => 10,
                1 => 100,
                _ => 1,
            };
            var number = Encoding.ASCII.GetString(display);
            Band? band = null;
            try {
                band = BandMethods.FromFrequency(Int32.Parse(number) * scale);
            } catch (Exception e) when (e is FormatException || e is ArgumentException) {
                // intentional suppress
            }
            return new K3SMessage{Antenna = antenna, Band = band};
        }

        private static K3SMessage FromIF(byte[] buffer, int start) {
            var freq = Encoding.ASCII.GetString(buffer, start + 2, 11);
            Band? band = null;
            try {
                band = BandMethods.FromFrequency(Int32.Parse(freq));
            } catch(Exception e) when (e is FormatException || e is ArgumentException) {
                // intentional suppress
            }
            bool transmit = buffer[start + 28] == (byte)'1';
            return new K3SMessage{Band = band, Transmit = transmit};
        }

        public static K3SMessage? Parse(byte[] buffer, int start, int length) {
            // Unclear what the encoding actually is
            var prefix = Encoding.ASCII.GetString(buffer, start, 2);
            return (prefix, length) switch {
                ("TQ", 3) => new K3SMessage{Transmit = buffer[start + 2] == (byte)'1'},
                ("DS", 12) => FromDS(buffer, start),
                ("IF", 37) => FromIF(buffer, start),
                _ => null,
            };
        }

        public bool Connected {get; init;} = true;

        public bool? Transmit {get; init;} = null;

        public Antenna? Antenna {get; init;} = null;

        public Band? Band {get; init;} = null;
    }

    public class K3SSerialClient : IDisposable
    {
        private static readonly byte[] FIRST_MSG = {(byte)'A', (byte)'I', (byte)'3', (byte)';'};
        private static readonly byte[] MSG = {(byte)'D', (byte)'S', (byte)';', (byte) 'T', (byte)'Q', (byte)';'};
        private readonly SerialPort serialPort;
        private readonly byte[] buffer = new byte[64];
        int offset = 0;

        public K3SSerialClient(String port, int baudRate) {
            this.serialPort = new SerialPort(port, baudRate);
        }

        public void Open() {
            if (!this.serialPort.IsOpen) {
                this.serialPort.Open();
            }
        }

        public async Task<List<K3SMessage>> ReceiveAsync() {
            this.Open();
            var messages = new List<K3SMessage>();
            while(true) {
                var memory = this.buffer.AsMemory(this.offset, this.buffer.Length - this.offset);
                this.offset += await this.serialPort.BaseStream.ReadAsync(memory);
                var start = 0;
                var semicolon = Array.FindIndex(this.buffer, start, this.offset - start, (b) => b == (byte)';');
                while (semicolon >= 0) {
                    var message = K3SMessage.Parse(this.buffer, start, semicolon - start);
                    if (null != message) {
                        messages.Add(message);
                    }
                    start = semicolon + 1;
                    semicolon = Array.FindIndex(this.buffer, start, this.offset - start, (b) => b == (byte)';');
                }
                Array.Copy(buffer, start, buffer, 0, this.offset - start);
                this.offset -= start;
                if (offset == this.buffer.Length) {
                    Array.Copy(buffer, 1, buffer, 0, this.buffer.Length - 1);
                    offset -= 1;
                }
                if (messages.Count > 0) {
                    return messages;
                }
            }
        }

        public async Task WriteFirstAsync() {
            this.Open();
            await this.serialPort.BaseStream.WriteAsync(FIRST_MSG);
        }

        public async Task WriteAsync() {
            this.Open();
            var remaining = this.serialPort.WriteBufferSize - this.serialPort.BytesToWrite;
            if (remaining >= MSG.Length) {
                await this.serialPort.BaseStream.WriteAsync(MSG);
            }
            await Task.Delay(200);
        }

        public void Dispose() {
            if (this.serialPort.IsOpen) {
                try{
                    serialPort.DiscardInBuffer();
                } catch(IOException) {
                    // intentional suppress
                }
                try {
                    serialPort.DiscardOutBuffer();
                } catch(IOException) {
                    // intentional suppress
                }
                serialPort.Close();
            }
            GC.SuppressFinalize(this);
        }
    }

    public class K3SMessageReceivedEventArgs : EventArgs
    {
        public K3SMessageReceivedEventArgs(K3SMessage message) => this.Message = message;

        public K3SMessage Message {get; init;}
    }

    public class K3SSerialControl : AsyncLoop<(string, int)>
    {
        private static readonly EventInstance eventInstance = new(0x8FFF03E8L, 0, EventLogEntryType.Warning);
        private bool first = true;

        public event EventHandler<K3SMessageReceivedEventArgs>? MessageReceived;

        protected override async Task Start((string, int) config, CancellationToken token) {
            var (port, baudRate) = config;
            try {
                if (!this.first) {
                    await Task.Delay(500, token);
                }
                this.first = false;
                using var client = new K3SSerialClient(port, baudRate);
                client.Open();
                this.MessageReceived?.Invoke(this, new(new()));
                var write = client.WriteFirstAsync();
                var recv = client.ReceiveAsync();
                while (true) {
                    token.ThrowIfCancellationRequested();
                    await Task.WhenAny(write, recv).WaitAsync(token);
                    if (recv.IsCompletedSuccessfully) {
                        foreach (var message in recv.Result) {
                            this.MessageReceived?.Invoke(this, new(message));
                        }
                    }
                    if (recv.IsCompleted) {
                        recv = client.ReceiveAsync();
                    }
                    if (write.IsCompleted) {
                        write = client.WriteAsync();
                    }
                }
            } catch(Exception e) {
                if (e is not OperationCanceledException && EventLog.SourceExists("Antflip")) {
                    try {
                        EventLog.WriteEvent("Antflip", eventInstance, port, baudRate, e.ToString());
                    } catch (Exception) {
                        // intentional suppress
                    }
                }
                this.MessageReceived?.Invoke(this, new(new K3SMessage{Connected=false}));
            }
        }
    }
}
