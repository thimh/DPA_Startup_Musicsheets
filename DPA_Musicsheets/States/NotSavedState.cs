using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPA_Musicsheets.States
{
    public class NotSavedState : SaveState
    {
        public NotSavedState(SaveContext context) : base(context)
        {

        }

        public override void NextState(SaveContext context)
        {

        }

        public override void Handle(CancelEventArgs e)
        {
            this.Context.ShowMessage("Save?", "Your changes have not been saved yet. Do you want to save?", e);
        }
    }
}
