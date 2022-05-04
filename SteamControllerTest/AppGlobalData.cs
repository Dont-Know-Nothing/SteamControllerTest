﻿using HidLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamControllerTest.SteamControllerLibrary;

namespace SteamControllerTest
{
    public class AppGlobalData
    {
        public const string APP_SETTINGS_FILENAME = "Settings.json";
        public const string CONTROLLER_CONFIGS_FILENAME = "ControllerConfigs.json";
        public const string APP_FOLDER_NAME = "SCAutism";
        public const string PROFILES_FOLDER_NAME = "Profiles";
        public const string LOGS_FOLDER_NAME = "Logs";
        public const string STEAM_CONTROLLER_PROFILE_DIR = "SteamController";
        public const string SWITCH_PRO_PROFILE_DIR = "SwitchPro";

        public static string exelocation = Process.GetCurrentProcess().MainModule.FileName;
        public static string exedirpath = Directory.GetParent(exelocation).FullName;
        public static string exeFileName = Path.GetFileName(exelocation);

        public static FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(exelocation);
        public static string exeversion = fileVersion.ProductVersion;
        public static ulong exeversionLong = (ulong)fileVersion.ProductMajorPart << 48 |
            (ulong)fileVersion.ProductMinorPart << 32 | (ulong)fileVersion.ProductBuildPart << 16;
        public static ulong fullExeVersionLong = exeversionLong | (ushort)fileVersion.ProductPrivatePart;

        public string appdatapath;
        public string userAppDataPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), APP_FOLDER_NAME);
        public string baseProfilesPath;
        public string controllerConfigsPath;

        public const string BLANK_VIGEMBUS_VERSION = "0.0.0.0";
        public const string MIN_SUPPORTED_VIGEMBUS_VERSION = "1.17.333.0";
        private const string VIGEMBUS_GUID = "{96E42B22-F5E9-42F8-B043-ED0F932F014F}";

        public static bool vigemInstalled = false;
        public static string vigembusVersion = BLANK_VIGEMBUS_VERSION;
        public static Version vigemBusVersionInfo =
            new Version(!string.IsNullOrEmpty(vigembusVersion) ? vigembusVersion :
                BLANK_VIGEMBUS_VERSION);
        public static Version minSupportedViGEmBusVersionInfo = new Version(MIN_SUPPORTED_VIGEMBUS_VERSION);

        public const string BLANK_FAKERINPUT_VERSION = "0.0.0.0";
        public bool fakerInputInstalled;
        public string fakerInputVersion = BLANK_FAKERINPUT_VERSION;

        public bool hidHideInstalled;

        public bool appSettingsDirFound;

        public const int CONFIG_VERSION = 0;
        public const int APP_CONFIG_VERSION = 0;
        public const string ASSEMBLY_RESOURCE_PREFIX = "pack://application:,,,/SteamControllerTest;";
        public const string RESOURCES_PREFIX = "/SteamControllertest;component/Resources";

        public AppSettingsStore appSettings;

        public AppGlobalData()
        {
            // Default to using remote AppData folder
            appdatapath = userAppDataPath;
            baseProfilesPath = Path.Combine(appdatapath, PROFILES_FOLDER_NAME);
            controllerConfigsPath = Path.Combine(appdatapath, CONTROLLER_CONFIGS_FILENAME);
        }

        public void FindConfigLocation()
        {
            if (Directory.Exists(appdatapath))
            {
                appSettingsDirFound = true;
            }
        }

        public bool CreateBaseConfigSkeleton()
        {
            bool result = true;
            try
            {
                Directory.CreateDirectory(appdatapath);
                Directory.CreateDirectory(Path.Combine(appdatapath, PROFILES_FOLDER_NAME));
                Directory.CreateDirectory(Path.Combine(appdatapath, PROFILES_FOLDER_NAME, "SteamController"));
                Directory.CreateDirectory(Path.Combine(appdatapath, PROFILES_FOLDER_NAME, "SwitchPro"));
                Directory.CreateDirectory(Path.Combine(appdatapath, LOGS_FOLDER_NAME));
            }
            catch (UnauthorizedAccessException)
            {
                result = false;
            }


            return result;
        }

        public void RefreshBaseDriverInfo()
        {
            RefreshViGEmBusInfo();
            fakerInputVersion = FakerInputVersion();
            hidHideInstalled = IsHidHideInstalled();
        }

        public void LoadAppSettings()
        {
            string configPath = Path.Combine(appdatapath, APP_SETTINGS_FILENAME);
            appSettings = new AppSettingsStore(configPath);
            appSettings.LoadConfig();
        }

        public void SaveAppSettings()
        {
            if (appSettings != null)
            {
                appSettings.SaveConfig();
            }
        }

        public static bool IsRunningSupportedViGEmBus()
        {
            //return vigemInstalled;
            return vigemInstalled &&
                minSupportedViGEmBusVersionInfo.CompareTo(vigemBusVersionInfo) <= 0;
        }

        public void RefreshViGEmBusInfo()
        {
            FindViGEmDeviceInfo();
        }

        private void FindViGEmDeviceInfo()
        {
            bool result = false;
            Guid deviceGuid = Guid.Parse(VIGEMBUS_GUID);
            NativeMethods.SP_DEVINFO_DATA deviceInfoData =
                new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize =
                System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);

            var dataBuffer = new byte[4096];
            ulong propertyType = 0;
            var requiredSize = 0;

            // Properties to retrieve
            NativeMethods.DEVPROPKEY[] lookupProperties = new NativeMethods.DEVPROPKEY[]
            {
                NativeMethods.DEVPKEY_Device_DriverVersion, NativeMethods.DEVPKEY_Device_InstanceId,
                NativeMethods.DEVPKEY_Device_Manufacturer, NativeMethods.DEVPKEY_Device_Provider,
                NativeMethods.DEVPKEY_Device_DeviceDesc,
            };

            List<ViGEmBusInfo> tempViGEmBusInfoList = new List<ViGEmBusInfo>();

            IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref deviceGuid, null, 0,
                NativeMethods.DIGCF_DEVICEINTERFACE);
            for (int i = 0; !result && NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
            {
                ViGEmBusInfo tempBusInfo = new ViGEmBusInfo();

                foreach (NativeMethods.DEVPROPKEY currentDevKey in lookupProperties)
                {
                    NativeMethods.DEVPROPKEY tempKey = currentDevKey;
                    if (NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData,
                        ref tempKey, ref propertyType,
                        dataBuffer, dataBuffer.Length, ref requiredSize, 0))
                    {
                        string temp = dataBuffer.ToUTF16String();
                        if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_DriverVersion.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_DriverVersion.pid)
                        {
                            try
                            {
                                tempBusInfo.deviceVersion = new Version(temp);
                                tempBusInfo.deviceVersionStr = temp;
                            }
                            catch (ArgumentException)
                            {
                                // Default to unknown version
                                tempBusInfo.deviceVersionStr = BLANK_VIGEMBUS_VERSION;
                                tempBusInfo.deviceVersion = new Version(tempBusInfo.deviceVersionStr);
                            }
                        }
                        else if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_InstanceId.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_InstanceId.pid)
                        {
                            tempBusInfo.instanceId = temp;
                        }
                        else if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_Manufacturer.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_Manufacturer.pid)
                        {
                            tempBusInfo.manufacturer = temp;
                        }
                        else if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_Provider.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_Provider.pid)
                        {
                            tempBusInfo.driverProviderName = temp;
                        }
                        else if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_DeviceDesc.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_DeviceDesc.pid)
                        {
                            tempBusInfo.deviceName = temp;
                        }
                    }
                }

                tempViGEmBusInfoList.Add(tempBusInfo);
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            // Iterate over list and find most recent version number
            //IEnumerable<ViGEmBusInfo> tempResults = tempViGEmBusInfoList.Where(item => minSupportedViGEmBusVersionInfo.CompareTo(item.deviceVersion) <= 0);
            Version latestKnown = new Version(BLANK_VIGEMBUS_VERSION);
            string deviceInstanceId = string.Empty;
            foreach (ViGEmBusInfo item in tempViGEmBusInfoList)
            {
                if (latestKnown.CompareTo(item.deviceVersion) <= 0)
                {
                    latestKnown = item.deviceVersion;
                    deviceInstanceId = item.instanceId;
                }
            }

            // Get bus info for most recent version found and save info
            ViGEmBusInfo latestBusInfo =
                tempViGEmBusInfoList.SingleOrDefault(item => item.instanceId == deviceInstanceId);
            PopulateFromViGEmBusInfo(latestBusInfo);
        }

        private void PopulateFromViGEmBusInfo(ViGEmBusInfo busInfo)
        {
            if (busInfo != null)
            {
                vigemInstalled = true;
                vigembusVersion = busInfo.deviceVersionStr;
                vigemBusVersionInfo = busInfo.deviceVersion;
            }
            else
            {
                vigemInstalled = false;
                vigembusVersion = BLANK_VIGEMBUS_VERSION;
                vigemBusVersionInfo = new Version(BLANK_VIGEMBUS_VERSION);
            }
        }

        public void RefreshFakerInputInfo()
        {
            fakerInputInstalled = IsFakerInputInstalled();
            fakerInputVersion = FakerInputVersion();
        }

        private static string FakerInputVersion()
        {
            // Start with BLANK_FAKERINPUT_VERSION for result
            string result = BLANK_FAKERINPUT_VERSION;
            IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref Util.fakerInputGuid, null, 0, NativeMethods.DIGCF_DEVICEINTERFACE);
            NativeMethods.SP_DEVINFO_DATA deviceInfoData = new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);
            bool foundDev = false;
            //bool success = NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData);
            for (int i = 0; !foundDev && NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
            {
                ulong devPropertyType = 0;
                int requiredSizeProp = 0;
                NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData,
                    ref NativeMethods.DEVPKEY_Device_DriverVersion, ref devPropertyType, null, 0, ref requiredSizeProp, 0);

                if (requiredSizeProp > 0)
                {
                    var versionTextBuffer = new byte[requiredSizeProp];
                    NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData,
                        ref NativeMethods.DEVPKEY_Device_DriverVersion, ref devPropertyType, versionTextBuffer, requiredSizeProp, ref requiredSizeProp, 0);

                    string tmpitnow = System.Text.Encoding.Unicode.GetString(versionTextBuffer);
                    string tempStrip = tmpitnow.TrimEnd('\0');
                    foundDev = true;
                    result = tempStrip;
                }
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return result;
        }

        public bool IsHidHideInstalled()
        {
            return CheckForSysDevice(@"root\HidHide");
        }

        public bool IsFakerInputInstalled()
        {
            return CheckForSysDevice(@"root\FakerInput");
        }

        private static bool CheckForSysDevice(string searchHardwareId)
        {
            bool result = false;
            Guid sysGuid = Guid.Parse("{4d36e97d-e325-11ce-bfc1-08002be10318}");
            NativeMethods.SP_DEVINFO_DATA deviceInfoData =
                new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize =
                System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);
            var dataBuffer = new byte[4096];
            ulong propertyType = 0;
            var requiredSize = 0;
            IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref sysGuid, null, 0, 0);
            for (int i = 0; !result && NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
            {
                if (NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData,
                    ref NativeMethods.DEVPKEY_Device_HardwareIds, ref propertyType,
                    dataBuffer, dataBuffer.Length, ref requiredSize, 0))
                {
                    string hardwareId = dataBuffer.ToUTF16String();
                    //if (hardwareIds.Contains("Virtual Gamepad Emulation Bus"))
                    //    result = true;
                    if (hardwareId.Equals(searchHardwareId))
                        result = true;
                }
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return result;
        }

        public void CreateControllerDeviceSettingsFile()
        {
            string newJson = @"{
                ""Controllers"": [
                ]
            }";

            using (StreamWriter swriter = new StreamWriter(controllerConfigsPath))
            using (JsonTextWriter jwriter = new JsonTextWriter(swriter))
            {
                JObject.Parse(newJson).WriteTo(jwriter);
            }
        }

        public void LoadControllerDeviceSettings(SteamControllerDevice testDev,
            ControllerOptionsStore store)
        {
            using (StreamReader sreader = new StreamReader(controllerConfigsPath))
            {
                string json = sreader.ReadToEnd();

                try
                {
                    JObject tempJObj = JObject.Parse(json);
                    JToken token = (from controller in tempJObj.SelectToken($@"$.Controllers")
                         where controller.Type == JTokenType.Object && controller.Value<string>("Mac") == testDev.Serial &&
                         controller.Value<string>("Type") == store.DeviceType.ToString()
                         select controller).FirstOrDefault();

                    if (token == null)
                    {
                        return;
                    }

                    JObject controllerObj = token.ToObject<JObject>();
                    string macAddr = testDev.Serial;
                    string devType = store.DeviceType.ToString();
                    //string settings = controllerObj["Settings"].ToString();
                    store.LoadSettings(controllerObj);
                }
                catch (JsonReaderException)
                {
                }
                catch (JsonSerializationException)
                {
                }
            }
        }

        public void SaveControllerDeviceSettings(SteamControllerDevice testDev,
            ControllerOptionsStore store)
        {
            JObject tempRootJObj = null;
            using (FileStream fs = new FileStream(controllerConfigsPath,
                FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sreader = new StreamReader(fs))
                {
                    string json = sreader.ReadToEnd();

                    try
                    {
                        JToken token = null;
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            tempRootJObj = JObject.Parse(json);
                            token = (from controller in tempRootJObj.SelectToken($"$.Controllers")
                                     where controller.Type == JTokenType.Object && controller.Value<string>("Mac") == testDev.Serial &&
                                     controller.Value<string>("Type") == store.DeviceType.ToString()
                                     select controller).First();
                        }
                        else
                        {
                            string newJson = @"{
                                ""Controllers"": [
                                ]
                            }";

                            tempRootJObj = JObject.Parse(newJson);
                        }

                        if (token != null)
                        {
                            // Found existing item. Update properties and replace object
                            JObject controllerObj = token.ToObject<JObject>();
                            string macAddr = testDev.Serial;
                            string devType = InputDeviceType.SteamController.ToString();

                            controllerObj["Mac"] = macAddr;
                            controllerObj["Type"] = devType;
                            store.PersistSettings(controllerObj);
                            token.Replace(controllerObj);
                        }
                        else
                        {
                            JToken controllersToken = tempRootJObj.SelectToken("Controllers");
                            if (controllersToken == null)
                            {
                                tempRootJObj.Add(new JProperty("Controllers", new JArray()));
                            }
                            else if (controllersToken != null && controllersToken.Type != JTokenType.Array)
                            {
                                tempRootJObj.Remove("Controllers");
                                tempRootJObj.Add(new JProperty("Controllers", new JArray()));
                            }

                            // No current object found. Create a new object and add it to JArray
                            string controllerJson = @"{
                                ""Mac"": """"
                                ""Type"": """",
                                ""Settings"": {

                                }
                            }";

                            JObject controllerObj = JObject.Parse(controllerJson);
                            tempRootJObj["Controllers"].ToObject<JArray>().Add(controllerObj);

                            store.PersistSettings(controllerObj);
                        }
                    }
                    catch (JsonReaderException)
                    {
                    }
                    catch (JsonSerializationException)
                    {
                    }
                }
            }

            using (FileStream fs = new FileStream(controllerConfigsPath,
               FileMode.Truncate, FileAccess.Write))
            {
                if (tempRootJObj != null)
                {
                    using (StreamWriter swriter = new StreamWriter(fs))
                    using (JsonTextWriter jwriter = new JsonTextWriter(swriter))
                    {
                        jwriter.Formatting = Formatting.Indented;
                        jwriter.Indentation = 2;
                        string temp = tempRootJObj.ToString();
                        //Trace.WriteLine(temp);
                        tempRootJObj.WriteTo(jwriter);
                    }
                }
            }
        }
    }

    public class ViGEmBusInfo
    {
        //public string path;
        public string instanceId;
        public string deviceName;
        public string deviceVersionStr;
        public Version deviceVersion;
        public string manufacturer;
        public string driverProviderName;
    }


    public enum InputDeviceType
    {
        None,
        SteamController,
        SwitchPro,
    }

    public static class AppGlobalDataSingleton
    {
        private static AppGlobalData instance;
        public static AppGlobalData Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AppGlobalData();
                }

                return instance;
            }
        }
    }
}
