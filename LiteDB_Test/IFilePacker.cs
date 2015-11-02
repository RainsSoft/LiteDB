using System;
using System.Collections.Generic;
using System.IO;
namespace IRobotQ.Packer
{

    /// <summary>
    /// 文件包接口
    /// </summary>
    public interface IFilePacker
    {
        IFilePackerStrategy AddFileTable(string name);
        IFilePackerStrategy GetFileTable(string name);
        void DelFileTable(string name);
        void RenameFileTable(string tableName, string newName);
        int GetFileTableList(out List<string> ret);
        bool IsTableExists(string name);
        void BeginUpdate(IFilePackerStrategy file);
        void BeginUpdate(string name);
        void EndUpdate(IFilePackerStrategy file, bool success);
        void EndUpdate(string name, bool success);
        void Close();
    }

    /// <summary>
    /// 文件访问接口
    /// </summary>
    public interface IFilePackerStrategy
    {

        IFilePacker Packer {
            get;
        }
        void AddFile(string strFileName);
        void AddFile(string strFileName, System.IO.Stream stream);
        void AddFile(string strFileName, byte[] fileData);
        void AddFile(string strFileName, DateTime date);
        void AddFile(string strFileName, System.IO.Stream stream, DateTime date);
        void AddFile(string strFileName, byte[] fileData, DateTime date);
        void RenameFile(string strFileName, string strNewFile);
        void RenameDir(string strDirName, string strNewDirName);
        void UpdateFile(string strFileName);
        void UpdateFile(string strFileName, System.IO.Stream stream);
        void UpdateFile(string strFileName, byte[] fileData);
        void UpdateFile(string strFileName, DateTime date);
        void UpdateFile(string strFileName, System.IO.Stream stream, DateTime date);
        void UpdateFile(string strFileName, byte[] fileData, DateTime date);
        void DelDir(string strDir);
        void DelFile(string strFileName);
        bool FileExists(string strFileName);
        DateTime GetUpdateDate(string strFileName);
        int GetFiles(string strDir, out List<string> fileNames, out int totalSize);
        int GetDirs(out List<string> dirs);
        /// <summary>
        /// 名称。
        /// </summary>
        string Name { get; set; }
        byte[] OpenFile(string strFileName);
        Stream OpenFileAsStream(string strFileName);
        string OpenFileAsString(string strFileName);
        void Clean();
    }
}
