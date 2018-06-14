//
//  Test.cs
//
//  Author:
//       Benito Palacios Sánchez <benito356@gmail.com>
//
//  Copyright (c) 2015 Benito Palacios Sánchez
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using Xunit;
using System;
using System.IO;

namespace Xdelta.UnitTests
{
    public class HeaderReaderTests : IDisposable
    {
        private static readonly System.Text.Encoding Encoding = System.Text.Encoding.ASCII;

        private MemoryStream input;
        private MemoryStream output;
        private MemoryStream patch;

        public HeaderReaderTests()
        {
            input = new MemoryStream();
            output = new MemoryStream();
            patch = new MemoryStream();
        }

        public void Dispose()
        {
            patch.Dispose();
            input.Dispose();
            output.Dispose();
        }

        private void WriteBytes(params byte[] data)
        {
            patch.Write(data, 0, data.Length);
            patch.Position -= data.Length;
        }

        private void TestThrows<T>(string message)
            where T : SystemException
        {
            T exception = Assert.Throws<T>(() => new Decoder(input, patch, output));
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void InvalidStamp()
        {
            WriteBytes(0x00, 0xAA, 0xBB, 0xCC);
            TestThrows<FormatException>("not a VCDIFF input");
        }

        [Fact]
        public void InvalidVersion()
        {
            WriteBytes(0xD6, 0xC3, 0xC4, 0x01);
            TestThrows<FormatException>("VCDIFF input version > 0 is not supported");
        }

        [Fact]
        public void InvalidHeaderIndicator()
        {
            WriteBytes(0xD6, 0xC3, 0xC4, 0x00, 0xF8);
            TestThrows<FormatException>("unrecognized header indicator bits set");
        }

        [Fact]
        public void InvalidHeaderIndicator2()
        {
            WriteBytes(0xD6, 0xC3, 0xC4, 0x00, 0x48);
            TestThrows<FormatException>("unrecognized header indicator bits set");
        }

        [Fact]
        public void HasSecondaryCompressor()
        {
            WriteBytes(0xD6, 0xC3, 0xC4, 0x00, 0x01);
            TestThrows<NotSupportedException>("unavailable secondary compressor");
        }

        [Fact]
        public void HasCodeTable()
        {
            WriteBytes(0xD6, 0xC3, 0xC4, 0x00, 0x02);
            TestThrows<NotSupportedException>("compressed code table not implemented");
        }

        [Fact]
        public void DoesNotThrowExceptionIfNotHeader()
        {
            WriteBytes(0xD6, 0xC3, 0xC4, 0x00, 0x00);
            Assert.Null(Record.Exception(() => new Decoder(input, patch, output)));
        }

        [Fact]
        public void ReadCorrectlyApplicationData()
        {
            WriteBytes(0xD6, 0xC3, 0xC4, 0x00, 0x04, 0x07);

            patch.Position = 6;
            WriteBytes(Encoding.GetBytes("pleonex"));
            patch.Position = 0;

            Decoder decoder = new Decoder(input, patch, output);
            Assert.Equal("pleonex", decoder.Header.ApplicationData);
        }
    }
}

