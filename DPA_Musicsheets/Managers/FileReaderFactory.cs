using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPA_Musicsheets.Managers
{
    class FileReaderFactory
    {
        private Dictionary<string, Type> types;

        public void AddReaderType(string name, Type type)
        {
            types[name] = type;
        }

        public FileReader CreateReader(string type)
        {
            Type readerType = types[type];
            FileReader fileReader = (FileReader)Activator.CreateInstance(readerType);
            return fileReader;
        }
    }
}
