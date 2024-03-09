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
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

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
            }
            return new K3SMessage{Antenna = antenna, Band = band};
        }

        public static K3SMessage? Parse(byte[] buffer, int start, int length) {
            // Unclear what the encoding actually is
            var prefix = Encoding.ASCII.GetString(buffer, start, 2);
            return (prefix, length) switch {
                ("TQ", 3) => new K3SMessage{Transmit = buffer[start + 2] == (byte)'1'},
                ("DS", 12) => FromDS(buffer, start),
                _ => null,
            };;

        }


        public bool? Transmit {get; init;} = null;

        public Antenna? Antenna {get; init;} = null;

        public Band? Band {get; init;} = null;
    }

    public class K3SSerialClient : IDisposable
    {
        private static readonly byte[] AI2 = {(byte)'A', (byte)'I', (byte)'2', (byte)';'};
        private readonly SerialPort serialPort;
        private readonly byte[] buffer = new byte[64];
        int offset = 0;

        public K3SSerialClient(String port) {
            this.serialPort = new SerialPort(port);
        }

        public async Task<List<K3SMessage>> ReceiveAsync() {
            var messages = new List<K3SMessage>();
            if (!this.serialPort.IsOpen) {
                try {
                    this.serialPort.Open();
                    await this.serialPort.BaseStream.WriteAsync(AI2);
                } catch(Exception) {
                    MessageBox.Show($"Failed to open serial port {serialPort.PortName}, please reconnect");
                    throw;
                }
            }
            while(true) {
                try {
                    this.offset += await this.serialPort.BaseStream.ReadAsync(this.buffer.AsMemory(this.offset, this.buffer.Length - offset));
                } catch(Exception) {
                    MessageBox.Show("Serial port error, please reconnect");
                    throw;
                }
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
                Array.Copy(buffer, start, buffer, 0, offset - start);
                offset -= start;
                if (offset == this.buffer.Length) {
                    Array.Copy(buffer, 1, buffer, 0, this.buffer.Length - 1);
                    offset -= 1;
                }
                if (messages.Count > 0) {
                    return messages;
                }
            }
        }

        public void Dispose() {
           serialPort.Close();
           GC.SuppressFinalize(this);
        }
    }

    public class K3SMessageReceivedEventArgs : EventArgs
    {
        public K3SMessageReceivedEventArgs(K3SMessage message) => this.Message = message;

        public K3SMessage Message {get; init;}
    }

    public class K3SSerialControl : AsyncLoop<string>
    {
        public event EventHandler<K3SMessageReceivedEventArgs>? MessageReceived;

        protected override async Task Start(string port, CancellationToken token) {
            using var client = new K3SSerialClient(port);
            var task = client.ReceiveAsync();
            while (true) {
                await task.WaitAsync(token);
                if (task.IsCompletedSuccessfully) {
                    var messages = task.Result;
                    messages.Reverse();
                    var tx = messages.FirstOrDefault(m => m?.Transmit != null, null);
                    if (tx != null) {
                        this.MessageReceived?.Invoke(this, new(tx));
                    }
                    var ant = messages.FirstOrDefault(m => m?.Antenna != null, null);
                    if (ant != null && ant != tx) {
                        this.MessageReceived?.Invoke(this, new(ant));
                    }
                }
                if (task.IsCompleted) {
                    task = client.ReceiveAsync();
                }
            }
        }
    }

}
