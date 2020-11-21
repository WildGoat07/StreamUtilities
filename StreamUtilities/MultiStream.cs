using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamUtilities
{
    /// <summary>
    /// The multi stream class allow to write into multiple streams at the same time.
    /// </summary>
    public class MultiStream : Stream
    {
        #region Private Fields

        private Stream[] streams;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="streams">Streams to write into. They must be writable.</param>
        public MultiStream(IEnumerable<Stream> streams) : this(streams.ToArray())
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="streams">Streams to write into. They must be writable.</param>
        public MultiStream(params Stream[] streams)
        {
            foreach (var stream in streams)
                if (!stream.CanWrite)
                    throw new ArgumentException("One of the streams can not be writen to.");
            this.streams = streams;
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Always false.
        /// </summary>
        public override bool CanRead => false;

        /// <summary>
        /// Always false.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Always true.
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">Not supported.</exception>
        public override long Length => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">Not supported.</exception>
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Closes all the streams.
        /// </summary>
        public override void Close()
        {
            base.Close();
            foreach (var stream in streams)
                stream.Close();
        }

        /// <summary>
        /// Flushes all the streams.
        /// </summary>
        public override void Flush()
        {
            foreach (var stream in streams)
                stream.Flush();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Not supported.</exception>
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Not supported.</exception>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="NotSupportedException">Not supported.</exception>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>
        /// Writes data into all the streams.
        /// </summary>
        /// <param name="buffer">Bytes to write into the streams.</param>
        /// <param name="offset">Index in <paramref name="buffer"/> at which to start writing.</param>
        /// <param name="count">Number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            foreach (var stream in streams)
                stream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Writes data into all the streams.
        /// </summary>
        /// <param name="buffer">Bytes to write into the streams.</param>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            //we implement this method because originally it copies the span to an array while we can just pass it.
            foreach (var stream in streams)
                stream.Write(buffer);
        }

        #endregion Public Methods
    }
}