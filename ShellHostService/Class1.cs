using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace ShellHostService
{
    public sealed class IPC : IBackgroundTask {
        private BackgroundTaskDeferral backgroundTaskDeferral;
        private List<ConnectionContainer> connections = new List<ConnectionContainer>();

        public void Run(IBackgroundTaskInstance taskInstance) {
            // Get a deferral so that the service isn't terminated.
            this.backgroundTaskDeferral = taskInstance.GetDeferral();

            // Associate a cancellation handler with the background task.
            taskInstance.Canceled += OnTaskCanceled;

            // Retrieve the app service connection and set up a listener for incoming app service requests.
            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            AppServiceConnection appServiceconnection = details.AppServiceConnection;
            appServiceconnection.RequestReceived += OnRequestReceived;

            getCallerName(appServiceconnection, taskInstance);
        }

        private async void getCallerName(AppServiceConnection appServiceConnection, IBackgroundTaskInstance taskInstance) {
            var response = await appServiceConnection.SendMessageAsync(getMessage("Command", "Get-Name"));

            if (response.Status == AppServiceResponseStatus.Success && response.Message.Count > 0)
            {
                int x = response.Message.Count;
                var keys = response.Message.Keys;
                string type = response.Message["Type"] as string;
                string command = response.Message["Command"] as string;
                string name = response.Message["Value"] as string;

                connections.Add(new ConnectionContainer(name, appServiceConnection));
            }
            else {
                return;
            }
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args) {
            var messageDeferral = args.GetDeferral();

            ValueSet message = args.Request.Message;
            ValueSet returnData = null;

            string command = message["Command"] as string;

            if (command == "Get-DeviceName")
            {
                // todo: get device name from uwp
                string uwpName = "OnScreenDeviceManager";

                var result = connections.Where(item => item.Name == uwpName);

                AppServiceConnection uwpConnection = null;

                if (result.Count() > 0)
                {
                    uwpConnection = result.FirstOrDefault().Connection;
                }

                if (uwpConnection != null)
                {
                    var response = await uwpConnection.SendMessageAsync(getMessage("Command", "Get-DeviceName"));

                    if (response.Status == AppServiceResponseStatus.Success && response.Message.Count > 0)
                    {
                        string deviceName = response.Message["Value"] as string;

                        returnData = getMessage("Result", value:deviceName);
                    }
                }
                else
                {
                    // todo: wait for connection to be established and try again
                }
            }
            else {
                returnData = getMessage("Result", value: $"Unrecognized command - \"{command}\"!");
            }

            try
            {
                // Return the data to the caller.
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
                messageDeferral.Complete();
            }
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason) {
            if (this.backgroundTaskDeferral != null)
            {
                // Complete the service deferral.
                //this.backgroundTaskDeferral.Complete();
            }
        }

        private ValueSet getMessage(string type, string command = null, object value = null)
        {
            var msg = new ValueSet();

            msg.Add("Type", type);
            msg.Add("Command", command);
            msg.Add("Value", value);

            return msg;
        }
    }
}
