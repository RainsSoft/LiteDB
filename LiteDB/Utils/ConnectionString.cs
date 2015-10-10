using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Manage ConnectionString to connect and create databases. Can be used as:
    /// * If only a word - get from App.Config
    /// * If is a path - use all parameters as default
    /// * Otherwise, is name=value collection
    /// </summary>
    public class ConnectionString
    {
        /// <summary>
        /// Path of filename (no default - required key)
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Default Timeout connection to wait for unlock (default: 1 minute)
        /// </summary>
        public TimeSpan Timeout { get; private set; }

        /// <summary>
        /// Supports recovery mode if a fail during write pages to disk
        /// 支持恢复模式，如果在写页到磁盘的失败
        /// </summary>
        public bool JournalEnabled { get; private set; }

        /// <summary>
        /// Jounal filename with full path
        /// 日志文件名的完整路径
        /// </summary>
        internal string JournalFilename { get; private set; }

        /// <summary>
        /// Define, in connection string, the user database version. When you increse this value
        /// LiteDatabase will run OnUpdate method for each new version. If defined, must be >= 1. Default: 1
        /// 定义，在连接字符串中，用户数据库版本。当你增加这个值litedatabase会为每一个新版本的更新方法。如果定义，必须是> = 1。默认：1
        /// </summary>
        public int UserVersion { get; private set; }

        public ConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException("connectionString");

            // If is only a name, get connectionString from App.config
            //ToDo:remove with support unity3d
            // if (Regex.IsMatch(connectionString, @"^[\w-]+$"))
            //    connectionString = ConfigurationManager.ConnectionStrings[connectionString].ConnectionString;

            // Create a dictionary from string name=value collection
            var values = new Dictionary<string, string>();

            if(connectionString.Contains("="))
            {
                values = connectionString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Split(new char[] { '=' }, 2))
                    .ToDictionary(t => t[0].Trim().ToLower(), t => t.Length == 1 ? "" : t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
            }
            else
            {
                // If connectionstring is only a filename, set filename 
                values["filename"] = Path.GetFullPath(connectionString);
            }

            // Read connection string parameters with default value
            this.Timeout = this.GetValue<TimeSpan>(values, "timeout", new TimeSpan(0, 1, 0));
            this.Filename = Path.GetFullPath(this.GetValue<string>(values, "filename", ""));
            this.JournalEnabled = this.GetValue<bool>(values, "journal", true);
            this.UserVersion = this.GetValue<int>(values, "version", 1);
            this.JournalFilename = Path.Combine(Path.GetDirectoryName(this.Filename), 
                Path.GetFileNameWithoutExtension(this.Filename) + "-journal" + 
                Path.GetExtension(this.Filename));

            // validade parameter values
            if (string.IsNullOrEmpty(Filename)) throw new ArgumentException("Missing FileName in ConnectionString");
            if (this.UserVersion <= 0) throw new ArgumentException("Connection String version must be greater or equals to 1");
        }

        private T GetValue<T>(Dictionary<string, string> values, string key, T defaultValue)
        {
            return values.ContainsKey(key) ?
                (T)Convert.ChangeType(values[key], typeof(T)) :
                defaultValue;
        }
    }
}
