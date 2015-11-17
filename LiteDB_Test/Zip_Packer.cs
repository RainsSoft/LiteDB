    public class ZipFilePacker : IFilePacker {
        internal static readonly string LISTFILE = "_internal_list.lst";
        ZipFile m_Zip;

        Dictionary<string, bool> m_FileTableNames = new Dictionary<string, bool>();
        #region IFilePacker 成员

        public bool Open(string strFileName, string arg, bool createIfNotExists) {
            bool exists = File.Exists(strFileName);
            if (exists == false && !createIfNotExists) {
                return false;
            }

            try {
                if (exists == false && createIfNotExists) {
                    m_Zip = new ZipFile(strFileName, Encoding.UTF8);
                    if (string.IsNullOrEmpty(arg) == false) {
                        m_Zip.Password = arg;
                    }
                }
                else {
                    if (ZipFile.IsZipFile(strFileName) == false) {
                        return false;
                    }
                    m_Zip = ZipFile.Read(strFileName);
                    if (string.IsNullOrEmpty(arg) == false) {
                        m_Zip.Password = arg;
                    }
                    ZipEntry entry = m_Zip[LISTFILE];
                    if (entry == null) {
                        throw new AccessPackerException("未能找到列表信息，文件已经损坏！", null);
                    }
                    using (MemoryStream ms = new MemoryStream()) {
                        entry.Extract(ms);
                        ms.Position = 0;
                        using (StreamReader sr = new StreamReader(ms, Encoding.UTF8)) {
                            string s=sr.ReadLine();
                            if(string.IsNullOrEmpty(s)==false){
                                m_FileTableNames.Add(s.ToLower(), false);
                            }
                        }
                    }
                }
                m_Zip.SortEntriesBeforeSaving = true;
                return true;
            }
            catch (Exception ee) {
                if (m_Zip != null) {
                    m_Zip.Dispose();
                }
                throw new AccessPackerException(ee.Message, ee);
            }
        }

        public unsafe bool Open(byte* dataBuf, string arg) {
            throw new NotImplementedException();
        }

        public bool IsClosed {
            get {
                return m_Zip == null;
            }
        }

        public void Close() {
            if (m_Zip != null) {
                //需要保存一下吗？
                //m_Zip.Save(); 不保存，由EndUpdate来搞定
                m_Zip.Dispose();
            }
            m_Zip = null;
        }

        public IFilePackerStrategy AddFileTable(string tableName) {
            if (string.IsNullOrEmpty(tableName)) {
                throw new ArgumentNullException(tableName);
            }
            if (IsTableExists(tableName)) {
                throw new AccessPackerException(tableName + "已经存在", null);
            }
            ZipEntry entry = m_Zip.AddDirectoryByName(tableName);
            if (Directory.Exists(tableName)) {
                m_Zip.AddDirectory(tableName, tableName);
            }
            m_FileTableNames.Add(tableName.ToLower(), false);
            IFilePackerStrategy ret = new ZipFileTable(m_Zip);
            ret.Name = tableName;

            //保存FileTable列表
            Save();
            return ret;
        }

        /// <summary>
        /// 获取指定名称的表。如果tableName为空，则视为根目录。
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public IFilePackerStrategy GetFileTable(string tableName) {
            if (string.IsNullOrEmpty(tableName)) {
                //throw new ArgumentNullException(tableName);
                IFilePackerStrategy ret = new ZipFileTable(m_Zip);
                ret.Name = tableName;
                return ret;
            }
            else if (m_Zip.ContainsEntry(tableName + "/")) {
                IFilePackerStrategy ret = new ZipFileTable(m_Zip);
                ret.Name = tableName;
                return ret;
            }
            else {
                return null;
            }
        }

        public void DelFileTable(string tableName) {
            if (string.IsNullOrEmpty(tableName)) {
                List<string> s = new List<string>();
                foreach (var v in m_Zip) {
                    if (v.FileName.Contains("/") == false && v.FileName != LISTFILE) {
                        s.Add(v.FileName);
                    }
                }

                m_Zip.RemoveEntries(s);

            }
            if (m_Zip.ContainsEntry(tableName + "/")) {
                m_Zip.RemoveEntry(tableName + "/");
            }
            m_FileTableNames.Remove(tableName.ToLower());
        }

        public void RenameFileTable(string tableName, string newName) {
            //m_Zip[tableName+"/"].FileName = newName;
            string oldName = tableName + "/";
            string newname1 = newName + "/";
            List<string> files = new List<string>();
            foreach (var v in m_Zip) {
                if (v.FileName.StartsWith(oldName, StringComparison.OrdinalIgnoreCase)) {
                    files.Add(v.FileName);

                }
            }
            foreach (var v in files) {
                string s = m_Zip[v].FileName;
                m_Zip[v].FileName = s.Replace(oldName, newname1);
            }

            m_FileTableNames.Remove(tableName.ToLower());
            m_FileTableNames.Add(newName.ToLower(), false);
        }

        public int GetFileTableList(out List<string> ret) {
            ret = new List<string>();
            int i = 0;
            foreach (var v in m_FileTableNames.Keys) {
                ret.Add(v);
                i++;
            }
            return i;
        }

        public bool IsTableExists(string name) {
            return m_FileTableNames.ContainsKey(name.ToLower());
        }

        public void BeginUpdate(IFilePackerStrategy file) {

        }

        public void EndUpdate(IFilePackerStrategy file, bool success) {
            if (success) {
                Save();
            }
            else {

            }
        }

        private void Save() {
            //保存FileTable列表
            if (m_FileTableNames.Count > 0) {
                StringBuilder sb = new StringBuilder();
                foreach (var v in m_FileTableNames.Keys) {
                    sb.Append(v);
                    sb.Append("\n");
                }
                sb.Remove(sb.Length - 1, 1);
                m_Zip.UpdateEntry(LISTFILE, sb.ToString());
            }
            m_Zip.Save();
        }
        #endregion
    }

    public class ZipFileTable : IFilePackerStrategy {

        private ZipFile m_Zip;
        internal ZipFileTable(ZipFile zip) {
            m_Zip = zip;
        }
        #region IFilePackerStrategy 成员
        /// <summary>
        /// 不支持
        /// </summary>
        /// <param name="strFileName"></param>
        public void AddFile(string strFileName) {
            throw new NotSupportedException();
            //if (FileExists(strFileName)) {
                UpdateFile(strFileName);
            //}
            //else {
            //    m_Zip.AddFile(strFileName, this.Name);
            //}
        }

        public void AddFile(string strFileName, System.IO.Stream stream) {
            //string dir = string.IsNullOrEmpty(this.Name) ? "" : this.Name + "\\";
            //if (FileExists(strFileName)) {
                UpdateFile(strFileName, stream);
            //}
            //else {
            //    m_Zip.AddEntry(dir + strFileName, stream);
            //}
        }

        public void AddFile(string strFileName, byte[] fileData) {
            //string dir = string.IsNullOrEmpty(this.Name) ? "" : this.Name + "\\";
           // if (FileExists(strFileName)) {
                UpdateFile(strFileName, fileData);
            //}
            //else {
            //    m_Zip.AddEntry(dir + strFileName, fileData);
            //}
        }

        public void RenameFile(string strFileName, string strNewFile) {
            string dir = string.IsNullOrEmpty(this.Name) ? "" : this.Name + "\\";
            if (FileExists(strFileName)) {
                m_Zip[dir + strFileName].FileName = dir + strNewFile;
            }

        }

        public void RenameDir(string strDirName, string strNewDirName) {
            string s = strDirName.Replace('\\', '/');
            string snew = strNewDirName.Replace('\\', '/');
            List<string> files = new List<string>();
            foreach (var v in m_Zip) {
                if (v.FileName.StartsWith(s, StringComparison.OrdinalIgnoreCase)) {
                    files.Add(v.FileName);
                }
            }

            foreach (var f in files) {
                string fn = m_Zip[f].FileName;
                m_Zip[f].FileName = fn.Replace(s, snew);
            }
        }

        public void UpdateFile(string strFileName) {
            throw new NotSupportedException();
            m_Zip.UpdateFile(strFileName, this.Name);
        }

        /// <summary>
        /// stream由系统负责关闭
        /// </summary>
        /// <param name="strFileName"></param>
        /// <param name="stream"></param>
        public void UpdateFile(string strFileName, System.IO.Stream stream) {
            string dir = string.IsNullOrEmpty(this.Name) ? "" : this.Name + "\\";
            m_Zip.UpdateEntry(dir + strFileName, stream);
        }

        public void UpdateFile(string strFileName, byte[] fileData) {
            string dir = string.IsNullOrEmpty(this.Name) ? "" : this.Name + "\\";
            m_Zip.UpdateEntry(dir + strFileName, fileData);
        }

        public void DelDir(string strDir) {
            string dir = string.IsNullOrEmpty(this.Name) ? "" : this.Name + "\\";
            m_Zip.RemoveEntry(dir + strDir);
        }

        public void DelFile(string strFileName) {
            string dir = string.IsNullOrEmpty(this.Name) ? "" : this.Name + "\\";
            m_Zip.RemoveEntry(dir + strFileName);
        }

        public bool FileExists(string strFileName) {
            string dir = string.IsNullOrEmpty(this.Name) ? "" : this.Name + "\\";
            bool ret = m_Zip[dir + strFileName] != null;
            return ret;
        }

        public int GetFiles(string strDir, out List<string> fileNames, out int totalSize) {
            fileNames = new List<string>();
            totalSize = 0;

            //如果this.name为空，则视为根目录
            if (string.IsNullOrEmpty(this.Name)) {
                foreach (var v in m_Zip) {
                    if (v.FileName.Contains("/") == false && v.FileName != ZipFilePacker.LISTFILE) {
                        fileNames.Add(v.FileName.Replace('/', '\\'));
                        totalSize += (int)v.UncompressedSize;
                    }
                }
            }
            else {

                string dirFullName = string.IsNullOrEmpty(strDir.Trim()) ? this.Name + "/" : this.Name + "/" + strDir.Replace('\\', '/');
                bool start = false;
                foreach (var v in m_Zip) {
                    if (v.FileName.StartsWith(dirFullName, StringComparison.OrdinalIgnoreCase)) {
                        start = true;
                        if (v.IsDirectory == false) {
                            fileNames.Add(v.FileName.Replace('/', '\\'));
                            totalSize += (int)v.UncompressedSize;
                        }
                    }
                    //else {
                    //    if (start == true) {
                    //        break; //后面不用找了。
                    //    }
                    //}
                }
            }

            return fileNames.Count;
        }

        public int GetDirs(out List<string> dirs) {
            dirs = new List<string>();
            bool start = false;
            string dir = string.IsNullOrEmpty(this.Name) ? "" : this.Name + "/";
            foreach (var v in m_Zip) {
                if (v.IsDirectory && v.FileName.StartsWith(dir, StringComparison.OrdinalIgnoreCase)) {
                    start = true;
                    dirs.Add(v.FileName.Replace('/', '\\').TrimEnd('\\'));
                }
                //else {
                //    if (start == true) {
                //        break; //后面不用找了。
                //    }
                //}
            }
            return dirs.Count;
        }

        public string Name {
            get;
            set;
        }

        public unsafe byte[] OpenFile(string strFileName) {
            string dir = string.IsNullOrEmpty(this.Name) ? "" : this.Name + "\\";

            ZipEntry z = m_Zip[dir + strFileName];
            if (z != null) {
                long size = z.UncompressedSize;
                byte[] ret = new byte[size];
                fixed (byte* p = ret) {
                    using (UnmanagedMemoryStream ms = new UnmanagedMemoryStream(p, size, size, FileAccess.Write)) {
                        z.Extract(ms);
                    }
                }
                return ret;
            }

            return null;
        }
        /// <summary>
        /// 由外部负责关闭stream
        /// </summary>
        /// <param name="strFileName"></param>
        /// <returns></returns>
        public System.IO.Stream OpenFileAsStream(string strFileName) {
            string dir = string.IsNullOrEmpty(this.Name) ? "" : this.Name + "\\";

            ZipEntry z = m_Zip[dir + strFileName];
            if (z != null) {
                MemoryStream ms = new MemoryStream();
                z.Extract(ms);
                ms.Position = 0;
                return ms;
            }
            return null;
        }

        public string OpenFileAsString(string strFileName) {
            string dir = string.IsNullOrEmpty(this.Name) ? "" : this.Name + "\\";

            ZipEntry z = m_Zip[dir + strFileName];
            if (z != null) {

                using (MemoryStream ms = new MemoryStream()) {
                    z.Extract(ms);
                    ms.Position = 0;
                    using (StreamReader sr = new StreamReader(ms, Encoding.UTF8)) {
                        return sr.ReadToEnd();
                    }
                }
            }

            return string.Empty;
        }

        public void Clean() {

        }

        #endregion
    }
    