//
//  VcdiffReaderTests.cs
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
#define USE_32_BITS_INTEGERS

using System;
using System.IO;
using Xunit;

namespace Xdelta.UnitTests
{
    public class VcdReaderTests : IDisposable
    {
        VcdReader reader;
        Stream stream;

        public VcdReaderTests()
        {
            stream = new MemoryStream();
            reader = new VcdReader(stream);
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        private void WriteBytes(params byte[] data)
        {
            stream.Write(data, 0, data.Length);
			stream.Position -= data.Length;
        }

		private void TestThrows<T>(Action code, string message)
			where T : SystemException
		{
			T exception = Assert.Throws<T>(code);
			Assert.Equal(message, exception.Message);
		}

        [Fact]
        public void ReadByteWithExactSize()
        {
            WriteBytes(0x10);
            byte actual = reader.ReadByte();
            Assert.Equal(0x10, actual);
        }

        [Fact]
        public void ReadByteWithMoreBytes()
        {
            WriteBytes(0x9E);
            byte actual = reader.ReadByte();
            Assert.Equal(0x9E, actual);
            Assert.Equal(1, stream.Position);
        }

        [Fact]
        public void ReadBytes()
        {
            byte[] expected = new byte[] { 0xCA, 0xFE, 0xBE, 0xBE, 0xBE };
            WriteBytes(expected);
            byte[] actual = reader.ReadBytes(5);
            Assert.Equal(expected, actual);
        }

        #if USE_32_BITS_INTEGERS
        [Fact]
        public void ReadIntegerWithExactSize()
        {
            WriteBytes(0xBA, 0xEF, 0x9A, 0x15);
            uint actual = reader.ReadInteger();
            Assert.Equal<uint>(123456789, actual);
        }

        [Fact]
        public void ReadIntegerWithMoreBytes()
        {
            WriteBytes(0x88, 0x80, 0x80, 0x80, 0x00);
            uint actual = reader.ReadInteger();
            Assert.Equal(0x80000000, actual);
            Assert.Equal(5, stream.Position);
        }

        [Fact]
        public void ReadIntegerWithOverflowBits()
        {
            WriteBytes(0x80, 0x80, 0x80, 0x80, 0x80);
            TestThrows<FormatException>(
                () => reader.ReadInteger(),
                "overflow in decode_integer");
        }

        [Fact]
        public void ReadIntegerWithOverflowValue()
        {
            WriteBytes(0x90, 0x80, 0x80, 0x80, 0x80);
            TestThrows<FormatException>(
                () => reader.ReadInteger(),
                "overflow in decode_integer");
        }

        [Fact]
        public void ReadMoreThanAllowedBytes()
        {
            TestThrows<FormatException>(
                () => reader.ReadBytes(0x80000010),
                "Trying to read more than UInt32.MaxValue bytes");
        }
        #else
        [Fact]
        public void ReadUIntegerWithExactSize()
        {
            WriteBytes(0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01);
            ulong actual = reader.ReadUInteger();
            Assert.Equal(0x01, actual);
        }

        [Fact]
        public void ReadUIntegerWithMoreBytes1()
        {
            WriteBytes(0xC0, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01);
            ulong actual = reader.ReadUInteger();
            Assert.Equal(0x4000000000000001, actual);
            Assert.Equal(9, stream.Position);
        }

        [Fact]
        public void ReadUIntegerWithMoreBytes2()
        {
            WriteBytes(0x81, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01);
            ulong actual = reader.ReadUInteger();
            Assert.Equal(0x8000000000000001, actual);
            Assert.Equal(10, stream.Position);
        }

        [Fact]
        public void ReadUIntegerWithOverflowBits()
        {
            WriteBytes(0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x00);
            TestThrows<FormatException>(
                () => reader.ReadUInteger(),
                "overflow in decode_integer");
            Assert.Equal(10, stream.Position);
        }

        [Fact]
        public void ReadUIntegerWithOverflowValue()
        {
            WriteBytes(0x82, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x00);
            ulong actual = reader.ReadUInteger();
            Assert.Equal(0x00, actual);
        }

        [Fact]
        public void ReadInt64WithExactSize()
        {
            WriteBytes(0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01);
            long actual = reader.ReadInteger();
            Assert.Equal(0x01, actual);
        }

        [Fact]
        public void ReadIntegerWithMoreBytes1()
        {
            WriteBytes(0xC0, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01);
            long actual = reader.ReadInteger();
            Assert.Equal(0x4000000000000001, actual);
            Assert.Equal(9, stream.Position);
        }

        [Fact]
        public void ReadIntegerWithMoreBytes2()
        {
            WriteBytes(0x81, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x00);
            long actual = reader.ReadInteger();
            Assert.Equal(Int64.MinValue, actual);
            Assert.Equal(10, stream.Position);
        }

        [Fact]
        public void ReadIntegerWithOverflowBits()
        {
            WriteBytes(0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x00);
            TestThrows<FormatException>(
                () => reader.ReadInteger(),
                "overflow in decode_integer");
            Assert.Equal(10, stream.Position);
        }

        [Fact]
        public void ReadIntegerWithOverflowValue()
        {
            WriteBytes(0x82, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x00);
            long actual = reader.ReadInteger();
            Assert.Equal(0x00, actual);
        }
        #endif
    }
}

