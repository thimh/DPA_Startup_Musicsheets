using System.ComponentModel;
using System.Runtime.Remoting.Contexts;

namespace DPA_Musicsheets.States
{
    public abstract class SaveState
    {
        public SaveContext Context { get; set; }
        public abstract void NextState(SaveContext context);
        public abstract void Handle(CancelEventArgs e);

        public SaveState(SaveContext context)
        {
            this.Context = context;
        }
    }
}