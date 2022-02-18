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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Antflip
{
    public enum Radio {
        Radio1 = 1,
        Radio2 = 2
    }

    public record N1MMRadioMessage(Radio Radio, int Frequency) : IBand {
        public static N1MMRadioMessage Parse(byte[] packet) {
            string message = Encoding.UTF8.GetString(packet);
            var xml = XDocument.Parse(message).Root;
            var radio = int.Parse(xml?.Element("RadioNr")?.Value ?? throw new FormatException("packet has missing/invalid <RadioNr> tag"));
            var freq = int.Parse(xml?.Element("Freq")?.Value ?? throw new FormatException("packet has missing/invalid <Freq> tag")) * 10;
            return new N1MMRadioMessage((Radio)radio, freq);
        }

        public Band Band {
            get => BandMethods.FromFrequency(this.Frequency);
        }
    }

    public class N1MMUdpClient : IDisposable
    {
        private UdpClient client;

        public N1MMUdpClient(int port = 12060) {
            this.client = new UdpClient();
            //this.client.EnableBroadcast = true;
            var loopback = IPAddress.Parse("127.0.0.2");
            client.Client.Bind(new IPEndPoint(loopback, port));
        }

        public async Task<N1MMRadioMessage> ReceiveAsync() {
            while (true) {
                var packet = await this.client.ReceiveAsync();
                try {
                    return N1MMRadioMessage.Parse(packet.Buffer);
                } catch (Exception e) when (e is ArgumentException || e is FormatException || e is OverflowException || e is XmlException) {

                }
            }
        }

        public void Dispose() {
            this.client.Dispose();
        }
    }

}
