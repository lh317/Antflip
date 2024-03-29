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

        public static Band FromFrequency(int frequency) =>
            frequency switch {
                >= 1_800_000 and <= 2_000_000 => Band.Band160M,
                >= 3_500_000 and <= 4_000_000 => Band.Band80M,
                >= 7_000_000 and <= 7_300_000 => Band.Band40M,
                >= 10_000_000 and <= 10_150_000 => Band.Band30M,
                >= 14_000_000 and <= 14_350_000 => Band.Band20M,
                >= 18_000_000 and <= 18_168_000 => Band.Band17M,
                >= 21_000_000 and <= 21_450_000 => Band.Band15M,
                >= 24_890_000 and <= 25_000_000 => Band.Band12M,
                >= 28_000_000 and <= 29_700_000 => Band.Band10M,
                _ => throw new ArgumentException("{frequency} is not a valid band")
            };
    }

    public record N1MMRotorMessage(string Name, double Azimuth, double Offset, bool BiDirectional, Band Band)
    {
        public static N1MMRotorMessage Parse(byte[] packet) {
            // Unclear what the encoding actually is.
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
        private readonly UdpClient client;

        public N1MMRotorClient(IPAddress address, int port = 12040) {
            this.client = new UdpClient();
            client.Client.Bind(new IPEndPoint(address, port));
        }

        public async Task<N1MMRotorMessage> ReceiveAsync() {
            // Unfortunately N1MM sends mesages other than rotor messages on the
            // rotor message port.  So silently ignore anything that fails to
            // parse as a valid rotor message.
            while (true) {
                var packet = await this.client.ReceiveAsync();
                try {
                    return N1MMRotorMessage.Parse(packet.Buffer);
                } catch (Exception e) when (e is ArgumentException || e is FormatException || e is OverflowException || e is XmlException) {

                }
            }
        }

        public void Dispose() {
            this.client.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
