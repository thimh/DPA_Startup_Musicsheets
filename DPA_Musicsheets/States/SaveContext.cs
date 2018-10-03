using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPA_Musicsheets.States
{
    public interface SaveContext
    {
        SaveState CurrentSaveState { get; set; }

        void ShowMessage(string title, string message, CancelEventArgs e);
    }
}
