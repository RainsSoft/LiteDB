  internal class FileSystemPacker : IFilePacker
    {

        #region IFilePacker 成员

        public IFilePackerStrategy AddFileTable(string name) {
            throw new NotImplementedException();
        }

        public IFilePackerStrategy GetFileTable(string name) {
            throw new NotImplementedException();
        }

        public void DelFileTable(string name) {
            throw new NotImplementedException();
        }

        public void RenameFileTable(string tableName, string newName) {
            throw new NotImplementedException();
        }

        public int GetFileTableList(out List<string> ret) {
            throw new NotImplementedException();
        }

        public bool IsTableExists(string name) {
            throw new NotImplementedException();
        }

        public void BeginUpdate(IFilePackerStrategy file) {

        }

        public void BeginUpdate(string name) {

        }

        public void EndUpdate(IFilePackerStrategy file, bool success) {

        }

        public void EndUpdate(string name, bool success) {

        }

        public void Close() {

        }

        #endregion
    }
    internal class FilesSystemPackerStrategy : IFilePackerStrategy
    {

        internal string m_RootDir;
        private IFilePacker m_Packer = new FileSystemPacker();
        public IFilePacker Packer {
            get {
                return m_Packer;
            }
        }
        #region IFilePackerStrategy 成员

        public void AddFile(string strFileName, byte[] fileData) {
            UpdateFile(strFileName, fileData);
        }

        public void AddFile(string strFileName, System.IO.Stream stream) {
            UpdateFile(strFileName, stream);
        }

        public void AddFile(string strFileName) {

        }

        public void DelDir(string strDir) {
            string dir = m_RootDir + "\\" + strDir;
            List<string> files;
            int size;
            int fc = GetFiles(strDir, out files, out size);
            foreach (var v in files) {
                File.Delete(m_RootDir + "\\" + v);
            }
            if (Directory.Exists(dir)) {
                Directory.Delete(dir, true);
            }
        }

        public void DelFile(string strFileName) {
            //Directory.Delete(Path.GetDirectoryName(m_RootDir + "\\" + strFileName));
            string fn = m_RootDir + "\\" + strFileName;
            if (File.Exists(fn)) {
                File.Delete(fn);
            }
        }
        //
        /// <summary>
        /// 调用该方法时,需要传入带路径的文件名
        /// </summary>
        /// <param name="strFileName">具体文件名</param>
        /// <returns></returns>
        public bool FileExists(string strFileName) {

            //有什么问题吗?
            string fn = m_RootDir + "\\" + strFileName;
            if (!File.Exists(strFileName)) {
                return File.Exists(fn);
            }
            return true;

        }

        public int GetDirs(out List<string> dirs) {

            List<string> sunDirs = new List<string>(Directory.GetDirectories(this.m_RootDir, "*", SearchOption.TopDirectoryOnly));
            dirs = new List<string>();
            foreach (var v in sunDirs) {
                if (v.IndexOf(".svn", StringComparison.OrdinalIgnoreCase) >= 0) {
                    continue;
                }
                //与数据库 同步
                string str = "";
                for (int i = v.Length - 1; i >= 0; i--) {
                    if (v[i] == '/' || v[i] == '\\')
                        break;
                    str = v[i] + str;
                }

                dirs.Add(str);
            }

            return dirs.Count;
        }

        public int GetFiles(string strDir, out List<string> fileNames, out int totalSize) {
            fileNames = new List<string>();
            totalSize = 0;
            string dir = m_RootDir + "\\" + strDir;
            if (Directory.Exists(dir) == false) {
                //fileNames = null;
                totalSize = 0;
                return 0;
            }
            string[] ret = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
            foreach (string s in ret) {
                FileInfo fi = new FileInfo(s);
                totalSize += (int)fi.Length;
            }
            foreach (string s in ret) {
                if (s.IndexOf(".svn", StringComparison.OrdinalIgnoreCase) >= 0) {
                    continue;
                }
                //fileNames.Add(s.Substring(dir.Length + 1, s.Length - dir.Length - 1));
                fileNames.Add(s.Substring(m_RootDir.Length + 1, s.Length - m_RootDir.Length - 1));
            }
            return ret.Length;
        }

        public string Name {
            get {
                return m_RootDir;
            }
            set {
                m_RootDir = value; ;
            }
        }

        public byte[] OpenFile(string strFileName) {
            byte[] buf;
            string s = strFileName.ToLower();
            if (s.StartsWith(m_RootDir.ToLower())) {
                s = s.Replace(m_RootDir.ToLower(), "");
            }
            else {
                s = strFileName;
            }
            if (s.StartsWith("\\")) {
                s = s.Remove(0, 1);
            }
            s = Path.Combine(m_RootDir, s);
            //if (!File.Exists(s)) return null;
            using (FileStream fs = new FileStream(s, FileMode.Open, FileAccess.Read)) {
                ////检测UTF8的BOM
                //int bom1 = fs.ReadByte();
                //int bom2 = fs.ReadByte();
                //int bom3 = fs.ReadByte();
                //if (bom1 == 0xEF && bom2 == 0xBB && bom3 == 0xBF) {
                //    buf = new byte[fs.Length - 3];
                //}
                //else {
                //    fs.Position = 0;
                //    buf = new byte[fs.Length];
                //}
                buf = new byte[fs.Length];
                fs.Read(buf, 0, buf.Length);

                return buf;
            }
        }

        public string OpenFileAsString(string strFileName) {
            using (StreamReader sr = new StreamReader(m_RootDir + "\\" + strFileName, GlobalConfig.TxtEncoding)) {
                return sr.ReadToEnd();
            }
        }

        public void UpdateFile(string strFileName, byte[] fileData) {

            string dir = Path.GetDirectoryName(m_RootDir + "\\" + strFileName);
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
            using (FileStream fs = new FileStream(m_RootDir + "\\" + strFileName, FileMode.Create, FileAccess.Write)) {
                fs.Write(fileData, 0, fileData.Length);
            }
        }
        //
        //如果不存在这创建
        //
        public void UpdateFile(string strFileName, System.IO.Stream stream) {

            string dir = Path.GetDirectoryName(m_RootDir + "\\" + strFileName);
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
            using (FileStream fs = new FileStream(m_RootDir + "\\" + strFileName, FileMode.Create, FileAccess.Write)) {
                byte[] b = new byte[stream.Length];
                stream.Read(b, 0, b.Length);
                fs.Write(b, 0, b.Length);
            }

        }

        public void UpdateFile(string strFileName) {
            //UpdateFile(strFileName,null);
        }

        #endregion

        #region IFilePackerStrategy 成员


        public void RenameDir(string strDirName, string strNewDirName) {
            string OldDir = m_RootDir + "\\" + strDirName;
            string NewDir = m_RootDir + "\\" + strNewDirName;
            Directory.Move(OldDir, NewDir);
        }

        public void RenameFile(string strFileName, string strNewFile) {
            //throw new NotImplementedException();
        }

        #endregion

        #region IFilePackerStrategy 成员


        public Stream OpenFileAsStream(string strFileName) {
            byte[] buf;
            string s = strFileName.ToLower();
            if (s.StartsWith(m_RootDir.ToLower())) {
                s = s.Replace(m_RootDir.ToLower(), "");
            }
            else {
                s = strFileName;
            }
            if (s.StartsWith("\\")) {
                s = s.Remove(0, 1);
            }
            s = Path.Combine(m_RootDir, s);
            FileStream fs = new FileStream(s, FileMode.Open, FileAccess.Read);
            return fs;
        }

        #endregion

        #region IFilePackerStrategy 成员


        public void Clean() {
            //DebugLog.Log("未实现该方法");
        }

        #endregion

        #region IFilePackerStrategy 成员


        public void AddFile(string strFileName, DateTime date) {
            AddFile(strFileName);
            FileInfo fi = null;
            if (File.Exists(strFileName)) {
                fi = new FileInfo(strFileName);
            }
            else {
                fi = new FileInfo(m_RootDir + "\\" + strFileName);
            }
            fi.LastWriteTime = date;
        }

        public void AddFile(string strFileName, Stream stream, DateTime date) {
            AddFile(strFileName, stream);
            FileInfo fi = null;
            if (File.Exists(strFileName)) {
                fi = new FileInfo(strFileName);
            }
            else {
                fi = new FileInfo(m_RootDir + "\\" + strFileName);
            }
            fi.LastWriteTime = date;
        }

        public void AddFile(string strFileName, byte[] fileData, DateTime date) {
            AddFile(strFileName, fileData);
            FileInfo fi = null;
            if (File.Exists(strFileName)) {
                fi = new FileInfo(strFileName);
            }
            else {
                fi = new FileInfo(m_RootDir + "\\" + strFileName);
            }
            fi.LastWriteTime = date;
        }

        public void UpdateFile(string strFileName, DateTime date) {
            UpdateFile(strFileName);
            FileInfo fi = null;
            if (File.Exists(strFileName)) {
                fi = new FileInfo(strFileName);
            }
            else {
                fi = new FileInfo(m_RootDir + "\\" + strFileName);
            }
            fi.LastWriteTime = date;
        }

        public void UpdateFile(string strFileName, Stream stream, DateTime date) {
            UpdateFile(strFileName, stream);
            FileInfo fi = null;
            if (File.Exists(strFileName)) {
                fi = new FileInfo(strFileName);
            }
            else {
                fi = new FileInfo(m_RootDir + "\\" + strFileName);
            }
            fi.LastWriteTime = date;
        }

        public void UpdateFile(string strFileName, byte[] fileData, DateTime date) {
            UpdateFile(strFileName, fileData);
            FileInfo fi = null;
            if (File.Exists(strFileName)) {
                fi = new FileInfo(strFileName);
            }
            else {
                fi = new FileInfo(m_RootDir + "\\" + strFileName);
            }
            fi.LastWriteTime = date;
        }

        public DateTime GetUpdateDate(string strFileName) {
            FileInfo fi = null;
            if (File.Exists(strFileName)) {
                fi = new FileInfo(strFileName);
            }
            else {
                fi = new FileInfo(m_RootDir + "\\" + strFileName);
            }
            return fi.LastWriteTime;
        }

        #endregion
    }