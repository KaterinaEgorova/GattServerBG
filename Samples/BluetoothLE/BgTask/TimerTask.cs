using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace Tasks
{
    public sealed class TimerTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            await RegisterBlAdvertisementTask();
            _deferral.Complete();
        }

        private async Task<bool> RegisterBlAdvertisementTask()
        {
            try
            {
                var blAdvTrigger = new BluetoothLEAdvertisementPublisherTrigger();
                DataWriter writer = new DataWriter();

                ushort suiid = 0xFEED;

                writer.WriteBytes(BitConverter.GetBytes(suiid));
                //UInt16 someValues = 0x1234;
                var advData = (ushort)DateTime.Now.Minute;
                writer.WriteUInt16(advData);

                var ds = blAdvTrigger.Advertisement.GetSectionsByType(BluetoothLEAdvertisementDataTypes.ServiceData16BitUuids).FirstOrDefault();
                if (ds != null)
                {
                    ds.Data = writer.DetachBuffer();
                }
                else
                {
                    blAdvTrigger.Advertisement.DataSections.Insert(0, new BluetoothLEAdvertisementDataSection(BluetoothLEAdvertisementDataTypes.ServiceData16BitUuids, writer.DetachBuffer()));
                }

                string BLAdvTaskName = "TaskMediaPlay.TileSongsTask";
                string BLAdvBTaskEntryPoint = "TaskMediaPlay.TileSongsTask";


                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == BLAdvTaskName)
                    {
                        task.Value.Unregister(true);
                        Debug.WriteLine($"{task.Value.Name} Unregistered");
                    }
                }

                var builder = new BackgroundTaskBuilder();
                builder.Name = BLAdvTaskName;
                builder.TaskEntryPoint = BLAdvBTaskEntryPoint;
                builder.SetTrigger(blAdvTrigger);
                var result = builder.Register();
                Debug.WriteLine("Adv Task registered with modified service data");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                return false;
            }

            return true;
        }

    }
}
