using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace OnScreenDeviceManager
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string deviceName = "\"HID-compliant touch screen\"";
        private AppServiceConnection appService = null;
        private bool FullTrustProcessLaunched = false;

        public MainPage()
        {
            this.InitializeComponent();
            init();
            
        }

        private async void init() {
            await FullTrustProcessLauncher.LaunchFullTrustProcessForAppAsync("App");
            
            FullTrustProcessLaunched = true; // todo: check status of fulltrustprocess
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
        }

        private static async Task<AppServiceConnectionStatus> openAppServiceConnection(AppServiceConnection appService)
        {
            var status = await appService.OpenAsync();

            if (status != AppServiceConnectionStatus.Success)
            {
                openAppServiceConnection(appService);
            }

            return status;
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            if (this.appService == null) {
                this.appService = ((App)Application.Current).appService;
            }

            if (FullTrustProcessLaunched)
            {
                ValueSet msg;
                var deviceEnabled = ((App)Application.Current).deviceEnabled;

                if (deviceEnabled) {
                    msg = getMessage("Command", "Disable-Device", deviceName);
                } else
                {
                    msg = getMessage("Command", "Enable-Device", deviceName);
                }

                appService.SendMessageAsync(msg);

                deviceEnabled = deviceEnabled ? false : true;
                ((App)Application.Current).deviceEnabled = deviceEnabled;
            }

            // todo: change button color on click
            // Button1.Foreground = new SolidColorBrush(Windows.UI.Colors.Beige);
        }

        private ValueSet getMessage(string type, string command = null, object value = null) {
            var msg = new ValueSet();
            
            msg.Add("Type", type);
            msg.Add("Command", command);
            msg.Add("Value", value);

            return msg;
        }

        private async void AppService_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var messageDeferral = args.GetDeferral();

            ValueSet message = args.Request.Message;
            ValueSet returnData = null;

            string command = message["Command"] as string;

            if (command == "Get-DeviceName")
            {
                returnData = getMessage("Result", value:deviceName);
            }
            else if (command == "Get-Name")
            {
                returnData = getMessage("Result", value: "OnScreenDeviceManager");
            }
            else
            {
                returnData = getMessage("Result", $"Unrecognized command - \"{command}\"!");
            }

            await args.Request.SendResponseAsync(returnData);

            messageDeferral.Complete();
        }
    }
}
