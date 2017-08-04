using BLEReader;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace BLEReader
{
    class Program
    {
        String version = "0.1";
        String app_name = "Arduino OTA_DFU for Nordic nRF5x";        
        bool scanonly, devicefound;
        String given_device_address;
        private static GattDeviceService service { get; set; }
        
        

        static void Main(string[] args)
        {
            Program program = new Program();
            program.discovery();

            //program.connectDirectDevice();
        }

        private void connectDirectDevice() {
            String bluetoothAddress = "";
            var bleDevice = BluetoothLEDevice.FromIdAsync(bluetoothAddress);

    }



        /// <summary>
        /// Discovery BLE devices in range
        /// </summary>
        private void discovery()
        {
            Console.WriteLine("Discovering devices...");
            // Query for extra properties you want returned
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            DeviceWatcher deviceWatcher =
                        DeviceInformation.CreateWatcher(
                                BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
                                requestedProperties,
                                DeviceInformationKind.AssociationEndpoint);
            deviceWatcher.Added += DeviceWatcher_Added;
           
            // Start the watcher.
            deviceWatcher.Start();

            Console.ReadLine();

        }




        /// <summary>
        /// Event called when a new device is discovered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="device"></param>
        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation device)
        {
            Console.WriteLine("[DeviceWatcher_Added]" + device.Name + " ID:" + device.Id);
            String deviceAddress = device.Id.Split('-')[1];
            Console.WriteLine("Found Device name:[" + device.Name + "] Device address:[" + deviceAddress + "]");
            
            scanonly = false;
            given_device_address = "cc:32:24:e9:13:1a";
            if (!scanonly && given_device_address == deviceAddress)
            //TODO Only for test
            //if (!scanonly && true)
            {
                //this.devicefound = true;
                try
                {
                    //DFUService dfs =DFUService.Instance;
                    UARTService.Instance.connectToDeviceAsync(device);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }
        }        
    }

    public class UARTService
    {

        #region Properties
        private static GattDeviceService service { get; set; }
        private GattCharacteristic controlPoint { get; set; }
        private GattDescriptor cccd { get; set; }
        private bool IsServiceChanged = false;
        public bool IsServiceInitialized { get; set; }

        //UUID to identify the DFU service
        public static String UARTService_UUID       = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
        public static String UARTService_UUID_TX    = "6e400002-b5a3-f393-e0a9-e50e24dcca9e";
        public static String UARTService_UUID_RX    = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";


        public static String CCCD = "00002902-0000-1000-8000-00805f9b34fb";

        #endregion

        private static UARTService instance = new UARTService();

        public static UARTService Instance
        {
            get { return instance; }
        }

        public GattDeviceService Service
        {
            get { return service; }
        }

        async void ConnectDevice(DeviceInformation deviceInfo)
        {
            // Note: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
            BluetoothLEDevice bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
            // ...
        }
        /// <summary>
        /// Connect to the device, check if there's the DFU Service and start the firmware update
        /// </summary>
        /// <param name="device">The device discovered</param>
        /// <returns></returns>
        public async void connectToDeviceAsync(DeviceInformation device)
        {
            try
            {
                var deviceAddress = "N/A";
                if (device.Id.Contains("-"))
                    deviceAddress = device.Id.Split('-')[1];
                
                Console.WriteLine("Connecting to:" + deviceAddress + "...");

                BluetoothLEDevice bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(device.Id);
                Console.WriteLine("Name:" + bluetoothLeDevice);

                //Perform the connection to the device
                var result = await bluetoothLeDevice.GetGattServicesAsync();
                if (result.Status == GattCommunicationStatus.Success)
                {
                    Console.WriteLine("Device " + deviceAddress + " connected. Updating firmware...");
                    //Scan the available services
                    var services = result.Services;
                    foreach (var service_ in services)
                    {
                        Console.WriteLine("Service " + service_.Uuid);
                        //If DFUService found...
                        if (service_.Uuid == new Guid(UARTService.UARTService_UUID))
                        { //NRF52 DFU Service
                            Console.WriteLine("UART Service found");
                            service = service_;
                            //Scan the available characteristics
                            GattCharacteristicsResult result_ = await service.GetCharacteristicsAsync();
                            if (result_.Status == GattCommunicationStatus.Success)
                            {
                                var characteristics = result_.Characteristics;
                                foreach (var characteristic in characteristics)
                                {
                                    Console.WriteLine("Char " + characteristic.Uuid + "-");
                                    Console.WriteLine("Handle " + characteristic.AttributeHandle + "-");
                                    Console.WriteLine("Handle " + characteristic.UserDescription + "-");
                                    
                                    if (characteristic.Uuid.ToString() == UARTService_UUID_RX)
                                    {
                                        Console.WriteLine("UARTService_UUID_RX found " + characteristic.UserDescription);
                                        this.controlPoint = characteristic;
                                    }

                                    if (characteristic.Uuid.ToString() == UARTService_UUID_TX)
                                    {
                                        Console.WriteLine("UARTService_UUID_TX found " + characteristic.UserDescription);
                                        this.controlPoint = characteristic;
                                    }


                                    GattDescriptorsResult result2_ = await characteristic.GetDescriptorsAsync();
                                    var descriptors = result2_.Descriptors;
                                    foreach (var descriptor in descriptors)
                                    {
                                        Console.WriteLine("Descr " + descriptor.Uuid + "-");
                                        Console.WriteLine("Handle " + descriptor.AttributeHandle + "-");
                                        if (descriptor.Uuid.ToString() == CCCD)
                                        {
                                            Console.WriteLine("CCC found " + characteristic.UserDescription);
                                            this.cccd = descriptor;
                                        }
                                        

                                    }
                                    

                                }     
                            }

                            this.startReaderAsync();
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Status error: " + result.Status.ToString() + " need to restarte the device");

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Accessing2 your device failed." + Environment.NewLine + e.Message + "\n" + e.StackTrace);
            }
        }

        private async void startReaderAsync()
        {
            GattCharacteristicProperties properties = this.controlPoint.CharacteristicProperties;

            if (properties.HasFlag(GattCharacteristicProperties.Read))
            {
                Console.WriteLine("This characteristic supports reading from it.");
            }
            if (properties.HasFlag(GattCharacteristicProperties.Write))
            {
                Console.WriteLine("This characteristic supports reading from it.");
            }
            if (properties.HasFlag(GattCharacteristicProperties.Notify))
            {
                Console.WriteLine("This characteristic supports subscribing to notifications.");
            }

            GattCommunicationStatus status = await this.controlPoint.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.Notify);
            if (status == GattCommunicationStatus.Success)
            {
                Console.WriteLine("Success");
                this.controlPoint.ValueChanged += Characteristic_ValueChanged;
                // ... 

                void Characteristic_ValueChanged(GattCharacteristic sender,
                                                    GattValueChangedEventArgs args)
                {
                    Console.WriteLine("Characteristic_ValueChanged");

                    // An Indicate or Notify reported that the value has changed.
                    var reader = DataReader.FromBuffer(args.CharacteristicValue);
                    Console.WriteLine("Read:" + reader.ReadString(10));
}
            }
        }
    }
}
