using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Interface for an encryption algorithm
    /// </summary>
    public abstract class LiteDEncryption
    {

        public void SetKey(string str) {
            var bytes = System.Text.Encoding.ASCII.GetBytes(str);
            SetKey(bytes, 0, bytes.Length);
        }

        public abstract void SetKey(byte[] data, int offset, int count);

        /// <summary>
        /// Encrypt an outgoing message in place
        /// </summary>
        public abstract bool Encrypt(byte[] buffer);
        public abstract bool Encrypt(byte[] fileStream, int streamOffset);
        public abstract bool Encrypt(byte[] fileStream, int streamOffset, int count);
        /// <summary>
        /// Decrypt an incoming message in place
        /// </summary>
        public abstract bool Decrypt(byte[] buffer);
        public abstract bool Decrypt(byte[] fileStream, int streamOffset);
        public abstract bool Decrypt(byte[] fileStream, int streamOffset, int count);
    }
    public class LiteXorEncryption : LiteDEncryption
    {
        private byte[] m_key;

        /// <summary>
        /// NetXorEncryption constructor
        /// </summary>
        public LiteXorEncryption(byte[] key)
            : base() {
            m_key = key;
        }

        public override void SetKey(byte[] data, int offset, int count) {
            m_key = new byte[count];
            Array.Copy(data, offset, m_key, 0, count);
        }

        /// <summary>
        /// NetXorEncryption constructor
        /// </summary>
        public LiteXorEncryption(string key)
            : base() {
            m_key = Encoding.UTF8.GetBytes(key);
        }

        /// <summary>
        /// Encrypt an outgoing message
        /// </summary>
        public override bool Encrypt(byte[] msg) {
            int numBytes = msg.Length;
            for (int i = 0; i < numBytes; i++) {
                int offset = i % m_key.Length;
                msg[i] = (byte)(msg[i] ^ m_key[offset]);
            }
            return true;
        }

        /// <summary>
        /// Decrypt an incoming message
        /// </summary>
        public override bool Decrypt(byte[] msg) {
            int numBytes = msg.Length;
            for (int i = 0; i < numBytes; i++) {
                int offset = i % m_key.Length;
                msg[i] = (byte)(msg[i] ^ m_key[offset]);
            }
            return true;
        }


        public override bool Encrypt(byte[] fileStream, int streamOffset) {
            int numBytes = fileStream.Length;
            return Encrypt(fileStream,streamOffset,numBytes);
        }
        /// <summary>
        /// 基于流的加密
        /// </summary>
        /// <param name="fileStream"></param>
        /// <param name="streamOffset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override bool Encrypt(byte[] fileStream, int streamOffset,int count) {
            int numBytes = count;//fileStream.Length;
            for (int i = 0; i < numBytes; i++) {
                int offset = (streamOffset + i) % m_key.Length;
                fileStream[i] = (byte)(fileStream[i] ^ m_key[offset]);
            }
            return true;
        }

        public override bool Decrypt(byte[] fileStream, int streamOffset) {
            int numBytes = fileStream.Length;
            return Decrypt(fileStream,streamOffset,numBytes);
        }
        /// <summary>
        /// 基于流的解密
        /// </summary>
        /// <param name="fileStream">stream 的某段byte[]</param>
        /// <param name="streamOffset">stream.position</param>
        /// <param name="count"></param>
        public override bool Decrypt(byte[] fileStream, int streamOffset, int count) {
            int numBytes = count;//fileStream.Length; 
            for (int i = 0; i < numBytes; i++) {
                int offset = (streamOffset + i) % m_key.Length;
                fileStream[i] = (byte)(fileStream[i] ^ m_key[offset]);
            }
            return true;
        }
    }
    /// <summary>
    /// 加密流
    /// </summary>
    /// <summary>
    /// Represents an encoder stream for raw  streams.
    /// </summary>
    /// <remarks>
    /// Note that this class does NOT handle/support any of the  container streams/headers.
    /// </remarks>
    public sealed class LiteXorEncoderStream : Stream
    {
        #region Fields

        private readonly Stream stream;
        private LiteXorEncryption encoder;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the stream supports reading.
        /// Always returns false.
        /// </summary>
        public override bool CanRead {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the stream supports writing.
        /// Always returns true.
        /// </summary>
        public override bool CanWrite {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the stream supports seeking.
        /// Always returns false.
        /// </summary>
        public override bool CanSeek {
            get { return false; }
        }

        /// <summary>
        /// Gets or sets the position in the stream.
        /// Not supported.
        /// </summary>
        public override long Position {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets the length of the stream.
        /// Not supported.
        /// </summary>
        public override long Length {
            get { throw new NotSupportedException(); }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new EncoderStream.
        /// </summary>
        public LiteXorEncoderStream(Stream stream) {
            this.stream = stream;
            this.encoder = new LiteXorEncryption("litedb");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the encoder.
        /// </summary>
        /// <param name="properties">The encoder properties.</param>
        public void Initialize(LiteXorEncryption properties) {
            //if (this.encoder == null || !this.encoder.Properties.Compare(properties))
            //    this.encoder = new FastEncoder(this.stream, properties);
            //this.encoder.Initialize();
            encoder = properties;
            if (encoder == null) {
                encoder = new LiteXorEncryption("litedb");
            }
        }

        /// <summary>
        /// Reads data from the stream.
        /// Not supported.
        /// </summary>
        /// <param name="buffer">The buffer to store the data.</param>
        /// <param name="offset">The offset in the buffer at which data is written.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        public override int Read(byte[] buffer, int offset, int count) {
            if (this.encoder == null)
                throw new InvalidOperationException("Decoder is not initialized. Please initialize it using the \"Initialize\" method first.");

            int oldpos = (int)this.stream.Position;
            int numBytes = this.stream.Read(buffer, offset, count);
            if (numBytes == 0)
                return 0;
            this.encoder.Encrypt(buffer, oldpos, numBytes);
            //decodedBytes += numBytes;

            return (int)numBytes;
        }

        /// <summary>
        /// Writes data to the stream.
        /// Not supported.
        /// </summary>
        /// <param name="buffer">The buffer written to the stream.</param>
        /// <param name="offset">The offset in the buffer from where reading starts.</param>
        /// <param name="count">The number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotSupportedException();


            //if (this.encoder == null)
            //    throw new InvalidOperationException("Encoder is not initialized. Please initialize it using the \"Initialize\" method first.");
            //int posold = (int)this.stream.Position;//原始流长度有限制
            //int constBL = 1024 * 4;
            //int bufsize = (count > constBL) ? constBL : count;
            //byte[] buf = new byte[bufsize];
            ////拆开
            //int readc = count;
            //while (readc > 0) {
            //    int leaft = (readc > bufsize) ? bufsize : readc;
            //    Array.Copy(buffer, offset, buf, 0, leaft);
            //    encoder.Encrypt(buf, posold);//加密
            //    this.stream.Write(buf, 0, leaft);
            //    //
            //    offset += leaft;
            //    posold += leaft;
            //    readc -= bufsize;
            //}

        }

        /// <summary>
        /// Seeks in the stream.
        /// Not supported.
        /// </summary>
        /// <param name="offset">The seek offset.</param>
        /// <param name="origin">The seek origin.</param>
        /// <returns>The new position.</returns>
        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Flushes the stream.
        /// </summary>
        public override void Flush() {
            //this.encoder.Encode(true);
        }

        /// <summary>
        /// Closes the stream and the encoder.
        /// </summary>
        public override void Close() {
            //this.encoder.Close();
        }

        /// <summary>
        /// Sets the length of the stream. Not supported.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value) {
            throw new NotSupportedException();
        }

        #endregion
    }

    /// <summary>
    /// Represents a decoder stream for raw  streams.
    /// </summary>
    /// <remarks>
    /// Note that this class does NOT handle/support any of the  container streams/headers.
    /// </remarks>
    public sealed class LiteXorDecoderStream : Stream
    {
        #region Fields

        private readonly Stream stream;
        private LiteXorEncryption decoder;
        private long decodedBytes;
        private long decodedSize;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the stream supports reading.
        /// Always returns true.
        /// </summary>
        public override bool CanRead {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the stream supports writing.
        /// Always returns false.
        /// </summary>
        public override bool CanWrite {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the stream supports seeking.
        /// Always returns false.
        /// </summary>
        public override bool CanSeek {
            get { return false; }
        }

        /// <summary>
        /// Gets or sets the position in the stream.
        /// Not supported.
        /// </summary>
        public override long Position {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets the length of the stream.
        /// This returns the length of the decoded data.
        /// If the size of the decoded data is unknown, a negative value is returned.
        /// </summary>
        public override long Length {
            get { return this.stream.Length; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new DecoderStream.
        /// </summary>
        public LiteXorDecoderStream(Stream stream) {
            this.stream = stream;
            this.decoder = new LiteXorEncryption("litedb");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the decoder.
        /// </summary>
        /// <param name="properties">The decoder properties.</param>
        /// <remarks>
        /// Please note that this also reads data from the stream in order to initialize the range decoder.
        /// </remarks>
        public void Initialize(LiteXorEncryption properties) {
            //if (this.decoder == null || !this.decoder.Properties.Compare(properties))
            //    this.decoder = new Decoder(this.stream, properties);

            //this.decoder.Initialize();
            this.decoder = properties;
            if (this.decoder == null) {
                this.decoder = new LiteXorEncryption("litedb");
            }
            this.decodedSize = this.stream.Length;
        }

        /// <summary>
        /// Reads and decodes data from the stream.
        /// </summary>
        /// <param name="buffer">The buffer to store the decoded data.</param>
        /// <param name="offset">The offset in the buffer at which decoded data is written.</param>
        /// <param name="count">The number of bytes to decode.</param>
        /// <returns>The number of decoded bytes.</returns>
        public override int Read(byte[] buffer, int offset, int count) {
            throw new NotSupportedException();

            //if (this.decoder == null)
            //    throw new InvalidOperationException("Decoder is not initialized. Please initialize it using the \"Initialize\" method first.");

            //int oldpos = (int)this.stream.Position;
            //int numBytes = this.stream.Read(buffer, offset, count);
            //if (numBytes == 0)
            //    return 0;
            //this.decoder.Decrypt(buffer, oldpos);
            //decodedBytes += numBytes;

            //return (int)numBytes;
        }

        /// <summary>
        /// Writes data to the stream.
        /// Not supported.
        /// </summary>
        /// <param name="buffer">The buffer written to the stream.</param>
        /// <param name="offset">The offset in the buffer from where reading starts.</param>
        /// <param name="count">The number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count) {
            
            if (this.decoder == null)
                throw new InvalidOperationException("Encoder is not initialized. Please initialize it using the \"Initialize\" method first.");
            int posold = (int)this.stream.Position;//原始流长度有限制
            int constBL = 1024 * 4;
#if DEBUG
            constBL = 11;
#endif
            int bufsize = (count > constBL) ? constBL : count;
            byte[] buf = new byte[bufsize];
            //拆开
            int readc = count;
            while (readc > 0) {
                int leaft = (readc > bufsize) ? bufsize : readc;
                Array.Copy(buffer, offset, buf, 0, leaft);
                decoder.Decrypt(buf, posold,leaft);//加密
                this.stream.Write(buf, 0, leaft);
                //
                offset += leaft;
                posold += leaft;
                readc -= bufsize;
            }
        }

        /// <summary>
        /// Seeks in the stream.
        /// Not supported.
        /// </summary>
        /// <param name="offset">The seek offset.</param>
        /// <param name="origin">The seek origin.</param>
        /// <returns>The new position.</returns>
        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Flushes the stream.
        /// </summary>
        public override void Flush() {

        }

        /// <summary>
        /// Sets the length of the decoded data.
        /// Not supported.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value) {
            //this.decodedSize = value;
            throw new NotSupportedException();
        }

        #endregion
    }
}
