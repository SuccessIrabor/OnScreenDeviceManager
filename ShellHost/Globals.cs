using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace ShellHost
{
    public enum ThreadState { 
        Suspended = 0,
        Active = 1,
        Exiting = 2
    }

    
    class Globals
    {
        public string DeviceName;
        public AppServiceConnection AppService;
        public Process Process;
        public ThreadState ThreadState;
        public int y;
        public bool deviceEnabled;

        public Globals()
        {
            ThreadState = ThreadState.Active;
            deviceEnabled = true;

            init(this);
        }

        private static string setExecutionPolicy(string level)
        {
            return $"Set-ExecutionPolicy {level}";
        }

        private static string getDevice(string name)
        {
            return $"Get-PnpDevice | where {{$_.friendlyname -like {name}}}";
        }

        private static string disableDevice(string deviceName)
        {
            return $"Get-PnpDevice | where {{$_.friendlyname -like {deviceName}}} | Disable-PnpDevice -Confirm:$false";
        }

        private static string enableDevice(string deviceName)
        {
            return $"Get-PnpDevice | where {{$_.friendlyname -like {deviceName}}} | Enable-PnpDevice -Confirm:$false";
        }

        private async Task<string> getDeviceName(AppServiceConnection appService, Globals globals)
        {
            var msg = new ValueSet();
            msg.Add("Command", "Get-DeviceName");

            Console.WriteLine("Getting device name...");

            var response = await appService.SendMessageAsync(msg);

            string deviceName = null;

            if (response.Status == AppServiceResponseStatus.Success && response.Message.Count > 0)
            {
                deviceName = response.Message["Value"] as string;

                Console.WriteLine($"Device-Name: {deviceName}");

                globals.DeviceName = deviceName;
            }
            else
            {
                Console.WriteLine("Failed to get device name!");
            }

            return deviceName;
        }

        private async Task<AppServiceConnectionStatus> openAppServiceConnection(AppServiceConnection appService, Globals globals)
        {
            Console.WriteLine("Establishing connection to app service...");

            var status = await appService.OpenAsync();

            if (status == AppServiceConnectionStatus.Success)
            {
                globals.AppService = appService;
                Console.WriteLine("Connection established!");
            }
            else
            {
                Console.WriteLine("Failed to establish connection!");
            }

            return status;
        }

        private async void AppService_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            Console.WriteLine("Request received!");

            // todo: remove
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var isBackground = Thread.CurrentThread.IsBackground;

            Console.WriteLine($"ThreadId: {threadId}");
            Console.WriteLine($"isBackground: {isBackground}");

            if (isBackground)
            {
                Thread.CurrentThread.IsBackground = false;
            }

            var messageDeferral = args.GetDeferral();
            
            Process proc = null;

            proc = this.Process;

            ValueSet message = args.Request.Message;
            ValueSet returnData = null;

            string command = message["Command"] as string;

            try
            {
                if (command == "init")
                {
                    Console.WriteLine("init command received!");

                    var deviceName = message["Value"] as string;

                    Console.WriteLine($"Device-Name: {deviceName}");

                    this.DeviceName = deviceName;

                    proc = CreateProcess(this);

                    // todo: remove
                    Console.WriteLine($"Process created in ThreadId {threadId}");

                    proc.Start();

                    Console.WriteLine("Initialization complete!");
                }
                else if (command == "Get-Devices")
                {
                    // todo: get list of pnp devices
                }
                else if (command == "Disable-Device")
                {
                    proc.execute(disableDevice(this.DeviceName));

                    returnData = getMessage("Result", value: "Ok");
                }
                else if (command == "Enable-Device")
                {
                    proc.execute(enableDevice(this.DeviceName));

                    returnData = getMessage("Result", value: "Ok");

                }
                else if (command == "Get-Name")
                {
                    returnData = getMessage("Result", value: "ShellHost");
                }
                else if (command == "Exit") {
                    int exitCode = (int)message["Value"];
                    Environment.Exit(exitCode);
                }
                else
                {
                    Console.WriteLine($"Unrecognized command - \"{command}\"!");
                }
                
            }
            catch (Exception e)
            {
                returnData = getMessage("Result", value: "Error");

                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            try
            {
                await args.Request.SendResponseAsync(returnData);
            }
            catch (Exception e)
            {
                // Your exception handling code here.
            }
            finally
            {
                // Complete the deferral so that the platform knows that we're done responding to the app service call.
                // Note for error handling: this must be called even if SendResponseAsync() throws an exception.
                //Thread.SetData(Thread.GetNamedDataSlot("Globals"), globals);
                //Console.WriteLine($"Added {globals.ToString()} to {threadId}"); // todo: remove
                messageDeferral.Complete();
            }
        }

        private ValueSet getMessage(string type, string command = null, object value = null, object args = null)
        {
            var msg = new ValueSet();

            msg.Add("Type", type);
            msg.Add("Command", command);
            msg.Add("Value", value);
            msg.Add("Args", args);

            return msg;
        }

        private Process CreateProcess(Globals globals)
        {
            Process proc = new Process();

            proc.StartInfo.FileName = "powershell.exe";
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = false;
            proc.StartInfo.UseShellExecute = false;

            globals.Process = proc;

            return proc;
        }

        private async void init(Globals globals)
        {
            AppServiceConnection appService = new AppServiceConnection();
            appService.AppServiceName = "ShellHostService";
            appService.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;

            var status = await openAppServiceConnection(appService, globals);

            if (status == AppServiceConnectionStatus.Success)
            {
                appService.RequestReceived += AppService_RequestReceived;

                try
                {
                    appService.SendMessageAsync(getMessage("Command", "Relay-Command", "init"));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        public async Task<AppServiceConnection> openNewConnection()
        {
            AppServiceConnection appService = new AppServiceConnection();
            appService.AppServiceName = "ShellHostService";
            appService.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;

            var status = await appService.OpenAsync();

            return appService;
        }
    }
}
