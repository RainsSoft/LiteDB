using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class IndexPage : BasePage
    {
        /// <summary>
        /// If a Index Page has less that this free space, it's considered full page for new items.
        /// </summary>
        public const int INDEX_RESERVED_BYTES = 100;

        public Dictionary<ushort, IndexNode> Nodes { get; set; }

        public IndexPage()
            : base()
        {
            this.PageType = PageType.Index;
            this.Nodes = new Dictionary<ushort, IndexNode>();
        }

        /// <summary>
        /// Clear page content - nodes
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            this.Nodes = new Dictionary<ushort, IndexNode>();
        }

        /// <summary>
        /// Update freebytes + items count
        /// </summary>
        public override void UpdateItemCount()
        {
            this.ItemCount = this.Nodes.Count;
            this.FreeBytes = PAGE_AVAILABLE_BYTES - this.Nodes.Sum(x => x.Value.Length);
        }

        public override void ReadContent(BinaryReader reader)
        {
            this.Nodes = new Dictionary<ushort, IndexNode>(this.ItemCount);

            for (var i = 0; i < this.ItemCount; i++)
            {
                var index = reader.ReadUInt16();
                var levels = reader.ReadByte();

                var node = new IndexNode(levels);

                node.Page = this;
                node.Position = new PageAddress(this.PageID, index);
                node.KeyLength = reader.ReadUInt16();
                node.Key = BinaryReaderExtensions.ReadBsonValue(reader, node.KeyLength);
                node.DataBlock = BinaryReaderExtensions.ReadPageAddress(reader);

                for (var j = 0; j < node.Prev.Length; j++)
                {
                    node.Prev[j] = BinaryReaderExtensions.ReadPageAddress(reader);
                    node.Next[j] = BinaryReaderExtensions.ReadPageAddress(reader);
                }

                this.Nodes.Add(node.Position.Index, node);
            }            
        }

        public override void WriteContent(BinaryWriter writer)
        {
            foreach (var node in this.Nodes.Values)
            {
                writer.Write(node.Position.Index); // node Index on this page
                writer.Write((byte)node.Prev.Length); // level length
                writer.Write(node.KeyLength); // valueLength
                BinaryWriterExtensions.WriteBsonValue(writer,node.Key, node.KeyLength); // value
                BinaryWriterExtensions.Write(writer,node.DataBlock); // data block reference

                for (var j = 0; j < node.Prev.Length; j++)
                {
                    BinaryWriterExtensions.Write(writer,node.Prev[j]);
                    BinaryWriterExtensions.Write(writer,node.Next[j]);
                }
            }
        }
    }
}
