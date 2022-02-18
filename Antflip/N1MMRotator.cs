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
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Antflip
{
    public enum Band
    {
        Band160M,
        Band80M,
        Band40M,
        Band30M,
        Band20M,
        Band17M,
        Band15M,
        Band12M,
        Band10M
    }

    public static class BandMethods
    {
        public static Band Parse(string s) =>
            s switch {
                "1.8" => Band.Band160M,
                "3.5" => Band.Band80M,
                "7.0" => Band.Band40M,
                "10.0" => Band.Band30M,
                "14.0" => Band.Band20M,
                "18.0" => Band.Band17M,
                "21.0" => Band.Band15M,
                "24.0" => Band.Band12M,
                "28.0" => Band.Band10M,
                _ => throw new FormatException("{s} is not a valid band")
            };
    }

    public record N1MMRotorMessage(string Name, double Azimuth, double Offset, bool BiDirectional, Band Band)
    {
        public static N1MMRotorMessage Parse(byte[] packet) {
            string message = Encoding.UTF8.GetString(packet);
            var xml = XElement.Parse(message);
            var name = xml.Element("rotor")?.Value ?? throw new ArgumentException("packet has missing/invalid <rotor> tag");
            var azimuth = double.Parse(xml.Element("goazi")?.Value ?? throw new ArgumentException("packet has missing/invalid <goazi> tag"));
            var offset = double.Parse(xml.Element("offset")?.Value ?? throw new ArgumentException("packet has missing/invalid <offset> tag"));
            var i = int.Parse(xml.Element("bidirectional")?.Value ?? throw new ArgumentException("packet has missing/invalid <bidirectional> tag"));
            var bidi = Convert.ToBoolean(i);
            var band = BandMethods.Parse(xml.Element("freqband")?.Value ?? throw new ArgumentException("packet has missing/invalid <freqband> tag"));
            return new N1MMRotorMessage(name, azimuth, offset, bidi, band);
        }
    }

    public class N1MMRotorClient : IDisposable
    {
        private UdpClient client;

        public N1MMRotorClient(int port = 12040) => client = new UdpClient(port);

        public async ValueTask<N1MMRotorMessage> ReceiveAsync() {
            Debug.WriteLine("UDP listener started");
            while (true) {
                var packet = await this.client.ReceiveAsync();
                Debug.WriteLine("UDP packet received");
                try {
                    return N1MMRotorMessage.Parse(packet.Buffer);
                } catch (Exception e) when (e is ArgumentException || e is FormatException || e is OverflowException || e is XmlException) {

                }
            }
        }

        public void Dispose() {
            this.client.Dispose();
        }
    }
}
