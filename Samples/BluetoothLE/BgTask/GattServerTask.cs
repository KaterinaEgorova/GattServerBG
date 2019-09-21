using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Background;
using Windows.Storage;
using Windows;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace Tasks
{
    public sealed class GattServerTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;
        private GattLocalService service;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            var details = taskInstance.TriggerDetails as GattServiceProviderTriggerDetails;
            
            service = details.Connection.Service;
            Debug.WriteLine($"task is triggered. TriggerId: {details?.Connection.TriggerId}");
            Debug.WriteLine("");
            var settings = ApplicationData.Current.LocalSettings;
            if (settings.Values["BackgroundNotes"] != null)
            {
                settings.Values.Remove("BackgroundNotes");
            }

            settings.Values["BackgroundNotes2"] = "we started Bg task....";

            ApplicationData.Current.LocalSettings.Values.Remove("Op1ReceivedEver");

            {

                foreach (var gattLocalCharacteristic in service.Characteristics)
                {
                    Debug.WriteLine($"user descr: {gattLocalCharacteristic.UserDescription}");

                    if (gattLocalCharacteristic.UserDescription == "Operand 1 Characteristic")
                    {
                            gattLocalCharacteristic.WriteRequested += Op1Characteristic_WriteRequestedAsync;
                    }
                }
            }
            details.Connection.Start();
            
        }

        private void PlayMusic()
        {
            ToastHelper.PopToast("SUCCESS", "This scenario successfully completed. Please mark it as passed.");
            _deferral.Complete();
        }

        private async void Op1Characteristic_WriteRequestedAsync(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            // BT_Code: Processing a write request.
            using (args.GetDeferral())
            {
                PlayMusic();
            }
            
        }
    }
}
    

