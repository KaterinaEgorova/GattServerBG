using System;
using System.Diagnostics;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage;
using Windows.Storage.Streams;

namespace SDKTemplate
{
    public static class Calculator
    {
        public enum CalculatorCharacteristics
        {
            Operand1 = 1,
            Operand2 = 2,
            Operator = 3
        }

        public enum CalculatorOperators
        {
            Add = 1,
            Subtract = 2,
            Multiply = 3,
            Divide = 4
        }

        public static int Operand1Received
        {
            get
            {
                {
                    //return 3;
                    try
                    {
                        if (ApplicationData.Current.LocalSettings.Values["Operand1Received"] == null)
                        {
                            return -25;
                        }

                        return Int32.Parse(ApplicationData.Current.LocalSettings.Values["Operand1Received"].ToString());
                    }
                    catch
                    {
                        return 0;
                    }
                }
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values["Operand1Received"] = value;
            }
        }

        public static int Operand2Received
        {
            get
            {
                {
                    //return 2;
                    try
                    {
                        if (ApplicationData.Current.LocalSettings.Values["Operand2Received"] == null)
                        {
                            return -25;
                        }

                        return Int32.Parse(ApplicationData.Current.LocalSettings.Values["Operand2Received"].ToString());
                    }
                    catch
                    {
                        return 0;
                    }
                }
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values["Operand2Received"] = value;
            }
        }

        public static CalculatorOperators OperatorReceived
        {
            get
            {

                //return CalculatorOperators.Add;
                try
                {
                    if (ApplicationData.Current.LocalSettings.Values["OperatorReceived"] == null)
                    {
                        return CalculatorOperators.Add;
                    }

                    return (CalculatorOperators) Enum.Parse(typeof(CalculatorOperators),
                        ApplicationData.Current.LocalSettings.Values["OperatorReceived"].ToString());
                }
                catch
                {
                    return CalculatorOperators.Add;
                }
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values["OperatorReceived"] = value;
            }
        }

        public static int ComputeResult()
        {
            Int32 computedValue = 0;
            switch (OperatorReceived)
            {
                case CalculatorOperators.Add:
                    computedValue = Operand1Received + Operand2Received;
                    break;
                case CalculatorOperators.Subtract:
                    computedValue = Operand1Received - Operand2Received;
                    break;
                case CalculatorOperators.Multiply:
                    computedValue = Operand1Received * Operand2Received;
                    break;
                case CalculatorOperators.Divide:
                    if (Operand2Received == 0 || (Operand1Received == -0x80000000 && Operand2Received == -1))
                    {
                        throw new Exception("Division overflow");
                    }
                    else
                    {
                        computedValue = Operand1Received / Operand2Received;
                    }
                    break;
                default:
                    throw new Exception("Invalid Operator");
            }
           // var settings = ApplicationData.Current.LocalSettings;
            //var mtValues = $"Operand1: {Operand1Received}; Operand2 ${Operand2Received}; Operator: {OperatorReceived}";
            //settings.Values["Values"] = mtValues;
            return computedValue;
        }

        

        /// <summary>
        /// BT_Code: Processing a write request.Takes in a GATT Write request and updates UX based on opcode.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="opCode">Operand (1 or 2) and Operator (3)</param>
        public static void ProcessWriteCharacteristic(GattWriteRequest request, CalculatorCharacteristics opCode)
        {
            Debug.WriteLine("processing request....");
            if (request.Value.Length != 4)
            {
                // Input is the wrong length. Respond with a protocol error if requested.
                if (request.Option == GattWriteOption.WriteWithResponse)
                {
                    request.RespondWithProtocolError(GattProtocolError.InvalidAttributeValueLength);
                }
                return;
            }

            var reader = DataReader.FromBuffer(request.Value);
            reader.ByteOrder = ByteOrder.LittleEndian;
            int val = reader.ReadInt32();
            
            switch (opCode)
            {
                case CalculatorCharacteristics.Operand1:
                    Operand1Received = val;
                    break;
                case CalculatorCharacteristics.Operand2:
                    Operand2Received = val;
                    break;
                case CalculatorCharacteristics.Operator:
                    if (!Enum.IsDefined(typeof(CalculatorOperators), val))
                    {
                        if (request.Option == GattWriteOption.WriteWithResponse)
                        {
                            request.RespondWithProtocolError(GattProtocolError.InvalidPdu);
                        }
                        return;
                    }
                    OperatorReceived = (CalculatorOperators)val;
                    break;
            }
            // Complete the request if needed
            if (request.Option == GattWriteOption.WriteWithResponse)
            {
                request.Respond();
            }
        }

        public static async void Op1Characteristic_WriteRequestedAsync(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            // BT_Code: Processing a write request.
            using (args.GetDeferral())
            {
                ApplicationData.Current.LocalSettings.Values["Op1ReceivedEver"] = "yes";
                // Get the request information.  This requires device access before an app can access the device's request.
                GattWriteRequest request = await args.GetRequestAsync();
                if (request == null)
                {
                    // No access allowed to the device.  Application should indicate this to the user.
                    return;
                }
                ProcessWriteCharacteristic(request, SDKTemplate.Calculator.CalculatorCharacteristics.Operand1);
                //UpdateUX();
            }
        }

        public static async void Op2Characteristic_WriteRequestedAsync(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            using (args.GetDeferral())
            {
                // Get the request information.  This requires device access before an app can access the device's request.
                GattWriteRequest request = await args.GetRequestAsync();
                if (request == null)
                {
                    // No access allowed to the device.  Application should indicate this to the user.
                    return;
                }

                Calculator.ProcessWriteCharacteristic(request, SDKTemplate.Calculator.CalculatorCharacteristics.Operand2);
                //UpdateUX();
            }
        }

        public static async void OperatorCharacteristic_WriteRequestedAsync(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            using (args.GetDeferral())
            {
                // Get the request information.  This requires device access before an app can access the device's request.
                GattWriteRequest request = await args.GetRequestAsync();
                if (request == null)
                {
                    // No access allowed to the device.  Application should indicate this to the user.
                    return;
                }

                Calculator.ProcessWriteCharacteristic(request, SDKTemplate.Calculator.CalculatorCharacteristics.Operator);
                //UpdateUX();
            }
        }

        public static void ResultCharacteristic_SubscribedClientsChanged(GattLocalCharacteristic sender, object args)
        {
            //rootPage.NotifyUser($"New device subscribed. New subscribed count: {sender.SubscribedClients.Count}", NotifyType.StatusMessage);
        }

        public static void ServiceProvider_AdvertisementStatusChanged(GattServiceProvider sender, GattServiceProviderAdvertisementStatusChangedEventArgs args)
        {
            // Created - The default state of the advertisement, before the service is published for the first time.
            // Stopped - Indicates that the application has canceled the service publication and its advertisement.
            // Started - Indicates that the system was successfully able to issue the advertisement request.
            // Aborted - Indicates that the system was unable to submit the advertisement request, or it was canceled due to resource contention.

            //rootPage.NotifyUser($"New Advertisement Status: {sender.AdvertisementStatus}", NotifyType.StatusMessage);
        }

        public static async void ResultCharacteristic_ReadRequestedAsync(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
        {
            // BT_Code: Process a read request. 
            using (args.GetDeferral())
            {
                // Get the request information.  This requires device access before an app can access the device's request. 
                GattReadRequest request = await args.GetRequestAsync();
                if (request == null)
                {
                    // No access allowed to the device.  Application should indicate this to the user.
                    //rootPage.NotifyUser("Access to device not allowed", NotifyType.ErrorMessage);
                    return;
                }

                var writer = new DataWriter();
                writer.ByteOrder = ByteOrder.LittleEndian;
                writer.WriteInt32(ComputeResult());

                // Can get details about the request such as the size and offset, as well as monitor the state to see if it has been completed/cancelled externally.
                // request.Offset
                // request.Length
                // request.State
                // request.StateChanged += <Handler>

                // Gatt code to handle the response
                request.RespondWithValue(writer.DetachBuffer());
            }
        }

    }
}