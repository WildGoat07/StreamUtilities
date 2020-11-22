using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamUtilities
{
    /// <summary>
    /// Memory stream that handle Memory&lt;byte&gt;.
    /// </summary>
    public class AllocatedMemoryStream : Stream
    {
        #region Private Fields

        private bool closed;
        private Memory<byte>? memory;
        private long position;
        private ReadOnlyMemory<byte> roMemory;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="memory">Memory to wrap this stream around.</param>
        public AllocatedMemoryStream(Memory<byte> memory)
        {
            this.roMemory = memory;
            this.memory = memory;
            position = 0;
            closed = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="memory">Memory to wrap this stream around.</param>
        public AllocatedMemoryStream(ReadOnlyMemory<byte> memory)
        {
            this.roMemory = memory;
            this.memory = null;
            position = 0;
            closed = false;
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Always true.
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Always true.
        /// </summary>
        public override bool CanSeek => true;

        /// <summary>
        /// True if the stream is writable, false otherwise.
        /// </summary>
        public override bool CanWrite => memory.HasValue;

        /// <summary>
        /// Length of the allocated memory.
        /// </summary>
        public override long Length => roMemory.Length;

        /// <summary>
        /// Position in the stream.
        /// </summary>
        public override long Position { get => position; set => Seek(value, SeekOrigin.Begin); }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Closes the stream, deleting all data saved inside.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The stream is closed or disposed.</exception>
        public override void Close()
        {
            base.Close();
            CheckClosed();
            closed = true;
            roMemory = default;
            memory = default;
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Reads data from the stream.
        /// </summary>
        /// <param name="buffer">Buffer to write into.</param>
        /// <param name="offset">Index at which to start writing bytes.</param>
        /// <param name="count">Number of bytes to write.</param>
        /// <returns>Number of read bytes.</returns>
        /// <exception cref="ObjectDisposedException">The stream is closed or disposed.</exception>
        /// <exception cref="ArgumentNullException">'buffer' can not be null.</exception>
        /// <exception cref="ArgumentException">The buffer's length is too short.</exception>
        /// <exception cref="ArgumentOutOfRangeException">'offset' or 'count' can not be negative.</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckClosed();
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length - offset < count)
                throw new ArgumentException("The 'buffer' length is too short.");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            count = Math.Min(count, (int)(Length - Position));
            roMemory.Span[(int)position..(int)(position + count)].CopyTo(buffer.AsSpan()[offset..(offset + count)]);
            position += count;
            return count;
        }

        /// <summary>
        /// Reads data from the stream.
        /// </summary>
        /// <param name="buffer">Buffer to write into.</param>
        /// <returns>Number of read bytes.</returns>
        /// <exception cref="ObjectDisposedException">The stream is closed or disposed.</exception>
        public override int Read(Span<byte> buffer)
        {
            CheckClosed();
            var count = Math.Min(buffer.Length, (int)(Length - Position));
            roMemory.Span[(int)position..(int)(position + count)].CopyTo(buffer);
            position += count;
            return count;
        }

        /// <summary>
        /// Change the position in the stream.
        /// </summary>
        /// <param name="offset">Offset relative to the origin.</param>
        /// <param name="origin">From where to start moving the position.</param>
        /// <returns>The new position in the stream.</returns>
        /// <exception cref="ObjectDisposedException">The stream is closed or disposed.</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckClosed();
            return position = origin switch
            {
                SeekOrigin.Begin => Math.Min(Length, Math.Max(0, offset)),
                SeekOrigin.Current => Math.Min(Length, Math.Max(0, position + offset)),
                SeekOrigin.End => Math.Min(Length, Math.Max(0, Length + offset)),
                _ => position
            };
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>
        /// Writes data into the stream.
        /// </summary>
        /// <param name="buffer">Bytes to write into the stream.</param>
        /// <param name="offset">Index at which to start reading the buffer.</param>
        /// <param name="count">Number of bytes to write.</param>
        /// <exception cref="ObjectDisposedException">The stream is closed or disposed.</exception>
        /// <exception cref="ArgumentNullException">'buffer' can not be null.</exception>
        /// <exception cref="ArgumentException">The buffer's length is too short.</exception>
        /// <exception cref="ArgumentOutOfRangeException">'offset' or 'count' can not be negative.</exception>
        /// <exception cref="InvalidOperationException">The stream is too short or not writable.</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckClosed();
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length - offset < count)
                throw new ArgumentException("The 'buffer' length is too short.");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (!memory.HasValue)
                throw new InvalidOperationException("The stream is not writable.");
            if (count + position > Length)
                throw new InvalidOperationException("The stream is too short.");
            buffer.AsSpan()[offset..(offset + count)].CopyTo(memory.Value.Span[(int)position..]);
            position += count;
        }

        /// <summary>
        /// Writes data into the stream.
        /// </summary>
        /// <param name="buffer">Bytes to write into the stream.</param>
        /// <exception cref="ObjectDisposedException">The stream is closed or disposed.</exception>
        /// <exception cref="InvalidOperationException">The stream is too short or not writable.</exception>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            CheckClosed();
            if (!memory.HasValue)
                throw new InvalidOperationException("The stream is not writable.");
            if (buffer.Length + position > Length)
                throw new InvalidOperationException("The stream is too short.");
            buffer.CopyTo(memory.Value.Span[(int)position..]);
            position += buffer.Length;
        }

        #endregion Public Methods

        #region Private Methods

        private void CheckClosed()
        {
            if (closed)
                throw new ObjectDisposedException("The stream is closed", innerException: null);
        }

        #endregion Private Methods
    }
}