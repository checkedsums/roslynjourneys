// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities;

using TypeCode = ObjectWriter.TypeCode;

/// <summary>
/// An <see cref="ObjectReader"/> that deserializes objects from a byte stream.
/// </summary>
internal sealed partial class ObjectReader : IDisposable
{
    /// <summary>
    /// We start the version at something reasonably random.  That way an older file, with 
    /// some random start-bytes, has little chance of matching our version.  When incrementing
    /// this version, just change VersionByte2.
    /// </summary>
    internal const byte VersionByte1 = 0b10101010;
    internal const byte VersionByte2 = 0b00001101;

    private readonly BinaryReader _reader;

    /// <summary>
    /// Map of reference id's to deserialized strings.
    /// </summary>
    private readonly ReaderReferenceMap _stringReferenceMap;

    /// <summary>
    /// Creates a new instance of a <see cref="ObjectReader"/>.
    /// </summary>
    /// <param name="stream">The stream to read objects from.</param>
    /// <param name="leaveOpen">True to leave the <paramref name="stream"/> open after the <see cref="ObjectWriter"/> is disposed.</param>
    private ObjectReader(Stream stream, bool leaveOpen)
    {
        // String serialization assumes both reader and writer to be of the same endianness.
        // It can be adjusted for BigEndian if needed.
        Debug.Assert(BitConverter.IsLittleEndian);

        _reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen);
        _stringReferenceMap = ReaderReferenceMap.Create();
    }

    /// <summary>
    /// Attempts to create a <see cref="ObjectReader"/> from the provided <paramref name="stream"/>.
    /// If the <paramref name="stream"/> does not start with a valid header, then <see langword="null"/> will
    /// be returned.
    /// </summary>
    public static ObjectReader? TryGetReader(Stream? stream, bool leaveOpen = false)
    {
        if (stream == null)
        {
            return null;
        }

        try
        {
            if (stream.ReadByte() != VersionByte1 ||
                stream.ReadByte() != VersionByte2)
            {
                return null;
            }
        }
        catch (AggregateException ex) when (ex.InnerException is not null)
        {
            // PipeReaderStream wraps any exception it throws in an AggregateException, which is not expected by
            // callers treating it as a normal stream. Unwrap and rethrow the inner exception for clarity.
            // https://github.com/dotnet/runtime/issues/70206
#if NET
            ExceptionDispatchInfo.Throw(ex.InnerException);
#else
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
#endif
        }

        return new ObjectReader(stream, leaveOpen);
    }

    /// <summary> 
    /// Creates an <see cref="ObjectReader"/> from the provided <paramref name="stream"/>. Unlike <see
    /// cref="TryGetReader(Stream, bool)"/>, it requires the version of the data in the stream to
    /// exactly match the current format version. Should only be used to read data written by the same version of
    /// Roslyn.
    /// </summary>
    public static ObjectReader GetReader(Stream stream, bool leaveOpen)
        => GetReader(stream, leaveOpen, checkValidationBytes: true);

    /// <summary> 
    /// <inheritdoc cref="GetReader(Stream, bool)"/>
    /// <param name="checkValidationBytes">Whether or not the validation bytes (see <see
    /// cref="ObjectWriter.WriteValidationBytes"/> should be checked immediately at the stream's current
    /// position.</param>
    /// </summary>
    public static ObjectReader GetReader(Stream stream, bool leaveOpen, bool checkValidationBytes)
    {
        var reader = new ObjectReader(stream, leaveOpen);
        if (checkValidationBytes)
            reader.CheckValidationBytes();

        return reader;
    }

    public void CheckValidationBytes()
    {
        var b = this.ReadByte();
        if (b != VersionByte1)
            throw ExceptionUtilities.UnexpectedValue(b);

        b = this.ReadByte();
        if (b != VersionByte2)
            throw ExceptionUtilities.UnexpectedValue(b);
    }

    public void Dispose()
    {
        _stringReferenceMap.Dispose();
    }

    public bool ReadBoolean() => _reader.ReadBoolean();
    public byte ReadByte() => _reader.ReadByte();
    // read as ushort because BinaryWriter fails on chars that are unicode surrogates
    public char ReadChar() => (char)_reader.ReadUInt16();
    public decimal ReadDecimal() => _reader.ReadDecimal();
    public double ReadDouble() => _reader.ReadDouble();
    public float ReadSingle() => _reader.ReadSingle();
    public int ReadInt32() => _reader.ReadInt32();
    public long ReadInt64() => _reader.ReadInt64();
    public sbyte ReadSByte() => _reader.ReadSByte();
    public short ReadInt16() => _reader.ReadInt16();
    public uint ReadUInt32() => _reader.ReadUInt32();
    public ulong ReadUInt64() => _reader.ReadUInt64();
    public ushort ReadUInt16() => _reader.ReadUInt16();
    public string? ReadString() => ReadStringValue();

    public string ReadRequiredString() => ReadString() ?? throw ExceptionUtilities.Unreachable();

    public Guid ReadGuid()
    {
        var accessor = new ObjectWriter.GuidAccessor
        {
            Low64 = ReadInt64(),
            High64 = ReadInt64()
        };

        return accessor.Guid;
    }

    public object? ReadScalarValue()
    {
        var code = (TypeCode)ReadByte();
        return code switch
        {
            TypeCode.Null => null,
            TypeCode.Boolean_True => true,
            TypeCode.Boolean_False => false,
            TypeCode.Int8 => ReadSByte(),
            TypeCode.UInt8 => ReadByte(),
            TypeCode.Int16 => ReadInt16(),
            TypeCode.UInt16 => ReadUInt16(),
            TypeCode.Int32 => ReadInt32(),
            TypeCode.Int32_1Byte => (int)ReadByte(),
            TypeCode.Int32_2Bytes => (int)ReadUInt16(),
            TypeCode.Int32_0 or TypeCode.Int32_1 or TypeCode.Int32_2 or TypeCode.Int32_3 or TypeCode.Int32_4 or TypeCode.Int32_5 or TypeCode.Int32_6 or TypeCode.Int32_7 or TypeCode.Int32_8 or TypeCode.Int32_9 or TypeCode.Int32_10 => (int)code - (int)TypeCode.Int32_0,
            TypeCode.UInt32 => ReadUInt32(),
            TypeCode.UInt32_1Byte => (uint)ReadByte(),
            TypeCode.UInt32_2Bytes => (uint)ReadUInt16(),
            TypeCode.UInt32_0 or TypeCode.UInt32_1 or TypeCode.UInt32_2 or TypeCode.UInt32_3 or TypeCode.UInt32_4 or TypeCode.UInt32_5 or TypeCode.UInt32_6 or TypeCode.UInt32_7 or TypeCode.UInt32_8 or TypeCode.UInt32_9 or TypeCode.UInt32_10 => (uint)((int)code - (int)TypeCode.UInt32_0),
            TypeCode.Int64 => ReadInt64(),
            TypeCode.UInt64 => ReadUInt64(),
            TypeCode.Float4 => ReadSingle(),
            TypeCode.Float8 => ReadDouble(),
            TypeCode.Decimal => ReadDecimal(),
            TypeCode.Char => (char)ReadUInt16(),// read as ushort because BinaryWriter fails on chars that are unicode surrogates
            TypeCode.StringUtf8 or TypeCode.StringUtf16 or TypeCode.StringRef_4Bytes or TypeCode.StringRef_1Byte or TypeCode.StringRef_2Bytes => ReadStringValue(code),
            TypeCode.DateTime => DateTime.FromBinary(ReadInt64()),
            _ => throw ExceptionUtilities.UnexpectedValue(code),
        };
    }

    public Encoding? ReadEncoding()
    {
        var code = (TypeCode)ReadByte();
        return code switch
        {
            TypeCode.Null => null,
            TypeCode.EncodingName => Encoding.GetEncoding(ReadRequiredString()),
            >= TypeCode.FirstWellKnownTextEncoding and <= TypeCode.LastWellKnownTextEncoding => ToEncodingKind(code).GetEncoding(),
            TypeCode.EncodingCodePage => Encoding.GetEncoding(ReadInt32()),
            _ => throw ExceptionUtilities.UnexpectedValue(code),
        };

        static TextEncodingKind ToEncodingKind(TypeCode code)
        {
            Debug.Assert(code is >= TypeCode.FirstWellKnownTextEncoding and <= TypeCode.LastWellKnownTextEncoding);
            return Microsoft.CodeAnalysis.EncodingExtensions.FirstTextEncodingKind + (byte)(code - TypeCode.FirstWellKnownTextEncoding);
        }
    }

    public byte[] ReadByteArray()
    {
        var (result, _) = ReadRawArray<byte>(static (reader, array, length) => reader.Read(array, 0, length));
        return result;
    }

    public char[] ReadCharArray()
    {
        var (result, _) = ReadCharArray(getArray: null);
        return result;
    }

    public (char[] array, int length) ReadCharArray(Func<int, char[]>? getArray)
        => ReadRawArray(static (reader, array, length) => reader.Read(array, 0, length), getArray);

    public (T[] array, int length) ReadRawArray<T>(
        Func<BinaryReader, T[], int, int> read,
        Func<int, T[]>? getArray = null)
    {
        // Defer to caller provided getArray if provided.  Otherwise, we'll just allocate the array ourselves.
        getArray ??= static length => length == 0 ? [] : new T[length];

        var length = ReadArrayLength();
        var array = getArray(length);

        var charsRead = read(_reader, array, length);

        return (array, charsRead);
    }

    internal uint ReadCompressedUInt()
    {
        var info = ReadByte();
        var marker = (byte)(info & ObjectWriter.ByteMarkerMask);
        var byte0 = (byte)(info & ~ObjectWriter.ByteMarkerMask);

        if (marker == ObjectWriter.Byte1Marker)
        {
            return byte0;
        }

        if (marker == ObjectWriter.Byte2Marker)
        {
            var byte1 = ReadByte();
            return (((uint)byte0) << 8) | byte1;
        }

        if (marker == ObjectWriter.Byte4Marker)
        {
            var byte1 = ReadByte();
            var byte2 = ReadByte();
            var byte3 = ReadByte();

            return (((uint)byte0) << 24) | (((uint)byte1) << 16) | (((uint)byte2) << 8) | byte3;
        }

        throw ExceptionUtilities.UnexpectedValue(marker);
    }

    private string? ReadStringValue()
    {
        var kind = (TypeCode)ReadByte();
        return kind == TypeCode.Null ? null : ReadStringValue(kind);
    }

    private string ReadStringValue(TypeCode kind)
    {
        return kind switch
        {
            TypeCode.StringRef_1Byte => _stringReferenceMap.GetValue(ReadByte()),
            TypeCode.StringRef_2Bytes => _stringReferenceMap.GetValue(ReadUInt16()),
            TypeCode.StringRef_4Bytes => _stringReferenceMap.GetValue(ReadInt32()),
            TypeCode.StringUtf16 or TypeCode.StringUtf8 => ReadStringLiteral(kind),
            _ => throw ExceptionUtilities.UnexpectedValue(kind),
        };
    }

    private unsafe string ReadStringLiteral(TypeCode kind)
    {
        string value;
        if (kind == TypeCode.StringUtf8)
        {
            value = _reader.ReadString();
        }
        else
        {
            // This is rare, just allocate UTF-16 bytes for simplicity.
            var characterCount = (int)ReadCompressedUInt();
            var bytes = _reader.ReadBytes(characterCount * sizeof(char));
            fixed (byte* bytesPtr = bytes)
            {
                value = new string((char*)bytesPtr, 0, characterCount);
            }
        }

        _stringReferenceMap.AddValue(value);
        return value;
    }

    private int ReadArrayLength()
        => (TypeCode)ReadByte() switch
        {
            TypeCode.Array_0 => 0,
            TypeCode.Array_1 => 1,
            TypeCode.Array_2 => 2,
            TypeCode.Array_3 => 3,
            _ => (int)this.ReadCompressedUInt(),
        };
}
