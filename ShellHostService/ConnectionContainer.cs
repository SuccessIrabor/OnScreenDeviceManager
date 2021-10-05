using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;

namespace ShellHostService
{
    public sealed class ConnectionContainer
    {
        public string Name { get; set; }
        public AppServiceConnection Connection { get; set; }

        public ConnectionContainer(string name, AppServiceConnection connection)
        {
            this.Connection = connection;
            this.Name = name;
        }
    }
}
