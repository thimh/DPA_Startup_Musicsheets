using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPA_Musicsheets.States
{
    public class SavedState : SaveState
    {
        public SavedState(SaveContext context) : base(context)
        {

        }

        public override void NextState(SaveContext context)
        {
            this.Context.CurrentSaveState = new NotSavedState(Context);
        }

        public override void Handle(CancelEventArgs e)
        {

        }
    }
}
