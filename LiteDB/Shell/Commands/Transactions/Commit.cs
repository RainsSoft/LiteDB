﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Commit : ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"commit(\s+trans)?$").Length > 0;
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            db.Commit();

            return BsonValue.Null;
        }
    }
}
