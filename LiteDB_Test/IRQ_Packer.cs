using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
//using System.Data;
//using IRobotQ.DBUtility;
using System.Diagnostics;
//using System.Data.SQLite;
//using SQLite;
using LiteDB;
//using SharpCompress;

namespace IRobotQ.Packer
{
    /// <summary>
    /// 表文件信息
    /// </summary>
    public class IRQ_FileTableInfo
    {
        #region get save id
        /// <summary>
        /// 存储数据ID
        /// </summary>
        /// <returns></returns>
        internal static string _get_DataId(string fileTableName) {
            return IRQ_Packer._getRealFileTableDataFullPath(fileTableName);
        }
        internal static string _get_Id(string fileTableName) {
            return IRQ_Packer._getRealFileTableFullPath(fileTableName);
        }
        /// <summary>
        /// 存储数据ID
        /// </summary>
        /// <returns></returns>
        public string _get_DataId() {
            return IRQ_Packer._getRealFileTableDataFullPath(this.FileTableName);
        }
        /// <summary>
        /// 直接对象ID
        /// </summary>
        /// <returns></returns>
        public string _get_Id() {
            return IRQ_Packer._getRealFileTableFullPath(this.FileTableName);
        }

        #endregion
        /// <summary>
        /// 数据表（目录名）(存储的时候需要规范化)
        /// </summary>
        public string FileTableName { get; set; }
        /// <summary>
        /// 文件个数
        /// </summary>
        public int FileCount { get; set; }
        public int TotalSize { get; set; }
        /// <summary>
        /// 文件列表,内容关联
        /// </summary>
        public List<IRQ_FileTableFileInfo> FileList { get; set; }

    }
    /// <summary>
    /// IRQ_FileTable 内文件信息
    /// </summary>
    public class IRQ_FileTableFileInfo
    {
        /// <summary>
        /// 对应存储的数据ID
        /// </summary>
        public string _GuidData { get; set; }

        /// <summary>
        /// 目录名
        /// </summary>
        public string FileDir { get; set; }
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 文件长度
        /// </summary>
        public int FileLen { get; set; }
        /// <summary>
        /// 文件修改时间
        /// </summary>
        public long FileUpdateTime { get; set; }
        ///// <summary>
        ///// 可能是关联的描述信息,JSON数据
        ///// </summary>
        //public string Tag { get; set; }
    }
    public class IRQ_Packer : IFilePacker
    {

        private LiteDatabase db;
        private string _dbPstr;
        //可以开头的字符! @ $ % -
        internal const string _filetable_DBPrefix = "$data/";//data前缀,文件存储前缀用
        internal const string _filetable_Prefix = "$/";//info前缀

        /// <summary>
        /// 取得内部使用的规范 dir name 不以/ \结尾
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static string _getFileLegalLowerDir(string path) {
            string pp = path.Replace("\\", "/");
            pp = pp.Replace("//", "/");
            pp = pp.EndsWith("/") ? pp.Remove(pp.Length-1) : pp ;
            return pp.ToLower();
        }
        /// <summary>
        /// 表信息ID
        /// </summary>
        /// <param name="tableinfoname"></param>
        /// <returns></returns>
        internal static string _getRealFileTableFullPath(string tableinfoname) {
            string vn = tableinfoname;
            if (!tableinfoname.ToLower().StartsWith(_filetable_Prefix))
                vn = _filetable_Prefix + (tableinfoname);
            return _getFileLegalLowerDir(vn);
        }
        /// <summary>
        /// 表存储数据ID
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns></returns>
        internal static string _getRealFileTableDataFullPath(string tablename) {
            string vn = tablename;
            if (!tablename.ToLower().StartsWith(_filetable_DBPrefix))
                vn = _filetable_DBPrefix + (tablename);
            return _getFileLegalLowerDir(vn);
        }


        public bool IsTableExists(string tableName2) {
            if (string.IsNullOrEmpty(tableName2)) {
                return false;
            }
            IRQ_FileTableInfo tinfo = new IRQ_FileTableInfo();
            tinfo.FileTableName = tableName2;
            bool ok = db.FileStorage.Exists(tinfo._get_Id());
            return ok;
        }
        /// <summary>
        /// 必须是 '/'结尾
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public IFilePackerStrategy AddFileTable(string tableName2) {
            if (string.IsNullOrEmpty(tableName2)) {
                throw new ArgumentNullException(tableName2);
            }
            if (IsTableExists(tableName2)) throw new Exception(tableName2 + "已经存在", null);
            //
            IRQ_FileTableInfo ft = new IRQ_FileTableInfo();
            ft.FileTableName = tableName2;
            ft.FileCount = 0;
            ft.TotalSize = 0;
            ft.FileList = new List<IRQ_FileTableFileInfo>();

            string strFileTableInfo = SimpleJsonEx.SimpleJson.SerializeObject(ft);

            //
            //保证是一个目录           
            string tableNameInfo = ft._get_Id();
            string tableNameData = ft._get_DataId();

            //创建空的压缩文件?存储目录结构？
            //SharpCompress.Archive.Zip.ZipArchive tablezip = SharpCompress.Archive.Zip.ZipArchive.Create();
            //tablezip.AddEntry("$/", new MemoryStream(), true);//固定目录          
            //SharpCompress.Common.CompressionInfo cin = new SharpCompress.Common.CompressionInfo();
            //cin.Type = SharpCompress.Common.CompressionType.Deflate;
            MemoryStream mszip = new MemoryStream();
            //tablezip.SaveTo(mszip, cin);
            //
            mszip.Position = 0;
            if (db.FileStorage.Exists(tableNameData)) {
                db.FileStorage.Delete(tableNameData);
            }
            db.FileStorage.Upload(new LiteFileInfo(tableNameData, _getFileLegalLowerDir(tableName2)), mszip);//增加0字节额外存储数据,可能用于存储目录结构数据
            mszip.Dispose();
            //tablezip.Dispose();
            //
            //保存成json格式
            byte[] bf = Encoding.UTF8.GetBytes(strFileTableInfo);
            using (MemoryStream ms = new MemoryStream(bf)) {
                if (db.FileStorage.Exists(tableNameInfo)) {
                    db.FileStorage.Delete(tableNameInfo);
                }
                db.FileStorage.Upload(new LiteFileInfo(tableNameInfo, _getFileLegalLowerDir(tableName2)), ms);//增加自己JSON数据文件
            }
            //新建目录对象
            IRQ_FileTable ret = new IRQ_FileTable(this, _dbPstr, db);
            ret.Name = tableName2;
            return ret;
        }

        public IFilePackerStrategy GetFileTable(string tableName2) {
            if (string.IsNullOrEmpty(tableName2)) throw new Exception("GetFileTable( tableName) the tableName is empty");

            if (!IsTableExists(tableName2)) return null;
            IFilePackerStrategy ret = new IRQ_FileTable(this, this._dbPstr, this.db);
            ret.Name = tableName2;
            return ret;
        }

        public void DelFileTable(string tableName2) {
            if (string.IsNullOrEmpty(tableName2)) throw new Exception("DelFileTable( tableName) the tableName is empty");

            IRQ_FileTable ret = this.GetFileTable(tableName2) as IRQ_FileTable;
            if (ret == null) return;
            IRQ_FileTableInfo tinfo = ret._getFileTableInfo();
            //删除文件以及自身
            for (int i = 0; i < tinfo.FileList.Count; i++) {
                db.FileStorage.Delete(tinfo.FileList[i]._GuidData);
            }
            db.FileStorage.Delete(tinfo._get_DataId());
            db.FileStorage.Delete(tinfo._get_Id());



        }

        public void RenameFileTable(string tableName2, string newName) {
            if (string.IsNullOrEmpty(tableName2)) {
                throw new ArgumentNullException(tableName2);
            }
            if (string.IsNullOrEmpty(newName)) {
                throw new ArgumentNullException(newName);
            }

            if (!IsTableExists(tableName2)) return;
            if (IsTableExists(newName)) return;

            string tableName = _getRealFileTableDataFullPath(tableName2);
            string tableNew = _getRealFileTableDataFullPath(newName);
            //对2个文件保存
            //if (!IsTableExists(tableName2) ) return;
            //doc["_id"] = this.Id;
            //   doc["filename"] = this.Filename;
            //   doc["mimeType"] = this.MimeType;
            //   doc["length"] = this.Length;
            //   doc["uploadDate"] = this.UploadDate;
            //   doc["metadata"] = this.Metadata ?? new BsonDocument();           
            MemoryStream lfd = new MemoryStream();
            db.FileStorage.Download(tableName, lfd);
            lfd.Position = 0L;
            if (db.FileStorage.Exists(tableNew)) {
                db.FileStorage.Delete(tableNew);
            }
            db.FileStorage.Upload(new LiteFileInfo(tableNew, newName), lfd);
            lfd.Dispose();

            //
            string tableNameInfo = _getRealFileTableFullPath(tableName2);
            string tableNewInfo = _getRealFileTableFullPath(newName);
            MemoryStream fs = new MemoryStream();
            db.FileStorage.Download(tableNameInfo, fs);
            fs.Position = 0L;
            byte[] buf = fs.ToArray();
            fs.Dispose();
            //
            IRQ_FileTableInfo ft = null;
            string strFileTableInfo = Encoding.UTF8.GetString(buf);
            ft = SimpleJsonEx.SimpleJson.DeserializeObject<IRQ_FileTableInfo>(strFileTableInfo);
            //更新子目录下的所有文件名
            ft.FileTableName = newName;
            //for (int i = 0; i < ft.FileList.Count; i++) {
            //    ft.FileList[i].FileDir = ft.FileList[i].FileDir.Remove(0, tableName2.Length);
            //    ft.FileList[i].FileDir = newName + ft.FileList[i].FileDir;
            //}
            //保存回去
            string json = SimpleJsonEx.SimpleJson.SerializeObject(ft);
            byte[] buf2 = Encoding.UTF8.GetBytes(json);
            MemoryStream ms = new MemoryStream(buf2);
            if (db.FileStorage.Exists(tableNewInfo)) {
                db.FileStorage.Delete(tableNewInfo);
            }
            db.FileStorage.Upload(new LiteFileInfo(tableNewInfo, newName), ms);
            ms.Dispose();
            //删除老的
            db.FileStorage.Delete(tableName);//删除对应的数据
            db.FileStorage.Delete(tableNameInfo);//删除对应的JSON
            //
        }

        public int GetFileTableList(out List<string> ret) {
            ret = new List<string>();
            IEnumerable<LiteFileInfo> files = db.FileStorage.Find(_filetable_Prefix);
            foreach (var v in files) {
                ret.Add(v.Filename);
            }
            return ret.Count;
        }


        public bool InUpdateing = false;
        public void BeginUpdate(IFilePackerStrategy file) {
            //throw new NotImplementedException();
            InUpdateing = true;

        }

        public void BeginUpdate(string name) {
            //throw new NotImplementedException();
            InUpdateing = true;
        }

        public void EndUpdate(IFilePackerStrategy file, bool success) {
            //throw new NotImplementedException();
            InUpdateing = false;
        }

        public void EndUpdate(string name, bool success) {
            //throw new NotImplementedException();
            InUpdateing = false;
        }

        public void Close() {
            if (this.db != null)
                this.db.Dispose();
            this.db = null;
        }
        internal static IRQ_Packer OpenPacker(string strFileName, string arg, bool createIfNotExists) {

            IRQ_Packer pk = new IRQ_Packer();
            bool nofile = false;
            if (!File.Exists(strFileName)) {
                nofile = true;
                if (!createIfNotExists) {
                    throw new Exception("not exist: " + strFileName + " in open packer");
                }
            }
            LiteDB.LiteDatabase db = new LiteDatabase(strFileName);
            string dbinfo = "$lite$/dbcache/sys.tmp";
            byte[] pbuf = Encoding.UTF8.GetBytes(arg);//arg必须不能啊为空
            if (nofile && !string.IsNullOrEmpty(arg)) {
                MemoryStream msP = new MemoryStream(pbuf);
                LiteDB.LiteXorEncoderStream es = new LiteXorEncoderStream(msP);
                db.FileStorage.Upload(dbinfo, es);
                es.Close();
                msP.Dispose();

            }
            if (!db.FileStorage.Exists(dbinfo)) {
                throw new Exception("not find the dbinfo in lite file");
            }

            byte[] pbuf2 = null;
            MemoryStream msP2 = new MemoryStream();
            LiteXorDecoderStream des = new LiteXorDecoderStream(msP2);
            db.FileStorage.Download(dbinfo, des);
            pbuf2 = msP2.ToArray();
            des.Close();
            msP2.Dispose();
            //
            if (pbuf.Length != pbuf2.Length) {
                throw new Exception("the lite pwd is not match!");
            }
            for (int i = 0; i < pbuf.Length; i++) {
                if (pbuf[i] != pbuf2[i]) {
                    throw new Exception("the lite pwd is not match!");
                }
            }
            //
            pk.db = db;
            pk._dbPstr = arg;
            //ToDo:不同的路径位置是不一样的

            return pk;

        }

        internal void ChangeArg(string arg) {
            //if (string.IsNullOrEmpty(arg)) throw new Exception("lite change arg is empty!");
            string dbinfo = "$lite$/dbcache/sys.tmp";

            if (!db.FileStorage.Exists(dbinfo)) {
                throw new Exception("not find the dbinfo in lite file");
            }
            byte[] pbuf = Encoding.UTF8.GetBytes(arg);
            if (string.IsNullOrEmpty(arg)) {
                MemoryStream msP = new MemoryStream(pbuf);
                LiteDB.LiteXorEncoderStream es = new LiteXorEncoderStream(msP);
                if (db.FileStorage.Exists(dbinfo)) {
                    db.FileStorage.Delete(dbinfo);
                }
                db.FileStorage.Upload(dbinfo, es);
                this._dbPstr = arg;
                es.Close();
                msP.Dispose();
                //

            }


        }
    }

    /// <summary>
    /// 以文件名,文件内容,文件长度组成的文件表的存储方式进行打包
    /// 结构
    /// FileDir,FileName,FileData,FileLen
    /// </summary>
    internal class IRQ_FileTable : IFilePackerStrategy
    {

        private LiteDatabase db;
        private string _dbP;
        private string _dbc;
        private IFilePacker m_Packer;
        public IFilePacker Packer {
            get { return m_Packer; }
        }

        internal IRQ_FileTable(IFilePacker packer, string pw, LiteDatabase db) {
            this.db = db;
            this._dbc = db.ConnectionString.Filename;
            this._dbP = pw;
            m_Packer = packer;
        }

        //    private SQLiteConnection m_Conn;
        //    private System.Security.SecureString m_buf;
        //    private SQLiteTransaction m_Trans;
        //    private IFilePacker m_Packer;
        private void CheckConnection() {
            if (db.IsDisposed) {
                db = new LiteDatabase(_dbc);
            }
        }

        /// <summary>
        /// 文件表名
        /// </summary>
        public string Name {
            get;
            set;
        }
        internal IRQ_FileTableInfo _getFileTableInfo() {
            string tableNameInfo = IRQ_Packer._getRealFileTableFullPath(this.Name);
            MemoryStream fs = new MemoryStream();
            db.FileStorage.Download(tableNameInfo, fs);
            fs.Position = 0L;
            byte[] buf = fs.ToArray();
            fs.Close();
            //
            IRQ_FileTableInfo ft = null;
            string strFileTableInfo = Encoding.UTF8.GetString(buf);
            ft = SimpleJsonEx.SimpleJson.DeserializeObject<IRQ_FileTableInfo>(strFileTableInfo);
            return ft;
        }
        void _saveFileTableInfo(IRQ_FileTableInfo tinfo) {
            if (tinfo.FileTableName != this.Name) {
                throw new Exception("_updateFileTableInfo 出现错误，tablefile.Name是否被修改？");
            }
            //            
            string strFileTableInfo = string.Empty;
            strFileTableInfo = SimpleJsonEx.SimpleJson.SerializeObject(tinfo);
            byte[] buf = Encoding.UTF8.GetBytes(strFileTableInfo);
            MemoryStream ms = new MemoryStream(buf);
            if (db.FileStorage.Exists(tinfo._get_Id())) {
                db.FileStorage.Delete(tinfo._get_Id());
            }
            db.FileStorage.Upload(tinfo._get_Id(), ms);
            ms.Dispose();
        }
        /// <summary>
        /// 获取某个目录的所有文件数量,并输出文件列表
        /// 不支持"."和".."
        /// </summary>
        /// <param name="strDir">目标目录,可以为string.Empty。</param>
        /// <param name="fileNames">文件列表</param>
        /// <param name="totalSize">总尺寸</param>
        /// <returns>数量</returns>
        public int GetFiles(string strDir, out List<string> fileNames, out int totalSize) {
            if (string.IsNullOrEmpty(strDir)) strDir = "";
            else if (string.IsNullOrEmpty(strDir.Trim())) strDir = "";
            if (string.IsNullOrEmpty(strDir)) strDir = this.Name;
            strDir = IRQ_Packer._getFileLegalLowerDir(strDir);
            IRQ_FileTableInfo table = _getFileTableInfo();

            fileNames = new List<string>();
            totalSize = 0;
            //if (string.IsNullOrEmpty(strDir)) {
            //    foreach (var v in table.FileList) {
            //        fileNames.Add(v.FileName);
            //        totalSize += v.FileLen;
            //    }                
            //}
            //else {
            foreach (var v in table.FileList) {
                if (EqualDir(v.FileDir, strDir)) {
                    fileNames.Add(v.FileName);
                    totalSize += v.FileLen;
                }
            }
            //}
            return fileNames.Count;

        }
        static bool EqualDir(string dir1, string dir2) {
            dir1 = IRQ_Packer._getFileLegalLowerDir(dir1);
            dir2 = IRQ_Packer._getFileLegalLowerDir(dir2);
            return dir1.Equals(dir2);
        }
        public int GetDirs(out List<string> dirs) {
            string tableinfoId = IRQ_Packer._getRealFileTableFullPath(this.Name);
            dirs = new List<string>();
            IRQ_FileTableInfo table = _getFileTableInfo();
            foreach (var v in table.FileList) {
                if (!dirs.Contains(v.FileDir)) {
                    dirs.Add(v.FileDir);

                }
            }
            return dirs.Count;
        }
        /// <summary>
        /// 增加文件并将文件内容添加到表中.
        /// 如果文件名名中包含参数，则获取该文件时也需要加上参数。
        /// 如:AddFile("aa\\bb.txt")
        /// 则在FileExis和GetFile时，其参数为:FileExists("aa\\bb.txt")
        /// </summary>
        /// <param name="strFileName"></param>
        public void AddFile(string strFileName) {
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }

            byte[] buf;
            using (FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read)) {
                buf = new byte[fs.Length];
                fs.Read(buf, 0, buf.Length);
            }
            AddFile(strFileName, buf);
        }
        //    internal SQLiteTransaction m_trans;

        /// <summary>
        /// 增加文件
        /// 如果文件名名中包含参数，则获取该文件时也需要加上参数。
        /// 如:AddFile("aa\\bb.txt")
        /// 则在FileExis和GetFile时，其参数为:FileExists("aa\\bb.txt")
        /// </summary>
        /// <param name="strFileName"></param>
        /// <param name="fileData">文件内容</param>
        public void AddFile(string strFileName, byte[] fileData) {
            AddFile(strFileName, fileData, DateTime.Now);
        }
        /// <summary>
        /// 增加文件
        /// 如果文件名名中包含参数，则获取该文件时也需要加上参数。
        /// 如:AddFile("aa\\bb.txt")
        /// 则在FileExis和GetFile时，其参数为:FileExists("aa\\bb.txt")
        /// </summary>
        /// <param name="strFileName"></param>
        /// <param name="fs">文件流</param>
        public void AddFile(string strFileName, Stream fs) {
            AddFile(strFileName, fs, DateTime.Now);
        }
        /// <summary>
        /// 更新文件内容
        /// </summary>
        /// <param name="strFileName"></param>
        public void UpdateFile(string strFileName) {
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }

            byte[] buf;
            using (FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read)) {
                buf = new byte[fs.Length];
                fs.Read(buf, 0, buf.Length);
            }
            UpdateFile(strFileName, buf);
        }

        /// <summary>
        /// 更新文件内容
        /// </summary>
        /// <param name="strFileName"></param>
        /// <param name="fileData">新文件的内容</param>
        public void UpdateFile(string strFileName, byte[] fileData) {

            UpdateFile(strFileName, fileData, DateTime.Now);

        }
        /// <summary>
        /// 更新文件内容
        /// </summary>
        /// <param name="strFileName"></param>
        /// <param name="fs">新文件流</param>
        public void UpdateFile(string strFileName, Stream fs) {

            UpdateFile(strFileName, fs, DateTime.Now);
        }

        public void RenameFile(string strFileName, string strNewFileName) {
            IRQ_FileTableInfo t_info = _getFileTableInfo();
            string strFile;
            string strDir = GetFirstDir(strFileName, out strFile);
            strDir = IRQ_Packer._getFileLegalLowerDir(strDir);
            IRQ_FileTableFileInfo f_info = null;
            foreach (var v in t_info.FileList) {
                if (EqualDir(v.FileDir, strDir) &&
                    v.FileName.Equals(strFile)) {
                    f_info = v;
                    break;
                }
            }
            if (f_info != null) {
                string strFileN;
                string strDirN = GetFirstDir(strNewFileName, out strFileN);
                strDirN = IRQ_Packer._getFileLegalLowerDir(strDirN);
                f_info.FileName = strFileN;
                f_info.FileDir = strDirN;
                _saveFileTableInfo(t_info);//修改
            }
        }
        public void RenameDir(string strDirName, string strNewDirName) {
            IRQ_FileTableInfo ft = _getFileTableInfo();
            string strDir = IRQ_Packer._getFileLegalLowerDir(strDirName);
            //更新子目录下的所有文件名           
            for (int i = 0; i < ft.FileList.Count; i++) {
                if (ft.FileList[i].FileDir.StartsWith(strDir)) {//处于相同目录的文件目录修改
                    ft.FileList[i].FileDir = ft.FileList[i].FileDir.Remove(0, strDir.Length);
                    ft.FileList[i].FileDir = IRQ_Packer._getFileLegalLowerDir(strNewDirName) + ft.FileList[i].FileDir;
                }
            }
            _saveFileTableInfo(ft);
        }
        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="strFileName"></param>
        public void DelFile(string strFileName) {
            if (string.IsNullOrEmpty(strFileName)) {
                return;
            }
            try {
                IRQ_FileTableInfo ft = _getFileTableInfo();
                string strFile;
                string strDir = GetFirstDir(strFileName, out strFile);
                strDir = IRQ_Packer._getFileLegalLowerDir(strDir);
                IRQ_FileTableFileInfo fi = null;
                foreach (var v in ft.FileList) {
                    if (EqualDir(v.FileDir, strDir) &&
                        v.FileName.Equals(strFile)) {
                        fi = v;
                        break;
                    }
                }
                ft.FileList.Remove(fi);
                ft.FileCount--;
                ft.TotalSize -= fi.FileLen;
                db.FileStorage.Delete(fi._GuidData);
                _saveFileTableInfo(ft);
            }
            catch (Exception ee) {
                throw new AccessPackerException("删除文件" + strFileName + "时发生错误!", ee);
            }
        }
        /// <summary>
        /// 查询文件是否存在.
        /// </summary>
        /// <param name="strFileName">完全路径名</param>
        /// <returns></returns>
        public bool FileExists(string strFileName) {
            if (string.IsNullOrEmpty(strFileName)) {
                return true;
            }

            try {
                IRQ_FileTableInfo ft = _getFileTableInfo();
                string strFile;
                string strDir = GetFirstDir(strFileName, out strFile);
                strDir = IRQ_Packer._getFileLegalLowerDir(strDir);
                IRQ_FileTableFileInfo fi = null;
                foreach (var v in ft.FileList) {
                    if (EqualDir(v.FileDir, strDir) &&
                        v.FileName.Equals(strFile)) {
                        fi = v;
                        break;
                    }
                }
                return fi != null;
            }
            catch (Exception ee) {
                throw new AccessPackerException("获取文件 " + strFileName + "的信息失败!", ee);
            }

        }
        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="strFileName"></param>
        /// <returns></returns>
        public byte[] OpenFile(string strFileName) {
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            IRQ_FileTableInfo ft = _getFileTableInfo();
            string strFile;
            string strDir = GetFirstDir(strFileName, out strFile);
            strDir = IRQ_Packer._getFileLegalLowerDir(strDir);
            IRQ_FileTableFileInfo fi = null;
            foreach (var v in ft.FileList) {
                if (EqualDir(v.FileDir, strDir) &&
                    v.FileName.Equals(strFile)) {
                    fi = v;
                    break;
                }
            }
            if (fi == null) {
                throw new Exception("OpenFile(string strFileName)" + strFileName + " 不存在.");
            }
            MemoryStream lfs = new MemoryStream();
            db.FileStorage.Download(fi._GuidData, lfs);
            lfs.Position = 0L;
            byte[] buf = lfs.ToArray();
            lfs.Dispose();
            return buf;
        }
        public Stream OpenFileAsStream(string strFileName) {
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            byte[] buf = OpenFile(strFileName);
            MemoryStream ms = new MemoryStream(buf);
            return ms;
        }
        public string OpenFileAsString(string strFileName) {

            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }

            byte[] buf = OpenFile(strFileName);
            using (MemoryStream ms = new MemoryStream(buf)) {
                using (StreamReader sr = new StreamReader(ms, Encoding.UTF8)) {
                    return sr.ReadToEnd();
                }
            }
        }

        public void DelDir(string strDir) {
            if (string.IsNullOrEmpty(strDir)) {
                return;
            }
            try {
                //删除目录
                IRQ_FileTableInfo ft = _getFileTableInfo();
                //string strFile;
                //string strDir = GetFirstDir(strFileName, out strFile);
                strDir = IRQ_Packer._getFileLegalLowerDir(strDir);
                List<IRQ_FileTableFileInfo> fis = new List<IRQ_FileTableFileInfo>();
                foreach (var v in ft.FileList) {
                    if (v.FileDir.StartsWith(strDir)) {
                        fis.Add(v);
                    }
                }
                //删除
                foreach (var v in fis) {
                    ft.FileList.Remove(v);
                    ft.FileCount--;
                    ft.TotalSize -= v.FileLen;
                    db.FileStorage.Delete(v._GuidData);
                }
                _saveFileTableInfo(ft);
            }
            catch (Exception ee) {
                throw new AccessPackerException("删除文件夹" + strDir + "时发生错误!", ee);
            }
        }

        /// <summary>
        /// 执行清理操作.
        /// </summary>
        public void Clean() {
            //CheckConnection();
            //using (SQLiteCommand cmd = new SQLiteCommand("VACUUM", m_Conn)) {
            //    cmd.ExecuteNonQuery();
            //}

        }

        //    #region IFilePackerStrategy 成员


        public void AddFile(string strFileName/*必须是相对FILETABLE的相对路径*/, DateTime date) {
            byte[] buf;
            using (FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read)) {
                buf = new byte[fs.Length];
                fs.Read(buf, 0, buf.Length);
            }
            AddFile(strFileName, buf, date);
        }

        public void AddFile(string strFileName/*必须是相对FILETABLE的相对路径*/, Stream fs, DateTime date) {
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }

            if (fs == null) return;
            if (!fs.CanRead) return;
            byte[] buf;
            if (fs.CanSeek) {
                buf = new byte[fs.Length];
                fs.Read(buf, 0, buf.Length);
            }
            else {
                using (MemoryStream ms = new MemoryStream(10240)) {
                    int b = fs.ReadByte();
                    while (b != -1) {
                        ms.WriteByte((byte)b);
                        b = fs.ReadByte();
                    }
                    buf = ms.ToArray();
                }
            }

            AddFile(strFileName, buf, date);
        }

        public void AddFile(string strFileName/*必须是相对FILETABLE的相对路径*/, byte[] fileData, DateTime date) {
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }

            //string strDir = Path.GetDirectoryName(strFileName);
            //string strFile = Path.GetFileName(strFileName);
            string strFile;
            string strDir = GetFirstDir(strFileName, out strFile);
            try {
                strDir = IRQ_Packer._getFileLegalLowerDir(strDir);
                IRQ_FileTableFileInfo fileinfo = new IRQ_FileTableFileInfo();
                fileinfo._GuidData = IRQ_Packer._filetable_DBPrefix+"_" + Guid.NewGuid().ToString("N");
                fileinfo.FileDir = strDir;
                fileinfo.FileLen = fileData.Length;
                fileinfo.FileName = strFile;
                fileinfo.FileUpdateTime = date.ToBinary();
                MemoryStream ms = new MemoryStream(fileData);
                if (db.FileStorage.Exists(fileinfo._GuidData)) {
                    db.FileStorage.Delete(fileinfo._GuidData);
                }
                db.FileStorage.Upload(fileinfo._GuidData, ms);
                ms.Dispose();
                //取得表的信息
                IRQ_FileTableInfo tinfo = _getFileTableInfo();
                tinfo.FileList.Add(fileinfo);
                tinfo.FileCount++;
                tinfo.TotalSize += fileinfo.FileLen;
                _saveFileTableInfo(tinfo);
            }
            catch (Exception ee) {
                throw new AccessPackerException("添加文件" + strFileName + "时发生错误!", ee);
            }
        }

        public void UpdateFile(string strFileName, DateTime date) {
            byte[] buf;
            using (FileStream fs = new FileStream(strFileName, FileMode.Open, FileAccess.Read)) {
                buf = new byte[fs.Length];
                fs.Read(buf, 0, buf.Length);
            }
            UpdateFile(strFileName, buf, date);
        }

        public void UpdateFile(string strFileName, Stream fs, DateTime date) {
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }

            if (fs == null) return;
            if (!fs.CanRead) return;
            byte[] buf;
            if (fs.CanSeek) {
                buf = new byte[fs.Length];
                fs.Read(buf, 0, buf.Length);
            }
            else {
                using (MemoryStream ms = new MemoryStream(10240)) {
                    int b = fs.ReadByte();
                    while (b != -1) {
                        ms.WriteByte((byte)b);
                        b = fs.ReadByte();
                    }
                    buf = ms.ToArray();
                }
            }

            UpdateFile(strFileName, buf, date);
        }

        public void UpdateFile(string strFileName, byte[] fileData, DateTime date) {
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }

            //string strDir = Path.GetDirectoryName(strFileName);
            //string strFile = Path.GetFileName(strFileName);
            string strFile;
            string strDir = GetFirstDir(strFileName, out strFile);
            try {
                strDir = IRQ_Packer._getFileLegalLowerDir(strDir);
                IRQ_FileTableInfo tinfo = _getFileTableInfo();
                IRQ_FileTableFileInfo finfo = null;
                foreach (var v in tinfo.FileList) {
                    if (v.FileDir.Equals(strDir) &&
                        v.FileName.Equals(strFile)) {
                        finfo = v; break;
                    }
                }
                if (finfo == null) {
                    finfo = new IRQ_FileTableFileInfo();
                    finfo._GuidData = IRQ_Packer._filetable_DBPrefix +"_"+ Guid.NewGuid().ToString("N");
                    finfo.FileDir = strDir;
                    finfo.FileName = strFile;
                    finfo.FileLen = fileData.Length;
                    finfo.FileUpdateTime = date.ToBinary();
                    //
                    tinfo.FileList.Add(finfo);
                    tinfo.FileCount++;
                    tinfo.TotalSize += finfo.FileLen;
                }
                tinfo.TotalSize += (fileData.Length - finfo.FileLen);
                finfo.FileUpdateTime = date.ToBinary();
                //
                MemoryStream ms = new MemoryStream(fileData);
                if (db.FileStorage.Exists(finfo._GuidData)) {
                    db.FileStorage.Delete(finfo._GuidData);
                }
                db.FileStorage.Upload(finfo._GuidData, ms);
                ms.Dispose();
                _saveFileTableInfo(tinfo);
            }
            catch (Exception ee) {
                throw new AccessPackerException("更新文件" + strFileName + "时发生错误", ee);
            }
        }

        public DateTime GetUpdateDate(string strFileName) {
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }


            string strFile;
            string strDir = GetFirstDir(strFileName, out strFile);
            try {
                strDir = IRQ_Packer._getFileLegalLowerDir(strDir);
                IRQ_FileTableInfo tinfo = _getFileTableInfo();
                IRQ_FileTableFileInfo finfo = null;
                foreach (var v in tinfo.FileList) {
                    if (v.FileDir.Equals(strDir) &&
                        v.FileName.Equals(strFile)) {
                        finfo = v;
                        break;
                    }
                }
                if (finfo != null) {
                    return DateTime.FromBinary(finfo.FileUpdateTime);
                }
                return DateTime.MinValue;
            }
            catch (Exception ee) {
                throw new AccessPackerException("打开文件" + strFileName + "时发生错误!", ee);
            }

        }



        //    #endregion
        /// <summary>
        /// 返回去除文件名的目录
        /// </summary>
        /// <param name="strFileName">全路径文件名</param>
        /// <param name="remain">文件名</param>
        /// <returns></returns>
        private static string GetFirstDir(string strFileName, out string remain) {
            string tmp1 = Path.GetDirectoryName(strFileName);
            string tmp2 = Path.GetFileName(strFileName);
            strFileName = tmp1 + "\\" + tmp2;
            int p = strFileName.IndexOf("\\");
            if (p == -1) {
                remain = strFileName;
                return "";
            }
            string ret = strFileName.Substring(0, p);
            remain = strFileName.Substring(p + 1, strFileName.Length - p - 1);
            return ret;
        }
    }

    internal class AccessPackerException : Exception
    {
        public AccessPackerException(string msg, Exception inner) : base(msg, inner) { }
    }

}
