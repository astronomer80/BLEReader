using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using System.ComponentModel;
using System.Text;

// Il modello di elemento per la pagina vuota è documentato all'indirizzo http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x410

namespace App_UWP_OTA_Dfu
{
        
    
    /// <summary>
    /// Pagina vuota che può essere usata autonomamente oppure per l'esplorazione all'interno di un frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        String textLog = "";
        String logfilename = "";
        private BackgroundWorker backgroundWorker1;


        public MainPage()
        {
            this.InitializeComponent();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.backgroundWorker1.ProgressChanged += WorkerThread_ProgressChanged;
            this.backgroundWorker1.DoWork += WorkerThread_DoWork;
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.WorkerSupportsCancellation = true;
            this.backgroundWorker1.RunWorkerCompleted += this.backgroundWorker1_RunWorkerCompleted;

            this.backgroundWorker1.RunWorkerAsync();
            String time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            this.logfilename = @"[" + time + "]_" + app_name + "_LOG.txt";
            log(app_name);
            discovery();

            //UARTService.Instance.connectToDeviceAsync3();
        }

        private void WorkerThread_DoWork(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("WorkerThread_DoWork");
        }

        private void WorkerThread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Debug.WriteLine("WorkerThread_ProgressChanged " + e.UserState.ToString());

        }

        private void textBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        

        async void log(String message)
        {
            Debug.WriteLine(message);
            try
            {
                this.textLog = this.textLog + "\n" + message;

                this.writeOnFile(message);
                

                while (this.backgroundWorker1.IsBusy) {
                    var b = 0;
                }
                this.backgroundWorker1.RunWorkerAsync();
                this.backgroundWorker1.ReportProgress(0, this.textLog);
            }
            catch (Exception e)
            {
                Debug.WriteLine("[log]" + e.Message + " " + e.StackTrace);
            }
        }

        private async void writeOnFile(String message)
        {
            try
            {
                // Create sample file; replace if exists.
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile sampleFile = await storageFolder.CreateFileAsync(logfilename, Windows.Storage.CreationCollisionOption.OpenIfExists);

                //await Windows.Storage.FileIO.WriteTextAsync(sampleFile, data);
                await Windows.Storage.FileIO.AppendTextAsync(sampleFile, message);
            }catch(Exception e)
            {
                Debug.WriteLine("[log]" + e.Message + " " + e.StackTrace);

            }

        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.textBox.Text = this.textLog;
            Debug.WriteLine("[backgroundWorker1_RunWorkerCompleted]");
        }





        String version = "0.1";
        String app_name = "Arduino OTA_DFU for Nordic nRF5x";
        bool scanonly, devicefound;
        //String given_device_address = "cc:32:24:e9:13:1a";
        String given_device_address = "e8:53:c7:3c:fc:e8";
        private static GattDeviceService service { get; set; }

        /// <summary>
        /// Discovery BLE devices in range
        /// </summary>
        private void discovery()
        {
            log("Discovering devices...");
            // Query for extra properties you want returned
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };
            
            DeviceWatcher deviceWatcher =
                        DeviceInformation.CreateWatcher(
                                BluetoothLEDevice.GetDeviceSelectorFromPairingState(true),
                                //BluetoothLEDevice.GetDeviceSelectorFromPairingState(true),
                                requestedProperties,
                                DeviceInformationKind.AssociationEndpoint);
            deviceWatcher.Added += DeviceWatcher_Added;

            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;

            // EnumerationCompleted and Stopped are optional to implement.
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            // Start the watcher.
            deviceWatcher.Start();
            
            /*
            DeviceWatcher deviceWatcher1 =
                        DeviceInformation.CreateWatcher(
                                //BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
                                BluetoothLEDevice.GetDeviceSelectorFromPairingState(true),
                                requestedProperties,
                                DeviceInformationKind.AssociationEndpoint);
            deviceWatcher1.Added += DeviceWatcher_Added;

            deviceWatcher1.Updated += DeviceWatcher_Updated;
            deviceWatcher1.Removed += DeviceWatcher_Removed;

            // EnumerationCompleted and Stopped are optional to implement.
            deviceWatcher1.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher1.Stopped += DeviceWatcher_Stopped;

            deviceWatcher1.Start();*/
            
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            log("DeviceWatcher_Updated");
        }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            log("DeviceWatcher_Stopped");
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            log("DeviceWatcher_EnumerationCompleted");
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            log("DeviceWatcher_Removed");
        }

        /// <summary>
        /// Event called when a new device is discovered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="device"></param>
        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation device)
        {
            //Console.WriteLine("[DeviceWatcher_Added]" + device.Name + " ID:" + device.Id);
            String deviceAddress = device.Id.Split('-')[1];
            log("Found Device name:[" + device.Name + "] Device address:[" + deviceAddress + "]");

            scanonly = false;
            if (!scanonly && given_device_address == deviceAddress)
            //TODO Only for test
            //if (!scanonly && true)
            {
                //this.devicefound = true;
                try
                {
                    //DFUService dfs =DFUService.Instance;
                    var uARTService = UARTService.Instance;
                    uARTService.setMain(this);

                    uARTService.connectToDeviceAsync(device);
                }
                catch (Exception e)
                {
                    log(e.Message);
                }

            }
        }

        public class UARTService
        {

            #region Properties
            private static GattDeviceService service { get; set; }
            private GattCharacteristic txCharacteristic { get; set; }
            private GattCharacteristic rxCharacteristic { get; set; }

            private GattDescriptor cccd { get; set; }
            public bool IsServiceInitialized { get; set; }

            //UUID to identify the DFU service
            public static String UARTService_UUID = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
            public static String UARTCharacteristics_UUID_TX = "6e400002-b5a3-f393-e0a9-e50e24dcca9e";
            public static String UARTCharacteristics_UUID_RX = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";
            public static String CCCD = "00002902-0000-1000-8000-00805f9b34fb";

            #endregion

            private static UARTService instance = new UARTService();            
            private MainPage mainPage;

            public static UARTService Instance
            {
                get { return instance; }
            }

            public GattDeviceService Service
            {
                get { return service; }
            }


            void log(String message)
            {
                //Debug.WriteLine(message);
                this.mainPage.log(message);
             
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

                    log("Connecting to:" + deviceAddress + "...");

                    //var service1 = await GattDeviceService.FromIdAsync(device.Id);

                    BluetoothLEDevice bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(device.Id);
                    //GattDeviceService service1 = await GattDeviceService.FromIdAsync(device.Id);


                    log("Name:" + bluetoothLeDevice);

                    //Perform the connection to the device

                    log("Device " + deviceAddress + " connected.");
                    //Scan the available services
                    var services = bluetoothLeDevice.GattServices;
                    //var service1= bluetoothLeDevice.GetGattService(new Guid(UARTService_UUID));
                    
                    foreach (var service_ in services)
                    {
                        log("Service " + service_.Uuid);
                        //If DFUService found...
                        if (service_.Uuid == new Guid(UARTService.UARTService_UUID))
                        { //NRF52 DFU Service
                            log("UART Service found");
                            service = service_;
                            //Scan the available characteristics

                            var characteristics = service.GetAllCharacteristics();
                            foreach (var characteristic in characteristics)
                            {
                                log("Char " + characteristic.Uuid + "-");
                                log("Handle " + characteristic.AttributeHandle + "-");

                                if (characteristic.Uuid.ToString() == UARTCharacteristics_UUID_RX)
                                {
                                    log("UARTService_UUID_RX found ");
                                    this.rxCharacteristic = characteristic;
                                }

                                if (characteristic.Uuid.ToString() == UARTCharacteristics_UUID_TX)
                                {
                                    log("UARTService_UUID_TX found " + characteristic.UserDescription);
                                    this.txCharacteristic = characteristic;
                                }

                                var descriptors = characteristic.GetAllDescriptors();
                                foreach (var descriptor in descriptors)
                                {
                                    log("Descr " + descriptor.Uuid + "-");
                                    log("Handle " + descriptor.AttributeHandle + "-");
                                    if (descriptor.Uuid.ToString() == CCCD)
                                    {
                                        log("CCC found " + characteristic.UserDescription);
                                        this.cccd = descriptor;
                                    }


                                }

                                this.startReaderAsync(characteristic);

                            }


                            //this.startReaderAsync(this.txCharacteristic);
                            //this.startReaderAsync(this.rxCharacteristic);

                            //break;


                        }
                    }
                }
                catch (Exception e)
                {
                    log("ERROR: Accessing2 your device failed." + Environment.NewLine + e.Message + "\n" + e.StackTrace);
                }
            }
            public async void connectToDeviceAsync3()
            {
                log("connectToDeviceAsync3");
                var devices = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(new Guid(UARTService_UUID)),
                new string[] { "System.Devices.ContainerId" });

                foreach (DeviceInformation di in devices)
                {
                    log(di.Name);
                    BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(di.Id);

                    if (bleDevice == null)
                    {
                        log("--- NULL ----");
                        continue;
                    }
                    var services = bleDevice.GattServices;
                    log(bleDevice.Name);
                    log(bleDevice.ConnectionStatus+"");
                    foreach (GattDeviceService service in services)
                    {
                        log(service.Uuid+"");

                    }

                    bleDevice.Dispose();
                }
            }

            /// <summary>
            /// Connect to the device, check if there's the DFU Service and start the firmware update
            /// </summary>
            /// <param name="device">The device discovered</param>
            /// <returns></returns>
            public async void connectToDeviceAsync2(DeviceInformation device)
            {
                try
                {
                    var deviceAddress = "N/A";
                    if (device.Id.Contains("-"))
                        deviceAddress = device.Id.Split('-')[1];

                    log("Connecting to:" + deviceAddress + "...");

                    BluetoothLEDevice bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(device.Id);

                    log("Name:" + bluetoothLeDevice);
                    log("Status:" + bluetoothLeDevice.ConnectionStatus);


                    //Perform the connection to the device
                    log("Device " + deviceAddress + " connected.");
                    //Scan the available services
                    var services = bluetoothLeDevice.GattServices;
                    //var service1 = bluetoothLeDevice.GetGattService(new Guid(UARTService_UUID));
                    foreach (var service_ in services)
                    {
                        log("Service " + service_.Uuid);
                        //If DFUService found...
                        if (service_.Uuid == new Guid(UARTService.UARTService_UUID))
                        { //NRF52 DFU Service
                            log("UART Service found");

                            var stCharacteristic = service_.GetCharacteristics(new Guid(UARTService.UARTCharacteristics_UUID_RX)).FirstOrDefault();
                            GattCharacteristicProperties properties = stCharacteristic.CharacteristicProperties;

                            if (properties.HasFlag(GattCharacteristicProperties.Read))
                            {
                                log("This characteristic supports reading from it.");
                                //this.startReaderAsync(this.txCharacteristic);
                                //this.startReaderAsync(this.rxCharacteristic);
                            }
                            if (properties.HasFlag(GattCharacteristicProperties.Write))
                            {
                                log("This characteristic supports reading from it.");
                            }
                            if (properties.HasFlag(GattCharacteristicProperties.Notify))
                            {
                                log("This characteristic supports subscribing to notifications.");

                                GattCommunicationStatus status = await stCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                        GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                if (status == GattCommunicationStatus.Success)
                                {
                                    log("Success");
                                    stCharacteristic.ValueChanged += Characteristic_ValueChangedTest;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    log("ERROR: Accessing2 your device failed." + Environment.NewLine + e.Message + "\n" + e.StackTrace);
                }
            }

            private async void startReaderAsync(GattCharacteristic characteristic)
            {
                GattCharacteristicProperties properties = characteristic.CharacteristicProperties;

                if (properties.HasFlag(GattCharacteristicProperties.Read))
                {
                    log("This characteristic supports reading from it.");

                    while (true)
                    {
                        GattReadResult result = await this.txCharacteristic.ReadValueAsync();
                        if (result.Status == GattCommunicationStatus.Success)
                        {
                            /*
                            byte[] value = new byte[result.Value.Length];
                            DataReader.FromBuffer(result.Value).ReadBytes(value);
                            Console.WriteLine(value.ToString());
                            */
                            DataReader dataReader = Windows.Storage.Streams.DataReader.FromBuffer(result.Value);
                            dataReader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                            string text = dataReader.ReadString(result.Value.Length);
                            log(text);
                        }
                    }
                }
                if (properties.HasFlag(GattCharacteristicProperties.Write))
                {
                    log("This characteristic supports reading from it.");
                }
                if (properties.HasFlag(GattCharacteristicProperties.Notify))
                {
                    log("This characteristic supports subscribing to notifications.");

                    GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                            GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (status == GattCommunicationStatus.Success)
                    {
                        log("Success");
                        characteristic.ValueChanged += Characteristic_ValueChangedTest;
                    }
                }


            }

            void Characteristic_ValueChangedTest(GattCharacteristic sender,
                                                        GattValueChangedEventArgs args)
            {
                log("Characteristic_ValueChanged " + sender.Uuid);

                // An Indicate or Notify reported that the value has changed.
                DataReader reader = DataReader.FromBuffer(args.CharacteristicValue);
                log("Len:" + reader.UnconsumedBufferLength);
                log("Read:" + reader.ReadString(reader.UnconsumedBufferLength));
            }

            internal void setMain(MainPage mainPage_)
            {
                this.mainPage = mainPage_;
            }
        }
    }
}
