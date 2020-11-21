using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamUtilities
{
    /// <summary>
    /// Stream that outputs random values.
    /// </summary>
    public class RandomStream : Stream
    {
        #region Private Fields

        private bool closed;
        private Random generator;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="seed">The seed of the internal random generator.</param>
        public RandomStream(int? seed) => generator = seed.HasValue ? new Random(seed.Value) : new Random();

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Always true.
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Always false.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Always false.
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Not supported.
        /// </summary>
        public override long Length => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

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
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Puts random bytes in the buffer.
        /// </summary>
        /// <param name="buffer">Buffer to write data into.</param>
        /// <param name="offset">Index in the buffer at which start writing data.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The number of byte read, or 0 if the stream has reached the end.</returns>
        /// <exception cref="ObjectDisposedException">The stream is closed or disposed.</exception>
        /// <exception cref="ArgumentNullException">'buffer' can not be null.</exception>
        /// <exception cref="ArgumentException">The buffer's length is too short.</exception>
        /// <exception cref="ArgumentOutOfRangeException">'offset' or 'count' can not be negative.</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length - offset < count)
                throw new ArgumentException("The 'buffer' length is too short.");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            return Read(buffer.AsSpan()[offset..(offset + count)]);
        }

        /// <summary>
        /// Puts random bytes in the buffer.
        /// </summary>
        /// <param name="buffer">Buffer to write data into.</param>
        /// <exception cref="ObjectDisposedException">The stream is closed or disposed.</exception>
        public override int Read(Span<byte> buffer)
        {
            CheckClosed();

            generator.NextBytes(buffer);
            return buffer.Length;
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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