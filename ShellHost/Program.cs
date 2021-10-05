using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace ShellHost
{
    class Program
    {
        private static string setExecutionPolicy(string level) {
            return $"Set-ExecutionPolicy {level}";
        }

        private static string getDevice(string name) {
            return $"Get-PnpDevice | where {{$_.friendlyname -like {name}}}";
        }

        private static string disableDevice(string deviceName)
        {
            return $"Get-PnpDevice | where {{$_.friendlyname -like {deviceName}}} | Disable-PnpDevice -Confirm:$false";
        }

        private static string enableDevice(string deviceName) {
            return $"Get-PnpDevice | where {{$_.friendlyname -like {deviceName}}} | Enable-PnpDevice -Confirm:$false";
        }

        private static async Task<AppServiceConnectionStatus> openAppServiceConnection(AppServiceConnection appService) {
            Console.WriteLine("Establishing connection to app service...");

            var status = await appService.OpenAsync();

            if (status == AppServiceConnectionStatus.Success)
            {
                Console.WriteLine("Connection established!");
            }
            else
            {
                Console.WriteLine("Failed to establish connection!");
            }

            return status;
        }

        private static async void AppService_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var messageDeferral = args.GetDeferral();

            try
            {
                
            }
            catch (Exception e)
            {
                // Your exception handling code here.
            }
            finally
            {
                messageDeferral.Complete();
            }
        }

        private static ValueSet getMessage(string type, string command = null, object value = null, object args = null)
        {
            var msg = new ValueSet();

            msg.Add("Type", type);
            msg.Add("Command", command);
            msg.Add("Value", value);
            msg.Add("Args", args);

            return msg;
        }

        static void Main(string[] args)
        {
            try
            {
                Console.SetWindowSize(1, 1);
                Console.CursorVisible = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            Globals globals = new Globals();

            Console.ReadKey();
        }
    }
}
