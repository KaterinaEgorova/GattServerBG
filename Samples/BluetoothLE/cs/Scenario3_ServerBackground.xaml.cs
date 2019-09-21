//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.UserDataTasks.DataProvider;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.Background;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using SDKTemplate.Services;

namespace SDKTemplate
{
    // This scenario declares support for a calculator service. 
    // Remote clients (including this sample on another machine) can supply:
    // - Operands 1 and 2
    // - an operator (+,-,*,/)
    // and get a result

    public sealed partial class Scenario3_ServerBackground : Page
    {
        private MainPage rootPage = MainPage.Current;

        //GattServiceProvider serviceProvider;

        private GattLocalCharacteristic op1Characteristic;
        //private int operand1Received = 0;

        private GattLocalCharacteristic op2Characteristic;
        //private int operand2Received = 0;

        private GattLocalCharacteristic operatorCharacteristic;
        //private CalculatorOperators operatorReceived = 0;

        private GattLocalCharacteristic resultCharacteristic;
        private int resultVal = 0;

        private bool peripheralSupported;
        private GattServiceProviderTrigger serviceProviderTrigger;
        private const string BTaskEntryPoint = "Tasks.GattServerTask";
        private const string TaskName = "GattServerTask";
        string BLAdvTaskName= "TaskMediaPlay.TileSongsTask";
        string BLAdvBTaskEntryPoint = "TaskMediaPlay.TileSongsTask";

        private const string TimerTaskEntryPoint = "Tasks.TimerTask";
        private const string TimerTaskName = "Tasks.TimerTask";

        MediaPlayer Player => PlaybackService.Instance.Player;

        #region UI Code
        public Scenario3_ServerBackground()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            peripheralSupported = await CheckPeripheralRoleSupportAsync();
            if (peripheralSupported)
            {
                ServerPanel.Visibility = Visibility.Visible;
            }
            else
            {
                PeripheralWarning.Visibility = Visibility.Visible;
            }
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                task.Value.Unregister(true);
                rootPage.NotifyUser($"{task.Value.Name} Unregistered", NotifyType.StatusMessage);
            }

            Player.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/TILE_113_Find.mp3"));
            Player.SystemMediaTransportControls.IsPlayEnabled = true;
            Player.SystemMediaTransportControls.ButtonPressed += SystemMediaTransportControls_ButtonPressed;
            /*TogglePlayPause();*/
        }

        private void SystemMediaTransportControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            TogglePlayPause();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                task.Value.Unregister(true);
                rootPage.NotifyUser($"{task.Value.Name} Unregistered", NotifyType.StatusMessage);
            }
            
            /*if (serviceProvider != null)
            {
                if (serviceProvider.AdvertisementStatus != GattServiceProviderAdvertisementStatus.Stopped)
                {
                    serviceProvider.StopAdvertising();
                }
                serviceProvider = null;
            }*/
        }

        private async void PublishButton_ClickAsync()
        {
            // Server not initialized yet - initialize it and start publishing
            if (serviceProviderTrigger == null)
            {
                var serviceStarted = await ServiceProviderInitAsync();
                if (serviceStarted)
                {
                    rootPage.NotifyUser("Service successfully started", NotifyType.StatusMessage);
                    PublishButton.Content = "Stop Service";
                }
                else
                {
                    rootPage.NotifyUser("Service not started", NotifyType.ErrorMessage);
                }

            }
            else
            {
                // BT_Code: Stops advertising support for custom GATT Service 
                /*serviceProvider.StopAdvertising();
                serviceProvider = null;*/
                var task = BackgroundTaskRegistration.AllTasks.First(x => x.Value.Name == TaskName);
                task.Value.Unregister(true);
                PublishButton.Content = "Start Service";
            }
        }

        private async void UpdateUX()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                switch (Calculator.OperatorReceived)
                {
                    case SDKTemplate.Calculator.CalculatorOperators.Add:
                        OperationText.Text = "+";
                        break;
                    case SDKTemplate.Calculator.CalculatorOperators.Subtract:
                        OperationText.Text = "-";
                        break;
                    case SDKTemplate.Calculator.CalculatorOperators.Multiply:
                        OperationText.Text = "*";
                        break;
                    case SDKTemplate.Calculator.CalculatorOperators.Divide:
                        OperationText.Text = "/";
                        break;
                    default:
                        OperationText.Text = "INV";
                        break;
                }
                Operand1Text.Text = Calculator.Operand1Received.ToString();
                Operand2Text.Text = Calculator.Operand2Received.ToString();
                try
                {
                    resultVal = Calculator.ComputeResult();
                }
                catch (Exception ex)
                {
                    rootPage.NotifyUser(ex.Message, NotifyType.ErrorMessage);
                }

                NotifyClientDevices(resultVal);
                ResultText.Text = resultVal.ToString();
            });
        }
        #endregion

        private async Task<bool> CheckPeripheralRoleSupportAsync()
        {
            // BT_Code: New for Creator's Update - Bluetooth adapter has properties of the local BT radio.
            var localAdapter = await BluetoothAdapter.GetDefaultAsync();

            if (localAdapter != null)
            {
                return localAdapter.IsPeripheralRoleSupported;
            }
            else
            {
                // Bluetooth is not turned on 
                return false;
            }
        }

        /// <summary>
        /// Uses the relevant Service/Characteristic UUIDs to initialize, hook up event handlers and start a service on the local system.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ServiceProviderInitAsync()
        {
            var requestStatus =
                await Windows.ApplicationModel.Background.BackgroundExecutionManager.RequestAccessAsync();
            if (requestStatus == BackgroundAccessStatus.Denied ||
                requestStatus == BackgroundAccessStatus.DeniedByUser ||
                requestStatus == BackgroundAccessStatus.DeniedBySystemPolicy)
            {
                rootPage.NotifyUser("BackgroundTask cannot to be registered", NotifyType.ErrorMessage);
                return false;
            }

            // Depending on the value of requestStatus, provide an appropriate response
            // such as notifying the user which functionality won't work as expected

            //await RegisterBlAdvertisementTask();
            await RegisterAppTriggeredTask();
            await RegisterTimeTriggeredTask();

            // BT_Code: Initialize and starting a custom GATT Service using GattServiceProvider.
            //GattServiceProviderResult serviceResult = await GattServiceProvider.CreateAsync(Constants.CalcServiceUuid);
            var serviceResultTriggerResult
                = await GattServiceProviderTrigger.CreateAsync(TaskName, Constants.CalcServiceUuid);
            if (serviceResultTriggerResult.Error == BluetoothError.Success)
            {
                serviceProviderTrigger = serviceResultTriggerResult.Trigger;
            }
            else
            {
                rootPage.NotifyUser($"Could not create service provider: {serviceResultTriggerResult.Error}",
                    NotifyType.ErrorMessage);
                return false;
            }

            GattLocalCharacteristicResult result =
                await serviceProviderTrigger.Service.CreateCharacteristicAsync(Constants.Op1CharacteristicUuid,
                    Constants.gattOperand1Parameters);
            if (result.Error == BluetoothError.Success)
            {
                op1Characteristic = result.Characteristic;
            }
            else
            {
                rootPage.NotifyUser($"Could not create operand1 characteristic: {result.Error}",
                    NotifyType.ErrorMessage);
                return false;
            }

            GattServiceProviderAdvertisingParameters advParameters = new GattServiceProviderAdvertisingParameters
            {
                // IsConnectable determines whether a call to publish will attempt to start advertising and 
                // put the service UUID in the ADV packet (best effort)
                IsConnectable = peripheralSupported,

                // IsDiscoverable determines whether a remote device can query the local device for support 
                // of this service
                IsDiscoverable = true
            };
            //serviceProviderTrigger.AdvertisementStatusChanged += ServiceProvider_AdvertisementStatusChanged;

            //serviceProvider.StartAdvertising(advParameters);
            //var triggerResult = await GattServiceProviderTrigger.CreateAsync("ttt", Constants.CalcServiceUuid);
            serviceProviderTrigger.AdvertisingParameters = advParameters;
            
            RegistarGattServerBgTaskTrigger();

            return true;
        }
        private async Task<bool> RegisterAppTriggeredTask()
        {
            try
            {
                var timeTrigger = new ApplicationTrigger();
                var builder = new BackgroundTaskBuilder();

                builder.Name = TimerTaskName;
                builder.TaskEntryPoint = TimerTaskEntryPoint;
                builder.SetTrigger(timeTrigger);
                var result = builder.Register();
                result.Completed += (sender, args) => { Debug.WriteLine("TimerTask completed..."); };
                var r =  await timeTrigger.RequestAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return true;
        }
        private async Task<bool> RegisterTimeTriggeredTask()
        {
            try
            {
                var timeTrigger = new TimeTrigger(15, false);
                var builder = new BackgroundTaskBuilder();

                builder.Name = TimerTaskName;
                builder.TaskEntryPoint = TimerTaskEntryPoint;
                builder.SetTrigger(timeTrigger);
                var result = builder.Register();
                result.Completed += (sender, args) => { Debug.WriteLine("TimerTask completed..."); };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return true;
        }
        
        private void Result_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
           Debug.WriteLine("Advertised!");
        }

        private void RegistarGattServerBgTaskTrigger()
        {
            var builder = new BackgroundTaskBuilder();

            builder.Name = TaskName;
            builder.TaskEntryPoint = BTaskEntryPoint;
            builder.SetTrigger(serviceProviderTrigger);
            try
            {
                BackgroundTaskRegistration t = builder.Register();

                AttachProgressAndCompletedHandlers(t);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Attach progress and completed handers to a background task.
        /// </summary>
        /// <param name="task">The task to attach progress and completed handlers to.</param>
        private void AttachProgressAndCompletedHandlers(IBackgroundTaskRegistration task)
        {
            //task.Progress += new BackgroundTaskProgressEventHandler(OnProgress);
            task.Completed += new BackgroundTaskCompletedEventHandler(OnCompleted);
        }

        private void OnCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            //var settings = ApplicationData.Current.LocalSettings;
            //var v = settings.Values["Values"]?.ToString();
            UpdateUX();
            rootPage.NotifyUser($"bg task is completed. Op1 ever received?: {ApplicationData.Current.LocalSettings.Values["Op1ReceivedEver"]} Result: {ApplicationData.Current.LocalSettings.Values["Op1ReceivedEver"]}; Received: {Calculator.Operand1Received.ToString()} {Calculator.OperatorReceived} {Calculator.Operand2Received.ToString()}; Result: {Calculator.ComputeResult()}", NotifyType.StatusMessage);
            Debug.WriteLine($"bg task is completed. Op1 ever received?: {ApplicationData.Current.LocalSettings.Values["Op1ReceivedEver"]} Result: {ApplicationData.Current.LocalSettings.Values["Op1ReceivedEver"]}; Received: {Calculator.Operand1Received.ToString()} {Calculator.OperatorReceived} {Calculator.Operand2Received.ToString()}; Result: {Calculator.ComputeResult()}");
/*            Player.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/TILE_113_Find.mp3"));*/
            //TogglePlayPause();
        }

        public void TogglePlayPause()
        {
            switch (Player.PlaybackSession.PlaybackState)
            {
                case MediaPlaybackState.Playing:
                    Player.Pause();
                    break;
                case MediaPlaybackState.Paused:
                    Player.Play();
                    break;
            }
        }

        private async void NotifyClientDevices(int computedValue)
        {
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteInt32(computedValue);

            // BT_Code: Returns a collection of all clients that the notification was attempted and the result.
            IReadOnlyList<GattClientNotificationResult> results = await resultCharacteristic.NotifyValueAsync(writer.DetachBuffer());

            rootPage.NotifyUser($"Sent value {computedValue} to clients.", NotifyType.StatusMessage);
            foreach (var result in results)
            {
                // An application can iterate through each registered client that was notified and retrieve the results:
                //
                // result.SubscribedClient: The details on the remote client.
                // result.Status: The GattCommunicationStatus
                // result.ProtocolError: iff Status == GattCommunicationStatus.ProtocolError
            }
        }
    }
}
