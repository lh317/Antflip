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
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Antflip
{
    public record K3SMessage()
    {
        public static K3SMessage? Parse(byte[] buffer, int start, int length) {
            // Unclear what the encoding actually is
            var message = Encoding.ASCII.GetString(buffer, start, length);
            if (message.StartsWith("TQ")) {
                return new K3SMessage{Transmit= message[2] == '1'};
            } else {
                return null;
            }
        }

        public bool Transmit {get; init;} = false;
    }

    public class K3SSerialClient : IDisposable
    {
        private SerialPort serialPort;
        byte[] buffer = new byte[64];
        int offset = 0;

        public K3SSerialClient(String port) {
            this.serialPort = new SerialPort(port);
        }

        public async Task<List<K3SMessage>> ReceiveAsync() {
            var messages = new List<K3SMessage>();
            if (!this.serialPort.IsOpen) {
                try {
                    this.serialPort.Open();
                } catch(Exception) {
                    MessageBox.Show("Failed to open serial port {serialPort.PortName}, please reconnect");
                    throw;
                }
            }
            while(true) {
                try {
                    this.offset += await this.serialPort.BaseStream.ReadAsync(this.buffer, offset, this.buffer.Length - offset);
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
        }
    }

    public class TransmittingEventArgs : EventArgs
    {
        public bool Transmitting {get; set;}

        public TransmittingEventArgs(bool tx) => this.Transmitting = tx;
    }

    public class K3SSerialControl : AsyncLoop<string>
    {
        public event EventHandler<TransmittingEventArgs>? Transmitting;
        protected override async Task Start(string port, CancellationToken token) {
            using (var client = new K3SSerialClient(port)) {
                var task = client.ReceiveAsync();
                while (true) {
                    await task.WaitAsync(token);
                    if (task.IsCompletedSuccessfully) {
                        var messages = task.Result;
                        this.Transmitting?.Invoke(this, new (messages.Last().Transmit));
                    }
                    if (task.IsCompleted) {
                        task = client.ReceiveAsync();
                    }
                }
            }
        }
    }

}
