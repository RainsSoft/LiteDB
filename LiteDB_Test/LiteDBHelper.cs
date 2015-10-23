using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LiteDB;

namespace LiteDB_Test
{
    public static class LiteDBHelper
    {
        /// <summary>
        /// 注意：如果obj对象已经插入过，则需要使用Update方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="DB"></param>
        /// <param name="objClassName"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static BsonValue Save<T>(LiteDatabase DB,string objClassName,T obj)
            where T : new() {
            // Open data file (or create if not exits)
            using (var db = new LiteDatabase(DB.ConnectionString.Filename)) {
                // Get a collection (or create, if not exits)
                var col = db.GetCollection<T>(objClassName);
                // Insert new customer document
                BsonValue value = col.Insert(obj);
                return value;
            }
        }
        public static bool Update<T>(LiteDatabase DB, string objClassName, T obj)
            where T : new() {
            // Open data file (or create if not exits)
            using (var db = new LiteDatabase(DB.ConnectionString.Filename)) {
                // Get a collection (or create, if not exits)
                var col = db.GetCollection<T>(objClassName);
                // Update a document inside a collection
                bool success = col.Update(obj);
                return success;
            }
        }
        public static bool Delete(LiteDatabase DB, string objClassName, int docId) {
            // Open data file (or create if not exits)
            using (var db = new LiteDatabase(DB.ConnectionString.Filename)) {
                // Get a collection (or create, if not exits)
                var col = db.GetCollection(objClassName);
                bool success = col.Delete(docId);
                return success;
            }
        }
        public static BsonDocument FindById(LiteDatabase DB, string objClassName, int docId) {
            // Open data file (or create if not exits)
            using (var db = new LiteDatabase(DB.ConnectionString.Filename)) {
                // Get a collection (or create, if not exits)
                var col = db.GetCollection(objClassName);
                BsonDocument doc = col.FindById(docId);
                return doc;
            }
        }
        public static T FindById<T>(LiteDatabase DB, string objClassName, int docId)
            where T : new() {
            // Open data file (or create if not exits)
            using (var db = new LiteDatabase(DB.ConnectionString.Filename)) {
                // Get a collection (or create, if not exits)
                var col = db.GetCollection<T>(objClassName);
                //BsonDocument doc = col.FindById(docId);
                //return BsonToObject.ConvertTo<T>(doc);
                T doc = col.FindById(docId);
                return doc;
            }
        }
        public static IList<BsonDocument> FindAll(LiteDatabase DB,string objClassName) {
            // Open data file (or create if not exits)
            using (var db = new LiteDatabase(DB.ConnectionString.Filename)) {
                // Get a collection (or create, if not exits)
                var col = db.GetCollection(objClassName);
                var doc = col.FindAll().ToList();
                return doc;
            }
        }
        public static IList<T> FindAll<T>(LiteDatabase DB,string objClassName)
            where T : new() {
            // Open data file (or create if not exits)
            using (var db = new LiteDatabase(DB.ConnectionString.Filename)) {
                // Get a collection (or create, if not exits)
                var col = db.GetCollection<T>(objClassName);
                var docs = col.FindAll();
                return docs.ToList();
            }
        }
    }
}
