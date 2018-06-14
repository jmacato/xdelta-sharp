//
//  DecoderTests.cs
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
using System;
using System.IO;
using Xunit;

namespace Xdelta.UnitTests
{
    public class WindowReaderTests : IDisposable
    {
        private Decoder decoder;

        private MemoryStream input;
        private MemoryStream output;
        private MemoryStream patch;

        public WindowReaderTests()
        {
            input = new MemoryStream();
            output = new MemoryStream();
            patch = new MemoryStream();

            WriteGenericHeader();
            decoder = new Decoder(input, patch, output);
        }

        public void Dispose()
        {
            patch.Dispose();
            input.Dispose();
            output.Dispose();
        }

        private void WriteGenericHeader()
        {
            WriteBytes(0xD6, 0xC3, 0xC4, 0x00, 0x00);
        }

        private void WriteBytes(params byte[] data)
        {
            patch.Write(data, 0, data.Length);
            patch.Position -= data.Length;
        }

        private void TestThrows<T>(string message)
            where T : SystemException
        {
            T exception = Assert.Throws<T>(() => decoder.Run());
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void ThrowsIfWindowIndicatorWithAllBits()
        {
            WriteBytes(0x81, 0x7F);
            TestThrows<FormatException>("unrecognized window indicator bits set");
        }

        [Fact]
        public void ThrowsIfWindowIndicatorWithInvalidBit()
        {
            WriteBytes(0x08);
            TestThrows<FormatException>("unrecognized window indicator bits set");
        }

        [Fact]
        public void ThrowsIfWindowCopyOverflow()
        {
            WriteBytes(0x01, 0x10, 0x8F, 0xFF, 0xFF, 0xFF, 0x70);
            TestThrows<FormatException>("decoder copy window overflows a file offset");
        }

        [Fact]
        public void ThrowsIfWindowCopyWindowOverflow()
        {
            WriteBytes(0x02, 0x10, 0x04);
            TestThrows<FormatException>("VCD_TARGET window out of bounds");
        }

        [Fact]
        public void ThrowsIfWindowOverflow()
        {
            WriteBytes(0x01, 0x8F, 0xFF, 0xFF, 0xFF, 0x70, 0x00, 0x00, 0x10);
            TestThrows<FormatException>("decoder target window overflows a UInt32");
        }

        [Fact]
        public void ThrowsIfWindowHardMaximumSize()
        {
            WriteBytes(0x01, 0x04, 0x00, 0x00, 0x8F, 0xFF, 0xFF, 0xFF, 0x70);
            TestThrows<FormatException>("Hard window size exceeded");
        }

        [Fact]
        public void ThrowsIfAllFieldsCompressed()
        {
            WriteBytes(0x00, 0x00, 0x00, 0xFF);
            TestThrows<FormatException>("unrecognized delta indicator bits set");
        }

        [Fact]
        public void ThrowsExceptionIfInvalidFieldCompressed()
        {
            WriteBytes(0x00, 0x00, 0x00, 0xF8);
            TestThrows<FormatException>("unrecognized delta indicator bits set");
        }

        [Fact]
        public void ThrowsExceptionIfCompressedActivate()
        {
            WriteBytes(0x00, 0x00, 0x00, 0x01);
            TestThrows<FormatException>("invalid delta indicator bits set");
        }

        [Fact]
        public void TestValidWindowFields()
        {
            WriteBytes(0x05, 0x10, 0x81, 0x00, 0x04, 0x00, 0x00,
                0x04, 0x0, 0x02, 0x00, 0x00, 0x00, 0x01,
                0x0A, 0x0B, 0x0C, 0x0D, 0xCA, 0xFE);

            Assert.Null(Record.Exception(() => decoder.Run()));

            Assert.Equal(patch.Length, patch.Position);
            Window window = decoder.LastWindow;

            Assert.Equal(WindowFields.Source | WindowFields.Adler32, window.Source);
            Assert.Equal<uint>(0x10, window.SourceSegmentLength);
            Assert.Equal<uint>(0x80, window.SourceSegmentOffset);
            Assert.Equal<uint>(0x00, window.TargetWindowLength);
            Assert.Equal(WindowCompressedFields.None, window.CompressedFields);
            Assert.Equal(0x04, window.Data.BaseStream.Length);
            Assert.Equal(0x00, window.Instructions.BaseStream.Length);
            Assert.Equal(0x02, window.Addresses.BaseStream.Length);
            Assert.Equal<uint>(0x01, window.Checksum);
            Assert.Equal(new byte[] { 0xA, 0xB, 0xC, 0xD }, window.Data.ReadBytes(4));
            //Assert.Equal(new byte[] { 0x0F }, window.Instructions.ReadBytes(1)); // No instruction to process
            Assert.Equal(new byte[] { 0xCA, 0xFE }, window.Addresses.ReadBytes(2));
        }
    }
}

