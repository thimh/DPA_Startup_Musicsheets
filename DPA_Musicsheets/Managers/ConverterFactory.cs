using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPA_Musicsheets.Managers
{
    class ConverterFactory
    {
        private Dictionary<string, Type> types;

        public void AddConverterType(string name, Type type)
        {
            types[name] = type;
        }

        public Converter CreateConverter(string type)
        {
            Type converterType = types[type];
            Converter converter = (Converter)Activator.CreateInstance(converterType);
            return converter;
        }
    }
}
