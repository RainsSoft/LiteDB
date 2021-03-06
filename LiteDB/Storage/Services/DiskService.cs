﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace LiteDB
{
    internal class DiskService : IDisposable
    {
        private const int LOCK_POSITION = 0;

        private ConnectionString _connectionString;

        private BinaryReader _reader;
        private BinaryWriter _writer;

        public DiskService(ConnectionString connectionString) {
            _connectionString = connectionString;

            // Open file as ReadOnly - if we need use Write, re-open in Write Mode
            var stream = new FileStream(_connectionString.Filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BasePage.PAGE_SIZE);

            _reader = new BinaryReader(stream);
        }

        /// <summary>
        /// Create a empty database ready to be used using connectionString as parameters
        /// </summary>
        public static void CreateNewDatafile(ConnectionString connectionString) {
            using (var stream = File.Create(connectionString.Filename)) {
                using (var writer = new BinaryWriter(stream)) {
                    DiskService.WritePage(writer, new HeaderPage());
                }
            }
        }

        /// <summary>
        /// Create a new Page instance and read data from disk
        /// </summary>
        public T ReadPage<T>(uint pageID)
            where T : BasePage, new() {
            // create page instance and read from disk (read page header + content page)
            var page = new T();
            var stream = _reader.BaseStream;
            var posStart = (long)pageID * (long)BasePage.PAGE_SIZE;
            var posEnd = posStart + BasePage.PAGE_SIZE;

            TryExec(_connectionString.Timeout, () => {
                // position cursor
                if (stream.Position != posStart) {
                    stream.Seek(posStart, SeekOrigin.Begin);
                }

                // read page header
                page.ReadHeader(_reader);

                // if T is base and PageType has a defined type, convert page
                var isBase = page.GetType() == typeof(BasePage);

                if (isBase) {
                    if (page.PageType == PageType.Index) page = (T)(object)page.CopyTo<IndexPage>();
                    else if (page.PageType == PageType.Data) page = (T)(object)page.CopyTo<DataPage>();
                    else if (page.PageType == PageType.Extend) page = (T)(object)page.CopyTo<ExtendPage>();
                    else if (page.PageType == PageType.Collection) page = (T)(object)page.CopyTo<CollectionPage>();
                }

                // read page content if page is not empty
                if (page.PageType != PageType.Empty) {
                    page.ReadContent(_reader);
                }

                // position cursor at starts next page
                _reader.ReadBytes((int)(posEnd - stream.Position));

            });

            return page;
        }

        /// <summary>
        /// Write a page from memory to disk 
        /// </summary>
        public void WritePage(BasePage page) {
            DiskService.WritePage(GetWriter(), page);
        }

        /// <summary>
        /// Static method for write a page using a diferent writer - used when create empty datafile
        /// </summary>
        public static void WritePage(BinaryWriter writer, BasePage page) {
            var stream = writer.BaseStream;
            var posStart = (long)page.PageID * (long)BasePage.PAGE_SIZE;
            var posEnd = posStart + BasePage.PAGE_SIZE;

            // position cursor
            if (stream.Position != posStart) {
                stream.Seek(posStart, SeekOrigin.Begin);
            }

            // write page header
            page.WriteHeader(writer);

            // write content except for empty pages
            if (page.PageType != PageType.Empty) {
                page.WriteContent(writer);
            }

            // write with zero non-used page
            writer.Write(new byte[posEnd - stream.Position]);

            // if page is dirty, clean up
            page.IsDirty = false;
        }

        /// <summary>
        /// Pre-allocate more disk space to fast write new pages on disk
        /// </summary>
        public void AllocateDiskSpace(long length) {
            var writer = this.GetWriter();
            var stream = writer.BaseStream as FileStream;

            if (length > stream.Length) {
                stream.SetLength(length);
            }
        }

        /// <summary>
        /// Get BinaryWriter
        /// </summary>
        private BinaryWriter GetWriter() {
            // If no writer - re-open file in Write Mode
            if (_writer == null) {
                _reader.Close(); // Close reader

                var stream = new FileStream(_connectionString.Filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, BasePage.PAGE_SIZE);

                _reader = new BinaryReader(stream);
                _writer = new BinaryWriter(stream);
            }

            return _writer;
        }

        #region Lock/Unlock functions

        /// <summary>
        /// Lock the datafile when start a begin transaction
        /// </summary>
        public void Lock() {
            var stream = this.GetWriter().BaseStream as FileStream;

            TryExec(_connectionString.Timeout, () => {
                // try to lock - if is in use, a exception will be throwed
#if !UNITY_WEBPLAYER
                stream.Lock(LOCK_POSITION, 1);
#else
                //Todo: how to do lock
                //in unity3d webplay this lib is not thread safe
#endif

            });
        }

        /// <summary>
        /// Unlock the datafile
        /// </summary>
        public void UnLock() {
            var stream = this.GetWriter().BaseStream as FileStream;
#if !UNITY_WEBPLAYER
            stream.Unlock(LOCK_POSITION, 1);
#else
            //ToDo: how to do unlock
#endif
        }

        public void Flush() {
            this.GetWriter().BaseStream.Flush();
        }

        /// <summary>
        /// Lock all file during write operations - avoid reads during inconsistence data
        /// </summary>
        public void ProtectWriteFile(Action action) {
            var stream = this.GetWriter().BaseStream as FileStream;
            var fileLength = stream.Length;
#if !UNITY_WEBPLAYER
            stream.Lock(LOCK_POSITION + 1, fileLength);
            action();
            stream.Unlock(LOCK_POSITION + 1, fileLength);
#else
            //ToDo: how to do lock
            action();
            //ToDo: how to do unlock
#endif
        }

        /// <summary>
        /// Try execute a block of code until timeout when IO lock exception occurs OR access denind
        /// </summary>
        public static void TryExec(TimeSpan timeout, Action action) {
            var timer = DateTime.Now.Add(timeout);

            while (DateTime.Now < timer) {
                try {
                    action();
                    return;
                }
                catch (UnauthorizedAccessException) {
                    Thread.Sleep(250);
                }
                catch (IOException ex) {
                    ex.WaitIfLocked(250);
                }
            }

            throw LiteException.LockTimeout(timeout);
        }

        #endregion

        public void Dispose() {
            _reader.Close();

            if (_writer != null) {
                _writer.Close();
            }
        }
    }
}
