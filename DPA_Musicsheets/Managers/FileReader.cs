using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPA_Musicsheets.Managers
{
    abstract class FileReader
    {
        public abstract void ReadFile(string fileName);
    }
}
