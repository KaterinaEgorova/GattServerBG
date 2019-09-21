using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Background;
using Windows.Foundation.Collections;
using Windows.Graphics.Printing.OptionDetails;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace TaskMediaPlay
{
    public sealed class TileSongsTask : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;
        
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            Debug.WriteLine("BG Task BLadv is triggered");
            var details = taskInstance.TriggerDetails as BluetoothLEAdvertisementPublisherTriggerDetails;
            Debug.WriteLine(details.Status);
            deferral.Complete();
        }
    }
}
