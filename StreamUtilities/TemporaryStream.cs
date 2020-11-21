using System;
using System.Collections.Generic;
using System.IO;

#pragma warning disable CS8601
#pragma warning disable CS8602

namespace StreamUtilities
{
    /// <summary>
    /// A temporary stream acts like a MemoryStream, but it will auto-remove the oldest data.
    /// </summary>
    public class TemporaryStream : Stream
    {
        #region Private Fields

        private LinkedListNode<Block> activeBlock;
        private int activeCursor;
        private LinkedList<Block> blocks;
        private bool closed;

        private int numberOfBlocks;
        private long position;
        private long size;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Constructor. The max size of the stream is equal to : <paramref name="maxBlockCount"/> *
        /// <paramref name="blockSize"/> bytes.
        /// </summary>
        /// <param name="maxBlockCount">The number of blocks that can handle this stream at most.</param>
        /// <param name="blockSize">The size of one block in bytes. 1024 by default.</param>
        public TemporaryStream(int maxBlockCount, int blockSize = 1024)
        {
            if (maxBlockCount <= 0)
                throw new ArgumentException("maxBlockCount can not be less than 1");
            MaxBlockCount = maxBlockCount;
            BlockSize = blockSize;
            blocks = new LinkedList<Block>();
            activeBlock = blocks.AddLast(new Block(BlockSize));
            activeCursor = 0;
            size = 0;
            position = 0;
            numberOfBlocks = 1;
            closed = false;
        }

        #endregion Public Constructors

        #region Public Properties

        private void CheckClosed()
        {
            if (closed)
                throw new ObjectDisposedException("The stream is closed", innerException: null);
        }

        /// <summary>
        /// The size of one block in bytes.
        /// </summary>
        public int BlockSize { get; }

        /// <summary>
        /// Always true.
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Always true.
        /// </summary>
        public override bool CanSeek => true;

        /// <summary>
        /// Always true.
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// The current length of the stream.
        /// </summary>
        public override long Length => size;

        /// <summary>
        /// The max number of blocks in the stream.
        /// </summary>
        public int MaxBlockCount { get; }

        /// <summary>
        /// Get or set the current position within the stream from which to read/write from.
        /// </summary>
        public override long Position { get => position; set => Seek(value, SeekOrigin.Begin); }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Deletes every blocks in the stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The stream is closed or disposed.</exception>
        public void Clear()
        {
            CheckClosed();
            blocks.Clear();
            activeBlock = blocks.AddLast(new Block(BlockSize));
            activeCursor = 0;
            size = 0;
            position = 0;
            numberOfBlocks = 1;
        }

        /// <summary>
        /// Closes the stream, deleting all data saved inside.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The stream is closed or disposed.</exception>
        public override void Close()
        {
            base.Close();
            CheckClosed();
            blocks.Clear();
            closed = true;
        }

        /// <summary>
        /// Does nothing for this stream.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Reads data from the stream.
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
            CheckClosed();
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length - offset < count)
                throw new ArgumentException("The 'buffer' length is too short.");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (position == size)
                return 0;
            int readBytes = 0;
            while (readBytes < count)
            {
                buffer[readBytes + offset] = activeBlock!.Value.Bytes[activeCursor];
                readBytes++;
                activeCursor++;
                position++;
                if (activeCursor == activeBlock.Value.Used)
                    if (position != size)
                    {
                        activeBlock = activeBlock.Next;
                        activeCursor = 0;
                    }
                    else
                        break;
            }
            return readBytes;
        }

        /// <summary>
        /// Move the position in the stream.
        /// </summary>
        /// <param name="offset">The new position in the stream based on <paramref name="origin"/>.</param>
        /// <param name="origin">The origin from which to start moving the position in the stream.</param>
        /// <returns>The new position in the stream.</returns>
        /// <exception cref="ObjectDisposedException">The stream is closed or disposed.</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckClosed();
            if (origin == SeekOrigin.Begin)
            {
                position = 0;
                activeCursor = 0;
                activeBlock = blocks.First;
                if (offset < 0)
                    return position;
            }
            else if (origin == SeekOrigin.End)
            {
                position = size;
                activeBlock = blocks.Last;
                activeCursor = activeBlock.Value.Used;
                if (offset > 0)
                    return position;
            }
            if (offset > 0)
                for (int i = 0; i < offset && position < size;)
                {
                    activeCursor++;
                    i++;
                    position++;
                    if (activeCursor == activeBlock.Value.Used)
                    {
                        if (activeBlock.Next != null)
                            activeBlock = activeBlock.Next;
                        else
                            break;
                        activeCursor = 0;
                    }
                }
            else
                for (int i = 0; i > offset && position > 0;)
                {
                    if (activeCursor == 0)
                    {
                        if (activeBlock.Previous != null)
                            activeBlock = activeBlock.Previous;
                        else
                            break;
                        activeCursor = activeBlock.Value.Used;
                    }
                    activeCursor--;
                    i--;
                    position--;
                }
            return position;
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="NotSupportedException">This method is not supported.</exception>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>
        /// Writes new data in the stream.
        /// </summary>
        /// <param name="buffer">Bytes to write into the stream.</param>
        /// <param name="offset">Index in <paramref name="buffer"/> to start from.</param>
        /// <param name="count">Number of bytes to write.</param>
        /// <exception cref="ObjectDisposedException">The stream is closed or disposed.</exception>
        /// <exception cref="ArgumentNullException">'buffer' can not be null.</exception>
        /// <exception cref="ArgumentException">The buffer's length is too short.</exception>
        /// <exception cref="ArgumentOutOfRangeException">'offset' or 'count' can not be negative.</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckClosed();
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset + count > buffer.Length)
                throw new ArgumentException("'buffer' is too short");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            for (int i = 0; i < count; ++i)
            {
                activeBlock.Value.Bytes[activeCursor] = buffer[i + offset];
                position++;
                activeCursor++;
                if (activeBlock.Value.Used < activeCursor)
                {
                    activeBlock.Value.Used = activeCursor;
                    size++;
                }
                if (activeCursor == activeBlock.Value.Bytes.Length)
                {
                    if (activeBlock.Next == null)
                    {
                        activeBlock = blocks.AddLast(new Block(BlockSize));
                        numberOfBlocks++;
                        if (numberOfBlocks > MaxBlockCount)
                        {
                            var bytesRemoved = blocks.First.Value.Used;
                            position -= bytesRemoved;
                            size -= bytesRemoved;
                            blocks.RemoveFirst();
                        }
                    }
                    else
                        activeBlock = activeBlock.Next;

                    activeCursor = 0;
                }
            }
        }

        /// <summary>
        /// Writes new data into the stream.
        /// </summary>
        /// <param name="buffer">Bytes to write into the stream.</param>
        /// <exception cref="ObjectDisposedException">The stream is closed or disposed.</exception>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            //we override this method because the base version make a copy of the span while we can simply read from it.
            CheckClosed();
            for (int i = 0; i < buffer.Length; ++i)
            {
                activeBlock.Value.Bytes[activeCursor] = buffer[i];
                position++;
                activeCursor++;
                if (activeBlock.Value.Used < activeCursor)
                {
                    activeBlock.Value.Used = activeCursor;
                    size++;
                }
                if (activeCursor == activeBlock.Value.Bytes.Length)
                {
                    if (activeBlock.Next == null)
                    {
                        activeBlock = blocks.AddLast(new Block(BlockSize));
                        numberOfBlocks++;
                        if (numberOfBlocks > MaxBlockCount)
                        {
                            var bytesRemoved = blocks.First.Value.Used;
                            position -= bytesRemoved;
                            size -= bytesRemoved;
                            blocks.RemoveFirst();
                        }
                    }
                    else
                        activeBlock = activeBlock.Next;

                    activeCursor = 0;
                }
            }
        }

        #endregion Public Methods

        #region Private Classes

        private class Block
        {
            #region Public Constructors

            public Block(int size)
            {
                Bytes = new byte[size];
                Used = 0;
            }

            #endregion Public Constructors

            #region Public Properties

            public byte[] Bytes { get; set; }

            public int Used { get; set; }

            #endregion Public Properties
        }

        #endregion Private Classes
    }
}