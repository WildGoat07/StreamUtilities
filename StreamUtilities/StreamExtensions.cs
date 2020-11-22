using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StreamUtilities
{
    /// <summary>
    /// Provide some utility extension methods
    /// </summary>
    public static class StreamExtensions
    {
        #region Public Methods

        /// <summary>
        /// Reads data from the stream and write to the given value.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="value">Outout value to write into.</param>
        public static unsafe void Read<T>(this Stream stream, out T value) where T : unmanaged
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            var valueBytes = new byte[sizeof(T)];
            if (stream.Read(valueBytes, 0, valueBytes.Length) < valueBytes.Length)
                throw new ArgumentException("Not enough data to build the value");
            value = MemoryMarshal.AsRef<T>(valueBytes);
        }

        /// <summary>
        /// Reads data from the stream and write into the given span.
        /// </summary>
        /// <typeparam name="T">Type of the items in the span.</typeparam>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="span">Span to write into.</param>
        public static void Read<T>(this Stream stream, Span<T> span) where T : unmanaged
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (stream.Read(MemoryMarshal.AsBytes(span)) < span.Length)
                throw new ArgumentException("Not enough data to build the span");
        }

        /// <summary>
        /// Writes data into the stream.
        /// </summary>
        /// <typeparam name="T">Type of the items in the span.</typeparam>
        /// <param name="stream">Stream to write into.</param>
        /// <param name="span">Date to write into the stream.</param>
        public static void Write<T>(this Stream stream, ReadOnlySpan<T> span) where T : unmanaged
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            stream.Write(MemoryMarshal.AsBytes(span));
        }

        /// <summary>
        /// Writes data into the stream.
        /// </summary>
        /// <typeparam name="T">Type of the items in the span.</typeparam>
        /// <param name="stream">Stream to write into.</param>
        /// <param name="span">Date to write into the stream.</param>
        public static void Write<T>(this Stream stream, Span<T> span) where T : unmanaged => Write(stream, (ReadOnlySpan<T>)span);

        /// <summary>
        /// Write a copy of the value into the stream. Use it when the size of the value is smaller
        /// than the size of a reference (either 4 or 8 bytes).
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="stream">Stream to write into.</param>
        /// <param name="value">Value to write into the stream.</param>
        public static void WriteCopy<T>(this Stream stream, T value) where T : unmanaged => Write(stream, MemoryMarshal.CreateReadOnlySpan(ref value, 1));

        /// <summary>
        /// Write a reference of the value into the stream. Use it when the size of the value is
        /// bigger than the size of a reference (either 4 or 8 bytes).
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="stream">Stream to write into.</param>
        /// <param name="value">Value to write into the stream.</param>
        public static void WriteRef<T>(this Stream stream, ref T value) where T : unmanaged => Write(stream, MemoryMarshal.CreateReadOnlySpan(ref value, 1));

        #endregion Public Methods
    }
}