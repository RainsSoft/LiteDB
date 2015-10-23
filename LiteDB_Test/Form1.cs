using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 10; i++) {
                test1();
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
        }
        
        private static void test1() {
            // Open database (or create if not exits)
            using (var db = new LiteDatabase(Application.StartupPath + "/MyData.db")) {
                // Get customer collection
                var col = db.GetCollection<Customer>("customers");
                float cf = (float)col.Count();
                //取第二页，降序
                try {
                    var data = col.FindBySplitePage<int>(n => n.Name.StartsWith("Jo"), n => (int)n.mF, true, 10, 1);
                    foreach (var v in data) {
                        Console.WriteLine("id:" + v.Id.ToString() + "_mf:" + v.mF.ToString());
                    }
                }
                catch (Exception ee) {
                    MessageBox.Show(ee.ToString());
                }                
                // Create your new customer instance
                var customer = new Customer {
                    Id=0,
                    Name = "John Doe",
                    Phones = new string[] { "8000-0000", "9000-0000" },
                    IsActive = true,
                    mF=cf
                };

                // Insert new customer document (Id will be auto-incremented)
                col.Insert(customer);

                // Update a document inside a collection
                customer.Name = "Joana Doe_"+(cf/10).ToString();

                col.Update(customer);

                // Index document using a document property
                col.EnsureIndex(x => x.Name);
                //other io test
                customer.Name = "Job test"+cf.ToString();
                LiteDB_Test.LiteDBHelper.Update<Customer>(db, "customers", customer);
                // Use Linq to query documents
                var results = col.Find(x => x.Name.StartsWith("Jo"));
                foreach (var v in results) {
                    //Debug.Log(v.Name);
                    Console.WriteLine("name:"+v.Name+"_mf:"+v.mF.ToString());
                }
              

            }
        }

        void test2() {
            LiteDB.LiteDatabase db = new LiteDatabase(Application.StartupPath + "/test.nosql");
            byte[] buf = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            using (MemoryStream ms = new MemoryStream(buf)) {
                //db.BeginTrans();
                try {
                    ms.Position = 0;
                    if (db.FileStorage.Exists("10005/1.txt")) db.FileStorage.Delete("10005/1.txt");
                    db.FileStorage.Upload("10005/1.txt", ms);
                    ms.Position = 0;
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
                        Console.WriteLine(vf.Id + "     " + vf.Filename);
                        //Debug.Log(vf.Id + "     " + vf.Filename);
                    }
                    // Get file stream
                    var stream = file.OpenRead();
                    stream.Read(buf, 0, buf.Length);
                    MemoryStream ms2 = new MemoryStream();
                    // Write file stream in a external stream
                    db.FileStorage.Download("10005/2.txt", ms2);
                    byte[] buf2 = ms2.ToArray();
                    db.FileStorage.Delete("10005/1.txt");
                    //db.Commit();
                }
                catch {
                    // db.Rollback();
                }

            }
        }
        // Create your POCO class
        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string[] Phones { get; set; }
            bool ma;
            public bool IsActive { set { ma = value; } }

            public float mF { get; set; }
        }
    }
}
