using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPA_Musicsheets.Models
{
    class MyNote
    {
        private MyLength length;
        private char tone;
        private bool hasDot;
        private bool hasSharp;
        private bool hasFlat;

        public MyNote(MyLength length, char tone, bool hasDot = false, bool hasSharp = false, bool hasFlat = false)
        {
            this.length = length;
            this.tone = tone;
            this.hasDot = hasDot;
            this.hasSharp = hasSharp;
            this.hasFlat = hasFlat;
        }
    }
}
