using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LiteDB;

namespace LiteDB_Test
{
    public partial class Form1 : Form
    {
        public Form1() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            LiteDB.LiteDatabase db = new LiteDatabase("test.nosql");
            byte[] buf = new byte[] { 1, 2, 3, 4,5,6,7,8,9,10 };
            using (MemoryStream ms = new MemoryStream(buf)) {
                //db.BeginTrans();
                try {
                    if (db.FileStorage.Exists("10005/1.txt")) db.FileStorage.Delete("10005/1.txt");
                    db.FileStorage.Upload("10005/1.txt", ms);
                    if (db.FileStorage.Exists("10005/2.txt")) db.FileStorage.Delete("10005/2.txt");
                    db.FileStorage.Upload("10005/2.txt", ms);
                    if (db.FileStorage.Exists("10005/3.txt")) db.FileStorage.Delete("10005/3.txt");
                    db.FileStorage.Upload("10005/3.txt", ms);
                    if (db.FileStorage.Exists("10005/4.txt")) db.FileStorage.Delete("10005/4.txt");
                    db.FileStorage.Upload("10005/4.txt", ms);
                   
                    // Get file reference using file id
                    var file = db.FileStorage.FindById("10005/1.txt");

                    // Find all files using StartsWith
                    IEnumerable<LiteFileInfo> files = db.FileStorage.Find("10005/"); //db.Files.Find("my_");
                   
                    foreach (var vf in files) {
                        Console.WriteLine(vf.Id+"     "+vf.Filename);
                    }
                    // Get file stream
                    var stream = file.OpenRead();
                    MemoryStream ms2 = new MemoryStream();
                    // Write file stream in a external stream
                    db.FileStorage.Download("10005/1.txt", ms2);
                    byte[] buf2 = ms2.ToArray();
                    db.FileStorage.Delete("10005/1.txt");
                    //db.Commit();
                }
                catch {
                   // db.Rollback();
                }

            }
        }
    }
}
