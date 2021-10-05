using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ShellHost
{
    class Process : System.Diagnostics.Process
    {
        public Process() : base() { 

        }

        public void execute(string cmd) {
            this.StandardInput.WriteLine(cmd);
            this.StandardInput.Flush();
        }
    }
}
