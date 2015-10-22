using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        #region Find

        /// <summary>
        /// Find documents inside a collection using Query object. Must have indexes in query expression 
        /// </summary>
        public IEnumerable<T> Find(Query query, int skip = 0, int limit = int.MaxValue)
        {
            if (query == null) throw new ArgumentNullException("query");

            var nodes = query.Run<T>(this);

            if (skip > 0) nodes = nodes.Skip(skip);

            if (limit != int.MaxValue) nodes = nodes.Take(limit);

            foreach (var node in nodes)
            {
                var dataBlock = this.Database.Data.Read(node.DataBlock, true);

                var doc = BsonSerializer.Deserialize(dataBlock.Buffer).AsDocument;

                // get object from BsonDocument
                var obj = this.Database.Mapper.ToObject<T>(doc);

                foreach (var action in _includes)
                {
                    action(obj);
                }

                yield return obj;
            }
        }

        /// <summary>
        /// Find documents inside a collection using Linq expression. Must have indexes in linq expression 
        /// </summary>
        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue)
        {
            return this.Find(_visitor.Visit(predicate), skip, limit);
        }

        #endregion

        #region FindById + One + All

        /// <summary>
        /// Find a document using Document Id. Returns null if not found.
        /// </summary>
        public T FindById(BsonValue id)
        {
            if (id == null || id.IsNull) throw new ArgumentNullException("id");

            return this.Find(Query.EQ("_id", id)).SingleOrDefault();
        }

        /// <summary>
        /// Find the first document using Query object. Returns null if not found. Must have index on query expression.
        /// </summary>
        public T FindOne(Query query)
        {
            return this.Find(query).FirstOrDefault();
        }

        /// <summary>
        /// Find the first document using Linq expression. Returns null if not found. Must have indexes on predicate.
        /// </summary>
        public T FindOne(Expression<Func<T, bool>> predicate)
        {
            return this.Find(_visitor.Visit(predicate)).FirstOrDefault();
        }

        /// <summary>
        /// Returns all documents inside collection order by _id index.
        /// </summary>
        public IEnumerable<T> FindAll()
        {
            return this.Find(Query.All());
        }

        #endregion

        #region Count/Exits

        /// <summary>
        /// Get document count using property on collection.
        /// </summary>
        public int Count()
        {
            var col = this.GetCollectionPage(false);

            if (col == null) return 0;

            return Convert.ToInt32(col.DocumentCount);
        }

        /// <summary>
        /// Count documnets with a query. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public int Count(Query query)
        {
            if (query == null) throw new ArgumentNullException("query");

            var nodes = query.Run<T>(this);

            return nodes.Count();
        }

        /// <summary>
        /// Count documnets with a query. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public int Count(Expression<Func<T, bool>> predicate)
        {
            return this.Count(_visitor.Visit(predicate));
        }

        /// <summary>
        /// Returns true if query returns any document. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public bool Exists(Query query)
        {
            if (query == null) throw new ArgumentNullException("query");

            var nodes = query.Run<T>(this);

            return nodes.FirstOrDefault() != null;
        }

        /// <summary>
        /// Returns true if query returns any document. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public bool Exists(Expression<Func<T, bool>> predicate)
        {
            return this.Exists(_visitor.Visit(predicate));
        }

        #endregion

        #region Min/Max

        /// <summary>
        /// Returns the first/min value from a index field
        /// </summary>
        public BsonValue Min(string field)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException("field");

            var col = this.GetCollectionPage(false);

            if (col == null) return BsonValue.MinValue;

            var index = col.GetIndex(field);
            var head = this.Database.Indexer.GetNode(index.HeadNode);
            var next = this.Database.Indexer.GetNode(head.Next[0]);

            if (next.IsHeadTail(index)) return BsonValue.MinValue;

            return next.Key;
        }

        /// <summary>
        /// Returns the first/min _id field
        /// </summary>
        public BsonValue Min()
        {
            return this.Min("_id");
        }

        /// <summary>
        /// Returns the first/min field using a linq expression
        /// </summary>
        public BsonValue Min<K>(Expression<Func<T, K>> property)
        {
            var field = _visitor.GetBsonField(property);

            return this.Min(field);
        }

        /// <summary>
        /// Returns the last/max value from a index field
        /// </summary>
        public BsonValue Max(string field)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException("field");

            var col = this.GetCollectionPage(false);

            if (col == null) return BsonValue.MaxValue;

            var index = col.GetIndex(field);
            var tail = this.Database.Indexer.GetNode(index.TailNode);
            var prev = this.Database.Indexer.GetNode(tail.Prev[0]);

            if (prev.IsHeadTail(index)) return BsonValue.MaxValue;

            return prev.Key;
        }

        /// <summary>
        /// Returns the last/max _id field
        /// </summary>
        public BsonValue Max()
        {
            return this.Max("_id");
        }

        /// <summary>
        /// Returns the last/max field using a linq expression
        /// </summary>
        public BsonValue Max<K>(Expression<Func<T, K>> property)
        {
            var field = _visitor.GetBsonField(property);

            return this.Max(field);
        }

        #endregion
		
		#region 添加分页查询
        /*
         var col = db.GetCollection<Customer>("customers");
        //取第二页，降序
        var data = col.FindBySplitePage<Int32>(n => n.Name.StartsWith("Jim1"), n => n.Age, true, 3, 2).ToList();
         */
        /// <summary>分页获取记录</summary>
        /// <typeparam name="TOder">排序字段类型</typeparam>
        /// <param name="predicate">linq查询表达式</param>
        /// <param name="orderSelector">排序表达式</param>
        /// <param name="isDescending">是否降序,true降序</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="pageIndex">要获取的页码，从1开始</param>
        /// <returns>分页后的数据</returns>
        public IEnumerable<T> FindBySplitePage<TOder>(Expression<Func<T, bool>> predicate,
            Func<T, TOder> orderSelector, Boolean isDescending, int pageSize, int pageIndex) {
            var allCount = Count(predicate);//计算总数
            if (allCount == 0) return new T[0] ;
            var pages = (int)Math.Ceiling((double)allCount / (double)pageSize);//计算页码
            if (pageIndex > pages) throw new Exception("页面数超过预期");
            if (isDescending) {//降序
                return Find(predicate)
                              .OrderByDescending(orderSelector)
                              .Skip((pageIndex - 1) * pageSize)
                              .Take(pageSize);
            }
            else {//升序
                return Find(predicate)
                             .OrderBy(orderSelector)
                             .Skip((pageIndex - 1) * pageSize)
                             .Take(pageSize);
            }
        }
        #endregion
    }
}
