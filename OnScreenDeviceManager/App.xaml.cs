using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace OnScreenDeviceManager
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary> 
    sealed partial class App : Windows.UI.Xaml.Application
    {
        public int x { get; set; }
        public string deviceName = "\"HID-compliant touch screen\""; // todo: add to windows.storage
        public AppServiceConnection appService = null;
        public ApplicationExecutionState executionState;
        public bool deviceEnabled { get; set; } // todo: get device current state

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            try
            {
                int value = (int)localSettings.Values["deviceEnabled"];

                if (value == 1)
                {
                    this.deviceEnabled = true;
                }
                else
                {
                    this.deviceEnabled = false;
                }
            }
            catch (Exception e) {
                this.deviceEnabled = false;    
            }
        }

        public delegate void WindowClosedEventHandler(object sender, Windows.UI.Core.CoreWindowEventArgs e);

        public void OnClosed(object sender, Windows.UI.Core.CoreWindowEventArgs e) {
            // todo: handle event on window closed

            return;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Windows.UI.Xaml.WindowClosedEventHandler OnClosedHandler = OnClosed;
            Window.Current.Closed += OnClosed;

            Frame rootFrame = Window.Current.Content as Frame;
            this.executionState = e.PreviousExecutionState;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    
                    rootFrame.Navigate(typeof(MainPage), appService);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity

            this.appService.SendMessageAsync(getMessage("Command", "Exit", 0));

            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (deviceEnabled)
            {
                localSettings.Values["deviceEnabled"] = 1;
            }
            else {
                localSettings.Values["deviceEnabled"] = 0;
            }

            deferral.Complete();
        }

        private void onEnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            var deferral = e.ToString();
            //TODO: Save application state and stop any background activity
        }

        private void onLeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            var deferral = e.ToString();
            //TODO: Save application state and stop any background activity
        }

        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args) {
            base.OnBackgroundActivated(args);

            var taskInstance = args.TaskInstance;
            
            AppServiceTriggerDetails appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            
            var appServiceDeferral = taskInstance.GetDeferral();

            var appServiceConnection = appService.AppServiceConnection;

            var current = ((App)Application.Current);

            current.appService = appServiceConnection;

            current.appService.RequestReceived += AppService_RequestReceived;

            appServiceDeferral.Complete();
        }

        private async void AppService_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var messageDeferral = args.GetDeferral();
            var deviceName = ((App)Current).deviceName;

            ValueSet message = args.Request.Message;
            ValueSet returnData = null;

            string command = message["Command"] as string;

            if (command == "Relay-Command") {
                string _c = message["Value"] as string;

                if (_c == "init") {
                    returnData = getMessage("Command", _c, deviceName);
                } else
                {
                    returnData = getMessage("Command", _c);
                }

                var current = ((App)Application.Current);

                current.appService.SendMessageAsync(returnData);
            }
            else if (command == "Get-DeviceName")
            {
                returnData = getMessage("Result", value: deviceName);
            }
            else if (command == "Get-Name")
            {
                returnData = getMessage("Result", value: "OnScreenDeviceManager");
            }
            else
            {
                returnData = getMessage("Result", $"Unrecognized command - \"{command}\"!");
            }

            if (command != "Relay-Command")
            {
                await args.Request.SendResponseAsync(returnData);
            }

            messageDeferral.Complete();
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
    }
}
