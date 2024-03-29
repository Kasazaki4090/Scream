﻿using AdonisUI;
using AdonisUI.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using Newtonsoft.Json;
using Scream.Models;
using Scream.Resources;
using Scream.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Scream
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : AdonisWindow
    {
        private const int pacListMenuItemIndex = 11;
        private const int serverMenuListIndex = 9;
        private const int routingRuleSetMenuItemIndex = 10;


        public TaskbarIcon notifyIcon;
        public Toggle toggle = new Toggle();

        public bool proxyState = false;
        public ProxyMode proxyMode = ProxyMode.manual;
        public int localPort = 1081;
        public int httpPort = 8001;
        public bool udpSupport = false;
        public bool shareOverLan = false;
        public bool useCusProfile = false;
        public bool usePartServer = false;
        public bool useMultipleServer = false;
        public int selectedServerIndex = 0;
        private string selectedCusConfig = "";
        public int selectedRoutingSet = 0;
        public string selectedPacFileName = "pac.js";
        public string dnsString = "localhost";
        public string byPass = "< local >; localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*;localhost.ptlogin2.qq.com";
        public bool colorScheme = false;
        public bool autoStart = false;

        public List<Dictionary<string, object>> profiles = new List<Dictionary<string, object>>();
        public List<Dictionary<string, object>> subsOutbounds = new List<Dictionary<string, object>>();
        public List<object> selectedPartServerIndex = new List<object>();
        public List<string> cusProfiles = new List<string>();
        public string logLevel = "none";
        public bool enableRestore = false;
        public List<string> subscriptionTag = new List<string>();
        public List<string> subscriptionUrl = new List<string>();
        public List<Dictionary<string, object>> routingRuleSets = new List<Dictionary<string, object>> { Utilities.ROUTING_GLOBAL, Utilities.ROUTING_DIRECT, Utilities.ROUTING_BYPASSCN_PRIVATE_APPLE };

        private FileSystemWatcher pacFileWatcher;
        private FileSystemWatcher cusFileWatcher;
        private FileSystemWatcher confdirFileWatcher;

        public MainWindow()
        {
            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);
            SystemEvents.DisplaySettingsChanged += new EventHandler(SystemEvents_DisplaySettingsChanged);
#if DEBUG
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#endif
            Hide();
            InitializeComponent();
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            this.mainMenu = FindResource("TrayMenu") as ContextMenu;
            if (CheckFiles() == false)
            {
                Application.Current.Shutdown();
                return;
            }

            configScanner.DoWork += ConfigScanner_DoWork;
            configScanner.RunWorkerCompleted += ConfigScanner_RunWorkerCompleted;
            configScanner.RunWorkerAsync();

            // read config
            ReadSettings();
            this.InitializeHttpServer();
            this.InitializeCoreProcess();
            pacFileWatcher = new FileSystemWatcher(AppDomain.CurrentDomain.BaseDirectory + @"pac\")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                Filter = selectedPacFileName
            };
            pacFileWatcher.Changed += (object source, FileSystemEventArgs fe) =>
            {
                Debug.WriteLine("detect pac content change change");
                if (proxyState == true && proxyMode == ProxyMode.pac)
                {
                    UpdateSystemProxy();
                }
            };
            pacFileWatcher.EnableRaisingEvents = true;
            cusFileWatcher = new FileSystemWatcher(AppDomain.CurrentDomain.BaseDirectory + @"config\", "*.json");
            cusFileWatcher.Deleted += (object source, FileSystemEventArgs e) =>
            {
                cusProfiles.Remove(e.Name);
                Dispatcher.Invoke(() => { UpdateServerMenuList(speedTestResultDic); });
                Debug.WriteLine($"cusconfig deleted: {e.FullPath}");
            };
            cusFileWatcher.Created += CusFileWatcher_Changed;
            cusFileWatcher.Changed += CusFileWatcher_Changed;
            cusFileWatcher.Renamed += (object sender, RenamedEventArgs e) =>
            {
                cusProfiles.Remove(e.OldName);
                Dispatcher.Invoke(() => { UpdateServerMenuList(speedTestResultDic); });
                if (e.Name.Substring(e.Name.LastIndexOf(".") + 1) == "json")
                {
                    Dispatcher.Invoke(() => { CusFileWatcher_Changed(sender, e); });
                }
            };
            cusFileWatcher.EnableRaisingEvents = true;
            confdirFileWatcher = new FileSystemWatcher(AppDomain.CurrentDomain.BaseDirectory + @"config\confdir\", "*.json");
            confdirFileWatcher.Deleted += (object source, FileSystemEventArgs e) =>
            {
                Dispatcher.Invoke(() => { CoreConfigChanged(this); });
                Debug.WriteLine($"confdir config deleted: {e.FullPath}");
            };
            confdirFileWatcher.Created += ConfdirFileWatcher_Changed;
            confdirFileWatcher.Changed += ConfdirFileWatcher_Changed;
            confdirFileWatcher.Renamed += (object sender, RenamedEventArgs e) =>
            {
                if (e.Name.Substring(e.Name.LastIndexOf(".") + 1) == "json")
                {
                    Dispatcher.Invoke(() => { ConfdirFileWatcher_Changed(sender, e); });
                }
            };
            confdirFileWatcher.EnableRaisingEvents = true;
            OverallChanged(this, null);
        }

        #region navigate
        private void NavigateMajor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Major.SelectedIndex == 0)
            {
                Outbounds outbounds = new Outbounds
                {
                    mainWindow = this
                };
                outbounds.InitializeData();
                PageFrame.Content = outbounds;
            }
            if (Major.SelectedIndex == 1)
            {
                Routes routes = new Routes
                {
                    mainWindow = this
                };
                routes.InitializeData();
                PageFrame.Content = routes;
            }
            if (Major.SelectedIndex == 2)
            {
                Preferences preferences = new Preferences
                {
                    mainWindow = this
                };
                preferences.InitializeData();
                PageFrame.Content = preferences;
            }
        }
        private void ApplyConfig_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OverallChanged(this, null);
            WriteSettings();
        }
        #endregion

        #region toggle
        private void Toggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ToggleUDPSupport)
            {
                udpSupport = toggle.UDPSupport;
            }
            if (sender == ToggleShareOverLan)
            {
                shareOverLan = toggle.ShareOverLan;
            }
            if (sender == ToggleAutoStart)
            {
                autoStart = toggle.AutoStart;
            }
            if (sender == ToggleColorScheme)
            {
                colorScheme = toggle.ColorScheme;
                ResourceLocator.SetColorScheme(Application.Current.Resources, !toggle.ColorScheme ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);
                mainMenu.UpdateDefaultStyle();
            }
        }
        #endregion

        #region systemevent,power&display
        void SystemEvents_PowerModeChanged(object s, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    notifyIcon.Icon = Properties.Resources.Scream;
                    break;
                case PowerModes.Suspend:
                    break;
            }
        }

        void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            notifyIcon.Icon = Properties.Resources.Scream;
        }

        #endregion

        #region startup check
        private bool ClearLog()
        {
            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"log\"))
            {
                try
                {
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"log\");
                }
                catch
                {
                    System.Windows.MessageBox.Show(AppDomain.CurrentDomain.BaseDirectory + @"log\", Strings.messagedircreatefail);
                    return false;
                }
            }
            try
            {
                File.Create(AppDomain.CurrentDomain.BaseDirectory + @"log\access.log").Close();
                File.Create(AppDomain.CurrentDomain.BaseDirectory + @"log\error.log").Close();
            }
            catch
            {
                AdonisUI.Controls.MessageBox.Show(AppDomain.CurrentDomain.BaseDirectory + "log\\access.log\n" + AppDomain.CurrentDomain.BaseDirectory + @"log\access.log", Strings.messagedircreatefail);
                return false;
            }
            return true;
        }


        private bool CheckFiles()
        {
            #region pac, config, log dir 
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            foreach (string folder in new string[] { @"pac\", @"config\", @"config\confdir\" })
            {
                if (!Directory.Exists(currentDir + folder))
                {
                    try
                    {
                        Directory.CreateDirectory(currentDir + folder);
                    }
                    catch
                    {
                        AdonisUI.Controls.MessageBox.Show(currentDir + folder, Strings.messagedircreatefail);
                        return false;
                    }
                }
            }
            if (ClearLog() == false)
            {
                return false;
            }
            #endregion

            #region check core

            bool findMissingFile = false;
            string coreDirectory = AppDomain.CurrentDomain.BaseDirectory + @"v2ray-core\";
            string coreDirectoryX = AppDomain.CurrentDomain.BaseDirectory + @"xray-core\";
            foreach (Tuple<string, string> file in Utilities.necessaryFiles.Zip(Utilities.necessaryFilesX, Tuple.Create))
            {
                if (!File.Exists(coreDirectory + file.Item1) && !File.Exists(coreDirectoryX + file.Item2))
                {
                    findMissingFile = true;
                    break;
                }
                if (File.Exists(coreDirectoryX + file.Item2))
                {
                    Utilities.corePath = coreDirectoryX + @"xray.exe";
                }
            }
            if (findMissingFile)
            {
                AdonisUI.Controls.MessageBoxResult result = AdonisUI.Controls.MessageBox.Show(Strings.messagenocore, Strings.messagenocoretitle, AdonisUI.Controls.MessageBoxButton.OKCancel);
                switch (result)
                {
                    case AdonisUI.Controls.MessageBoxResult.OK:
                        {
                            proxyState = false;
                            Process.Start(Strings.coreUrl);
                            return true;
                        }
                    case AdonisUI.Controls.MessageBoxResult.Cancel:
                        {
                            return false;
                        }
                    default:
                        {
                            return false;
                        }
                }

            }
            else
            {
                (mainMenu.Items[1] as MenuItem).IsEnabled = true;
                return true;
            }
            #endregion 
        }

        #endregion

        #region read/write settings
        private void WriteSettings()
        {
            string settingPath = AppDomain.CurrentDomain.BaseDirectory + "settings.json";
            Dictionary<string, object> settings = new Dictionary<string, object>
            {
                {
                    "appStatus",
                    new Dictionary<string, object>
                    {
                        { "autoStart", autoStart },
                        { "colorScheme", colorScheme },
                        { "proxyState", proxyState },
                        { "proxyMode", proxyMode },
                        { "selectedServerIndex", selectedServerIndex },
                        { "selectedCusConfig", selectedCusConfig },
                        { "selectedRoutingSet", selectedRoutingSet },
                        { "usePartServer", usePartServer },
                        { "useMultipleServer", useMultipleServer },
                        { "useCusProfile", useCusProfile }
                    }
                },
                {
                    "selectedPartServerIndex",
                    new Dictionary<string, object>
                    {
                        { "index", selectedPartServerIndex }
                    }
                },
                { "byPass", byPass },
                {
                    "subScriptions",
                    new Dictionary<string, object>
                    {
                        { "url", subscriptionUrl },
                        { "tag", subscriptionTag }
                    }
                },
                { "selectedPacFileName", selectedPacFileName },
                { "logLevel", logLevel },
                { "localPort", localPort },
                { "httpPort", httpPort },
                { "udpSupport", udpSupport },
                { "shareOverLan", shareOverLan },
                { "dnsString", dnsString },
                { "enableRestore", enableRestore },
                { "profiles", profiles },
                { "routingRuleSets", routingRuleSets },
            };
            File.WriteAllText(settingPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }

        private void ReadSettings()
        {
            string settingPath = AppDomain.CurrentDomain.BaseDirectory + "settings.json";
            if (!File.Exists(settingPath))
            {
                WriteSettings();
            }
            else
            {
                JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
                string settingString = File.ReadAllText(settingPath);

                dynamic settings = javaScriptSerializer.Deserialize<dynamic>(settingString);
                try
                {
                    autoStart = settings["appStatus"]["autoStart"];
                    colorScheme = settings["appStatus"]["colorScheme"];
                    proxyState = settings["appStatus"]["proxyState"];
                    proxyMode = (ProxyMode)settings["appStatus"]["proxyMode"];
                    selectedServerIndex = settings["appStatus"]["selectedServerIndex"];
                    selectedCusConfig = settings["appStatus"]["selectedCusConfig"];
                    selectedRoutingSet = settings["appStatus"]["selectedRoutingSet"];
                    usePartServer = settings["appStatus"]["usePartServer"];
                    useMultipleServer = settings["appStatus"]["useMultipleServer"];
                    useCusProfile = settings["appStatus"]["useCusProfile"];

                    selectedPacFileName = settings["selectedPacFileName"];
                    
                    logLevel = settings["logLevel"];
                    localPort = (int)settings["localPort"];
                    httpPort = (int)settings["httpPort"];
                    udpSupport = settings["udpSupport"];
                    shareOverLan = settings["shareOverLan"];
                    dnsString = settings["dnsString"];
                    enableRestore = settings["enableRestore"];

                    foreach (int index in settings["selectedPartServerIndex"]["index"])
                    {
                        selectedPartServerIndex.Add((int)index);
                    }
                    byPass = settings["byPass"];
                    foreach (string url in settings["subScriptions"]["url"])
                    {
                        subscriptionUrl.Add(url);
                    }
                    foreach (string tag in settings["subScriptions"]["tag"])
                    {
                        subscriptionTag.Add(tag);
                    }
                    foreach (dynamic profile in settings["profiles"])
                    {
                        try
                        {
                            if (Utilities.RESERVED_TAGS.FindIndex(x => x == profile["tag"] as string) == -1)
                            {
                                profiles.Add(profile as Dictionary<string, object>);
                            }
                        }
                        catch { continue; }
                    }
                    routingRuleSets.Clear();
                    foreach (Dictionary<string, object> set in settings["routingRuleSets"])
                    {
                        routingRuleSets.Add(set);
                    }
                    Debug.WriteLine($"read {routingRuleSets.Count} rules");
                    if (routingRuleSets.Count == 0)
                    {
                        routingRuleSets = new List<Dictionary<string, object>> { Utilities.ROUTING_GLOBAL, Utilities.ROUTING_DIRECT, Utilities.ROUTING_BYPASSCN_PRIVATE_APPLE };
                        Debug.WriteLine("reset routing rules");
                    }
                }
                catch
                {
                    notifyIcon.ShowBalloonTip("", Strings.messagereaddefaultserror, BalloonIcon.Info);
                }
            }
        }

        #endregion

        private ContextMenu mainMenu;

        #region mode and status management

        private void UpdateStatusAndModeMenus()
        {
            if (proxyState)
            {
                (mainMenu.Items[0] as MenuItem).Header = Strings.coreloaded;
                (mainMenu.Items[1] as MenuItem).Header = Strings.unloadcore;
            }
            else
            {
                (mainMenu.Items[0] as MenuItem).Header = Strings.coreunloaded;
                (mainMenu.Items[1] as MenuItem).Header = Strings.loadcore;
            }
            for (int i = 0; i < 3; i += 1)
            {
                (mainMenu.Items[i + 5] as MenuItem).IsChecked = (int)proxyMode == i;
            }
        }

        private void ModeChanged(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"mode changed by {sender} {e}");
            MenuItem senderItem = sender as MenuItem;
            int senderTag = Int32.Parse(senderItem.Tag as string);
            if (proxyState == true && proxyMode == ProxyMode.manual && senderTag != (int)ProxyMode.manual)
            {
                this.BackupSystemProxy();
            }
            if (proxyState == true && proxyMode != ProxyMode.manual && senderTag == (int)ProxyMode.manual)
            {
                if (enableRestore)
                {
                    RestoreSystemProxy();
                }
                else
                {
                    CancelSystemProxy();
                }
            }
            proxyMode = (ProxyMode)senderTag;
            this.UpdateStatusAndModeMenus();
            if (senderTag == (int)ProxyMode.pac)
            {
                this.UpdatePacMenuList();
            }
            if (proxyState == true)
            {
                this.UpdateSystemProxy();
            }
        }

        public void OverallChanged(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("refresh all settings");
            bool previousStatus = proxyState;
            if (sender == this)
            {
                previousStatus = false;
            }
            else if (sender == mainMenu.Items[1] || sender == v2rayCoreWorker)
            {
                proxyState = !proxyState;
            }
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"pac\" + selectedPacFileName))
            {
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"pac\" + selectedPacFileName, Properties.Resources.simplepac);
            }

            selectedServerIndex = Math.Min(profiles.Count + subsOutbounds.Count - 1, selectedServerIndex);
            selectedPartServerIndex.RemoveAll(x => (int)x >= (profiles.Count + subsOutbounds.Count));
            if (profiles.Count + subsOutbounds.Count > 0)
            {
                selectedServerIndex = Math.Max(selectedServerIndex, 0);
                selectedPartServerIndex = selectedPartServerIndex.Count > 0 ? selectedPartServerIndex : new List<object> { 0 };
            }
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"config\" + selectedCusConfig))
            {
                selectedCusConfig = "";
            }
            selectedRoutingSet = Math.Min(routingRuleSets.Count - 1, selectedRoutingSet);
            if (routingRuleSets.Count > 0)
            {
                selectedRoutingSet = Math.Max(selectedRoutingSet, 0);
            }

            if ((!useMultipleServer && selectedServerIndex == -1 && selectedCusConfig == "") ||
                (useMultipleServer && profiles.Count + subsOutbounds.Count < 1))
            {
                proxyState = false;
            }
            else if (!useMultipleServer && selectedCusConfig == "")
            {
                useCusProfile = false;
            }
            else if (!useMultipleServer && selectedServerIndex == -1)
            {
                useCusProfile = true;
            }

            if (proxyMode != ProxyMode.manual)
            {
                if (previousStatus == false && proxyState == true)
                {
                    this.BackupSystemProxy();
                }
                else if (previousStatus == true && proxyState == false)
                {
                    if (enableRestore)
                    {
                        RestoreSystemProxy();
                    }
                    else
                    {
                        CancelSystemProxy();
                    }
                }
            }

            this.CoreConfigChanged(this);

            if (proxyState == true)
            {
                this.UpdateSystemProxy();
            }
            else if (sender == mainMenu.Items[1])
            {
                this.UnloadV2ray();
            }
            this.UpdateStatusAndModeMenus();
            this.UpdatePacMenuList();
        }

        #endregion

        #region pac management

        private void UpdatePacMenuList()
        {
            MenuItem pacMenuList = mainMenu.Items[pacListMenuItemIndex] as MenuItem;
            pacMenuList.Items.Clear();
            string pacDir = AppDomain.CurrentDomain.BaseDirectory + @"pac\";
            DirectoryInfo pacDirInfo = new DirectoryInfo(pacDir);
            FileInfo[] pacFiles = pacDirInfo.GetFiles("*.js", SearchOption.TopDirectoryOnly);
            int i = 0;
            foreach (FileInfo pacFile in pacFiles)
            {
                MenuItem menuItem = new MenuItem
                {
                    Header = "_" + pacFile.Name,
                    Tag = i,
                    IsCheckable = true,
                    IsChecked = pacFile.Name == selectedPacFileName
                };
                menuItem.Click += SwitchPac;
                pacMenuList.Items.Add(menuItem);
                i += 1;
            }
            pacMenuList.Items.Add(new Separator());
            pacMenuList.Items.Add(FindResource("editPacMenuItem"));
            pacMenuList.Items.Add(FindResource("resetPacButton"));
        }

        private void SwitchPac(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"pac switched by {sender} {e}");
            this.selectedPacFileName = ((sender as MenuItem).Header as string).Substring(1); // exclude leading _
            pacFileWatcher.Filter = selectedPacFileName;
            if (proxyState == true && proxyMode == ProxyMode.pac)
            {
                UpdateSystemProxy();
                UpdatePacMenuList();
            }
        }

        private void EditPacMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "/select, \"" + AppDomain.CurrentDomain.BaseDirectory + @"pac\" + selectedPacFileName + @"\");
        }

        #endregion

        #region server info management
        private const int usePartServerTag = -9;
        private const int useAllServerTag = -10;
        private const int useCusConfigTag = -11;
        private bool speedtestState = true;
        private BackgroundWorker configScanner = new BackgroundWorker();
        private void ConfdirFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (confdirFileWatcher != null)
            {
                confdirFileWatcher.EnableRaisingEvents = false;
                Thread th = new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(3000);
                    Process v2rayProcess = new Process();
                    v2rayProcess.StartInfo.FileName = Utilities.corePath;
                    DirectoryInfo confdirDirectoryInfo = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + @"config\confdir\");
                    FileInfo[] Configs = confdirDirectoryInfo.GetFiles("*.json", SearchOption.TopDirectoryOnly);
                    foreach (FileInfo conf in Configs)
                    {
                        Debug.WriteLine(v2rayProcess.StartInfo.FileName);
                        v2rayProcess.StartInfo.Arguments = "-test -config " + "\"" + conf.FullName + "\"";
                        v2rayProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        try
                        {
                            v2rayProcess.Start();
                            v2rayProcess.WaitForExit();
                            Debug.WriteLine("exit code is " + v2rayProcess.ExitCode.ToString());
                            if (v2rayProcess.ExitCode != 0)
                            {
                                File.Move(conf.FullName, string.Format(@"{0}.error", conf.FullName));
                            }
                        }
                        catch { };
                        Debug.WriteLine($"confdir config changed: {e.FullPath}");
                    }
                    Dispatcher.Invoke(() => { CoreConfigChanged(this); });
                    confdirFileWatcher.EnableRaisingEvents = true;
                    v2rayProcess.Dispose();
                    GC.SuppressFinalize(this);
                }
                ));
                th.Start();
            }
        }
        private void CusFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (cusFileWatcher != null)
            {
                cusFileWatcher.EnableRaisingEvents = false;
                
                Thread th = new Thread(new ThreadStart(()=>
                {
                    Thread.Sleep(3000);

                    Process v2rayProcess = new Process();
                    v2rayProcess.StartInfo.FileName = Utilities.corePath;

                    DirectoryInfo configDirectoryInfo = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + @"config\");
                    FileInfo[] cusConfigs = configDirectoryInfo.GetFiles("*.json", SearchOption.TopDirectoryOnly);
                    cusProfiles.Clear();
                    foreach (FileInfo cusConfig in cusConfigs)
                    {
                        Debug.WriteLine(v2rayProcess.StartInfo.FileName);
                        v2rayProcess.StartInfo.Arguments = "-test -config " + "\"" + cusConfig.FullName + "\"";
                        v2rayProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        try
                        {
                            v2rayProcess.Start();
                            v2rayProcess.WaitForExit();
                            Debug.WriteLine("exit code is " + v2rayProcess.ExitCode.ToString());
                            if (v2rayProcess.ExitCode == 0)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    cusProfiles.Add(cusConfig.Name);
                                });
                            }
                            else {
                                File.Move(cusConfig.FullName, string.Format(@"{0}.error", cusConfig.FullName));
                            }
                        }
                        catch { };
                    }
                    Dispatcher.Invoke(() => { UpdateServerMenuList(speedTestResultDic); });
                    cusFileWatcher.EnableRaisingEvents = true;
                    v2rayProcess.Dispose();
                    GC.SuppressFinalize(this);
                }
                ));
                th.Start();
                
            }
        }
        private void ConfigScanner_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Dispatcher.Invoke(() => { UpdateServerMenuList(speedTestResultDic); });
            configScanner.DoWork -= ConfigScanner_DoWork;
            configScanner.RunWorkerCompleted -= ConfigScanner_RunWorkerCompleted;
            configScanner = null;
            ExtUtils.FlushMemory();
        }

        private void ConfigScanner_DoWork(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("will scan configs");
            Process v2rayProcess = new Process();
            v2rayProcess.StartInfo.FileName = Utilities.corePath;
            
            DirectoryInfo configDirectoryInfo = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + @"config\");
            FileInfo[] cusConfigs = configDirectoryInfo.GetFiles("*.json", SearchOption.TopDirectoryOnly);
            cusProfiles.Clear();
            foreach (FileInfo cusConfig in cusConfigs)
            {
                Debug.WriteLine(v2rayProcess.StartInfo.FileName);
                v2rayProcess.StartInfo.Arguments = "-test -config " + "\"" + cusConfig.FullName + "\"";
                v2rayProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                try
                {
                    v2rayProcess.Start();
                    v2rayProcess.WaitForExit();
                    Debug.WriteLine("exit code is " + v2rayProcess.ExitCode.ToString());
                    if (v2rayProcess.ExitCode == 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            cusProfiles.Add(cusConfig.Name);
                        });
                    }
                    else
                    {
                        File.Move(cusConfig.FullName, string.Format(@"{0}.error", cusConfig.FullName));
                    }
                }
                catch { };
            }

            DirectoryInfo confdirDirectoryInfo = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + @"config\confdir\");
            FileInfo[] Configs = confdirDirectoryInfo.GetFiles("*.json", SearchOption.TopDirectoryOnly);
            foreach (FileInfo conf in Configs)
            {
                Debug.WriteLine(v2rayProcess.StartInfo.FileName);
                v2rayProcess.StartInfo.Arguments = "-test -config " + "\"" + conf.FullName + "\"";
                v2rayProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                try
                {
                    v2rayProcess.Start();
                    v2rayProcess.WaitForExit();
                    Debug.WriteLine("exit code is " + v2rayProcess.ExitCode.ToString());
                    if (v2rayProcess.ExitCode != 0)
                    {
                        File.Move(conf.FullName, string.Format(@"{0}.error", conf.FullName));
                    }
                }
                catch { };
            }
            v2rayProcess.Dispose();
            GC.SuppressFinalize(this);
        }

        private void UpdateServerMenuList(Dictionary<string, string> responseTime)
        {
            MenuItem serverMenuList = mainMenu.Items[serverMenuListIndex] as MenuItem;
            serverMenuList.Items.Clear();
            if (profiles.Count == 0 && cusProfiles.Count == 0 && subsOutbounds.Count == 0)
            {
                serverMenuList.Items.Add(Strings.messagenoserver);
                return;
            }
            int tagIndex = 0;
            foreach (Dictionary<string, object> outbound in this.profiles)
            {
                string tagadd = responseTime.ContainsKey(outbound["tag"].ToString()) ? "[" + responseTime[outbound["tag"].ToString()] + "]" : null;
                bool selectedOutboundIndex = usePartServer ? selectedPartServerIndex.Exists(x => (int)x == tagIndex) : selectedServerIndex == tagIndex;
                MenuItem newOutboundItem = new MenuItem
                {
                    Header = (outbound["tag"] as string).Replace("_", "__") + tagadd,
                    Tag = tagIndex,
                    IsChecked = selectedOutboundIndex && !useMultipleServer && !useCusProfile
                };
                newOutboundItem.Click += SwitchServer;
                serverMenuList.Items.Add(newOutboundItem);
                tagIndex += 1;
            }
            foreach (Dictionary<string, object> outbound in this.subsOutbounds)
            {
                string tagadd = responseTime.ContainsKey(outbound["tag"].ToString()) ? "[" + responseTime[outbound[@"tag"].ToString()] + "]" : null;
                bool selectedOutboundIndex = usePartServer ? selectedPartServerIndex.Exists(x => (int)x == tagIndex) : selectedServerIndex == tagIndex;
                MenuItem newOutboundItem = new MenuItem
                {
                    Header = (outbound["tag"] as string).Replace("_", "__") + tagadd,
                    Tag = tagIndex,
                    IsChecked = selectedOutboundIndex && !useMultipleServer && !useCusProfile
                };
                newOutboundItem.Click += SwitchServer;
                serverMenuList.Items.Add(newOutboundItem);
                tagIndex += 1;
            }
            if (profiles.Count + subsOutbounds.Count > 0)
            {
                serverMenuList.Items.Add(new Separator());
                MenuItem newItem = new MenuItem
                {
                    Header = Strings.useall,
                    Tag = useAllServerTag,
                    IsChecked = !useCusProfile && useMultipleServer
                };
                newItem.Click += SwitchServer;
                serverMenuList.Items.Add(newItem);
            }
            if (profiles.Count + subsOutbounds.Count > 0)
            {
                MenuItem newItem = new MenuItem
                {
                    Header = Strings.usepart,
                    IsChecked = !useCusProfile && usePartServer,
                    Tag = usePartServerTag
                };
                newItem.Click += SwitchServer;
                serverMenuList.Items.Add(newItem);
            }
            if (profiles.Count + subsOutbounds.Count > 0)
            {
                serverMenuList.Items.Add(new Separator());
                MenuItem newItem = new MenuItem
                {
                    Header = Strings.speedtest,
                    IsEnabled = speedtestState,
                    ToolTip = Strings.speedtesttip
                };
                newItem.Click += SpeedTest;
                serverMenuList.Items.Add(newItem);
            }
            if (cusProfiles.Count > 0)
            {
                serverMenuList.Items.Add(new Separator());
            }
            foreach (string cusConfigFileName in cusProfiles)
            {
                MenuItem newOutboundItem = new MenuItem
                {
                    Header = "_" + cusConfigFileName.Replace("_", "__"),
                    IsChecked = useCusProfile && cusConfigFileName == selectedCusConfig,
                    Tag = useCusConfigTag
                };
                newOutboundItem.Click += SwitchServer;
                serverMenuList.Items.Add(newOutboundItem);
            }
        }

        void SwitchServer(object sender, RoutedEventArgs e)
        {
            int outboundCount = profiles.Count + subsOutbounds.Count;
            int senderTag = (int)(sender as MenuItem).Tag;
            if (senderTag >= 0 && senderTag < outboundCount)
            {
                useMultipleServer = false;
                useCusProfile = false;

                if (usePartServer)
                {
                    if (selectedPartServerIndex.Count == 1 && selectedPartServerIndex.Exists(x => (int)x == senderTag))
                    {
                        return;
                    }
                    else if (selectedPartServerIndex.Exists(x => (int)x == senderTag))
                    {
                        selectedPartServerIndex.Remove(senderTag);
                    }
                    else
                    {
                        selectedPartServerIndex.Add(senderTag);
                    }
                }
                else
                {
                    selectedServerIndex = senderTag;
                }
            }
            else if (senderTag == useCusConfigTag)
            {
                useMultipleServer = false;
                useCusProfile = true;
                usePartServer = false;
                selectedCusConfig = ((sender as MenuItem).Header as string).Substring(1).Replace("__", "_");
            }
            else if (senderTag == useAllServerTag)
            {
                useCusProfile = false;
                usePartServer = false;
                useMultipleServer = !useMultipleServer;
            }
            else if (senderTag == usePartServerTag)
            {
                useMultipleServer = false;
                useCusProfile = false;
                usePartServer = !usePartServer;
            }
            Debug.WriteLine("switch server");
            this.CoreConfigChanged(sender);
        }

        #endregion

        #region speed test
        BackgroundWorker speedTestWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
        Dictionary<string, string> speedTestResultDic = new Dictionary<string, string>();
        private Semaphore speedTestSemaphore;
        private const string SpeedTestUrl = @"https://www.google.com/generate_204";

        private void SpeedTest(object sender, RoutedEventArgs e)
        {
            speedtestState = false;
            Dispatcher.Invoke(() => { UpdateServerMenuList(speedTestResultDic); });
            speedTestResultDic.Clear();
            speedTestWorker.DoWork += SpeedTestWorker_DoWork;
            speedTestWorker.RunWorkerCompleted += SpeedTestWorker_RunWorkerCompleted;
            if (speedTestWorker.IsBusy)
            {
                return;
            }
            speedTestWorker.RunWorkerAsync();

        }

        private void SpeedTestWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            v2rayJsonConfigTest = GenerateConfigFileTest();
            List<Dictionary<string, object>> allOutbounds = new List<Dictionary<string, object>>(profiles);
            allOutbounds.AddRange(subsOutbounds);
            Debug.WriteLine("task start……");
            List<Task> tasks = new List<Task>();
            speedTestSemaphore = new Semaphore(10, 10);
            int tag = 0;

            Process v2rayProcessTest = new Process();
            v2rayProcessTest.StartInfo.FileName = Utilities.corePath;
            v2rayProcessTest.StartInfo.Arguments = @" -config http://127.0.0.1:18000/test/config.json";
#if DEBUG
            v2rayProcessTest.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
#else
            v2rayProcessTest.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
#endif
            v2rayProcessTest.Start();

            foreach (Dictionary<string, object> outbound in allOutbounds)
            {
                tag++;
                speedTestSemaphore.WaitOne();
                tasks.Add(Task.Run(() =>
                {
                    speedTestResultDic.Add(outbound["tag"].ToString(), ExtUtils.GetHttpStatusTime(SpeedTestUrl, 29527 + tag)); })
                    .ContinueWith(task => { speedTestSemaphore.Release(); })
                    );
                Thread.Sleep(10);
            }
            Task.WaitAll(tasks.ToArray());
            v2rayProcessTest.Kill();
            v2rayProcessTest.Dispose();
            GC.SuppressFinalize(this);
        }

        private void SpeedTestWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            speedtestState = true;
            Dispatcher.Invoke(() => { UpdateServerMenuList(speedTestResultDic); });
            v2rayJsonConfigTest = null;
            Debug.WriteLine("test done ");
            speedTestWorker.DoWork -= SpeedTestWorker_DoWork;
            speedTestWorker.RunWorkerCompleted -= SpeedTestWorker_RunWorkerCompleted;
            ExtUtils.FlushMemory();
        }

        #endregion

        #region simple http server

        private static HttpListener listener = new HttpListener();
        private static BackgroundWorker httpServerWorker = new BackgroundWorker();

        private void InitializeHttpServer()
        {
            listener.Prefixes.Add("http://127.0.0.1:18000/proxy.pac/");
            listener.Prefixes.Add("http://127.0.0.1:18000/config.json/");
            listener.Prefixes.Add("http://127.0.0.1:18000/test/config.json/");
            listener.Start();
            httpServerWorker.WorkerSupportsCancellation = true;
            httpServerWorker.DoWork += new DoWorkEventHandler(HttpServerWorker_DoWork);
            httpServerWorker.RunWorkerAsync();
        }

        private void HttpServerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                Debug.WriteLine("get request {0} {1}", request.Url, DateTime.Now.ToString());
                byte[] respondBytes = new byte[0];
                HttpListenerResponse response = context.Response;
                if (request.Url.AbsolutePath == "/config.json")
                {
                    respondBytes = v2rayJsonConfig;
                    response.ContentType = "application/json; charset=utf-8";
                }
                if (request.Url.AbsolutePath == "/test/config.json")
                {
                    respondBytes = v2rayJsonConfigTest;
                    response.ContentType = "application/json; charset=utf-8";
                }
                else if (request.Url.AbsolutePath.StartsWith("/proxy.pac"))
                {
                    Debug.WriteLine("pac file is requested");
                    // https://support.microsoft.com/en-us/help/4025058/windows-10-does-not-read-a-pac-file-referenced-by-a-file-protocol
                    response.ContentType = "application/x-ns-proxy-autoconfig";
                    if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"pac\" + selectedPacFileName))
                    {
                        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"pac\" + selectedPacFileName, Properties.Resources.simplepac);
                    }
                    respondBytes = File.ReadAllBytes(AppDomain.CurrentDomain.BaseDirectory + @"pac\" + selectedPacFileName);
                }
                // Obtain a response object.
                System.IO.Stream output = response.OutputStream;
                output.Write(respondBytes, 0, respondBytes.Length);
                output.Close();
                Thread.Sleep(0);
            }
        }

        #endregion

        #region system proxy management
        //https://social.msdn.microsoft.com/Forums/vstudio/en-US/19517edf-8348-438a-a3da-5fbe7a46b61a/how-to-change-global-windows-proxy-using-c-net-with-immediate-effect?forum=csharpgeneral
        [DllImport("wininet.dll")]
        static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        const int INTERNET_OPTION_REFRESH = 37;
        const int INTERNET_OPTION_PROXY_SETTINGS_CHANGED = 95;

        public void BackupSystemProxy()
        {
            ;
        }

        public void RestoreSystemProxy()
        {
            ;
        }


        Random paccounter = new Random(); // to force windows refresh pac files

        public void UpdateSystemProxy()
        {
            if (proxyState == false || proxyMode == ProxyMode.manual)
            {
                return;
            }
            RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
            if (proxyMode == ProxyMode.pac)
            {
                registry.SetValue("ProxyEnable", 0);
                registry.SetValue("AutoConfigURL", $"http://127.0.0.1:18000/proxy.pac/{paccounter.Next()}", RegistryValueKind.String);
            }
            else if (proxyMode == ProxyMode.global)
            {
                registry.SetValue("ProxyEnable", 1);
                string proxyServer = $"http://127.0.0.1:{httpPort}";
                registry.SetValue("ProxyServer", proxyServer);
                registry.SetValue("ProxyOverride", byPass);
                registry.DeleteValue("AutoConfigURL", false);
            }
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }

        void CancelSystemProxy()
        {
            RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
            registry.SetValue("ProxyEnable", 0);
            registry.DeleteValue("AutoConfigURL", false);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }

        #endregion

        #region core load/unload management

        private static BackgroundWorker v2rayCoreWorker = new BackgroundWorker();
        private Process v2rayProcess;
        private Semaphore coreWorkerSemaphore = new Semaphore(0, 1);
        private bool coreKilledByMe = false;

        private void InitializeCoreProcess()
        {
            v2rayProcess = new System.Diagnostics.Process();
            v2rayProcess.StartInfo.FileName = Utilities.corePath;
            Debug.WriteLine(v2rayProcess.StartInfo.FileName);
            v2rayProcess.StartInfo.Arguments = @"-config http://127.0.0.1:18000/config.json" + string.Format(@" -confdir {0}\config\confdir", AppDomain.CurrentDomain.BaseDirectory);
#if DEBUG
            v2rayProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
#else
            v2rayProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
#endif
            v2rayCoreWorker.DoWork += V2rayCoreWorker_DoWork;
            v2rayCoreWorker.RunWorkerAsync();
        }

        private void V2rayCoreWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                coreWorkerSemaphore.WaitOne();
                v2rayProcess.Start();
                coreKilledByMe = false;
                v2rayProcess.WaitForExit();
                if (!coreKilledByMe)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        this.OverallChanged(v2rayCoreWorker, null);
                        notifyIcon.ShowBalloonTip("", Strings.messagecorequit, BalloonIcon.Warning);
                    });
                }
                Thread.Sleep(0);
            }
        }

        public void UnloadV2ray()
        {
            try
            {
                coreKilledByMe = true;
                v2rayProcess.Kill();
            }
            catch
            {
            };
        }

        private void ToggleCore()
        {
            this.UnloadV2ray();
            coreWorkerSemaphore.Release(1);
        }
        #endregion

        #region core config management
        byte[] v2rayJsonConfig = new byte[0];
        public byte[] v2rayJsonConfigTest = new byte[0];

        private void CoreConfigChanged(object sender)
        {
            Debug.WriteLine($"{sender} calls config change");
            if (proxyState == true)
            {
                if (!useMultipleServer && !usePartServer && useCusProfile)
                {
                    v2rayJsonConfig = File.ReadAllBytes(AppDomain.CurrentDomain.BaseDirectory + @"config\" + selectedCusConfig);
                }
                else
                {
                    v2rayJsonConfig = this.GenerateConfigFile();
                }
                this.ToggleCore();
            }
            this.UpdateServerMenuList(speedTestResultDic);
            this.UpdateRuleSetMenuList();
            ExtUtils.FlushMemory();
        }

        private byte[] GenerateConfigFile()
        {
            Dictionary<string, object> fullConfig = Utilities.configTemplate;
            fullConfig["log"] = new Dictionary<string, string>
            {
#if DEBUG
                {"loglevel", "debug" }
#else
                { "error",AppDomain.CurrentDomain.BaseDirectory + @"log\error.log" },
                { "access", AppDomain.CurrentDomain.BaseDirectory + @"log\access.log"},
                {"loglevel", logLevel }
#endif
            };
            string[] dnsList = dnsString.Split(',');
            if (dnsList.Count() > 0)
            {
                fullConfig["dns"] = new Dictionary<string, string[]>
                {
                    { "servers", dnsList }
                };
            }
            fullConfig["inbounds"] = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "port", localPort },
                    { "listen", shareOverLan ? @"0.0.0.0" : @"127.0.0.1" },
                    {
                        "settings",
                        new Dictionary<string, object> { {"udp", udpSupport} }
                    },
                    { "protocol", "socks" }
                },
                new Dictionary<string, object>
                {
                    { "port", httpPort },
                    { "listen", shareOverLan ? @"0.0.0.0" : @"127.0.0.1" },
                    { "protocol", "http" }
                }
            };

            List<Dictionary<string, object>> allOutbounds = new List<Dictionary<string, object>>(profiles);
            allOutbounds.AddRange(subsOutbounds);

            Dictionary<string, object> outboundsForConfig = new Dictionary<string, object>();
            Dictionary<string, object> uniqueTagOutbounds = new Dictionary<string, object>();
            List<string> exceptTagOutbounds = new List<string>();
            foreach (Dictionary<string, object> outbound in allOutbounds)
            {
                uniqueTagOutbounds[outbound[@"tag"].ToString()] = outbound;
                if (usePartServer && !selectedPartServerIndex.Contains(allOutbounds.IndexOf(outbound)))
                {
                    exceptTagOutbounds.Add(outbound[@"tag"].ToString());
                }
            }
            List<object> uniqueTagsExceptDirectDecline = new List<object>(uniqueTagOutbounds.Keys);
            uniqueTagOutbounds["direct"] = Utilities.OUTBOUND_DIRECT;
            uniqueTagOutbounds["decline"] = Utilities.OUTBOUND_DECLINE;

            fullConfig["routing"] = Utilities.DeepClone(routingRuleSets[selectedRoutingSet]);
            IList<object> currentRules = (fullConfig["routing"] as Dictionary<string, object>)["rules"] as IList<object>;
            foreach (Dictionary<string, object> aRule in currentRules)
            {
                if (uniqueTagOutbounds.ContainsKey(aRule["outboundTag"].ToString()))
                {
                    uniqueTagsExceptDirectDecline.Remove(aRule["outboundTag"].ToString());
                    exceptTagOutbounds.Remove(aRule["outboundTag"].ToString());
                }
                if (aRule.ContainsKey("outboundTag") && aRule["outboundTag"].ToString() == "main")
                {
                    if (!useMultipleServer && !usePartServer)
                    {
                        aRule["outboundTag"] = allOutbounds[selectedServerIndex][@"tag"].ToString();
                    }
                    else
                    {
                        aRule.Remove("outboundTag");
                        aRule["balancerTag"] = "balance";
                    }
                }
            }
            
            bool useBalance = false;
            foreach (Dictionary<string, object> aRule in currentRules)
            {
                if (aRule.ContainsKey("balancerTag") && !aRule.ContainsKey("outboundTag"))
                {
                    useBalance = true;
                    break;
                }
                else
                {
                    if (uniqueTagOutbounds.ContainsKey(aRule["outboundTag"].ToString()))
                    {
                        outboundsForConfig[aRule["outboundTag"].ToString()] = uniqueTagOutbounds[aRule["outboundTag"].ToString()];
                    }
                }
            }
            if (useBalance)
            {
                foreach (string except in exceptTagOutbounds.ToArray())
                {
                    uniqueTagOutbounds.Remove(except);
                    uniqueTagsExceptDirectDecline.Remove(except);
                }
                (fullConfig["routing"] as Dictionary<string, object>).Add("balancers", new List<object>
                {
                    new Dictionary<string, object>
                    {
                        { "tag", "balance" },
                        { "selector", uniqueTagsExceptDirectDecline }
                    }
                }
                );
                fullConfig["outbounds"] = uniqueTagOutbounds.Values;
            }
            else
            {
                fullConfig["outbounds"] = outboundsForConfig.Values;
            }
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(fullConfig, Formatting.Indented));
        }

        private byte[] GenerateConfigFileTest()
        {
            Dictionary<string, object> fullConfig = Utilities.configTemplate;
            List<Dictionary<string, object>> allOutbounds = new List<Dictionary<string, object>>(profiles);
            allOutbounds.AddRange(subsOutbounds);
            fullConfig["log"] = new Dictionary<string, string>
            {
                { "loglevel", "none" },
                { "error", "none" },
                { "access", "none" }
            };
            fullConfig["dns"] = new Dictionary<string, object>
            {
                { "servers", new List<string> { "8.8.8.8" } }
            };
            List<Dictionary<string, object>> allInbounds = new List<Dictionary<string, object>>();
            Dictionary<string, object> allRouting = new Dictionary<string, object>
                    {
                        { @"domainStrategy", @"AsIs" }
                    };
            List<Dictionary<string, object>> allRule = new List<Dictionary<string, object>>();
            int tag = 0;
            foreach (Dictionary<string, object> od in allOutbounds)
            {
                tag++;
                allInbounds.Add(new Dictionary<string, object>
                {
                    { "port", 29527 + tag },
                    { "listen",  @"127.0.0.1" },
                    { "protocol", "http" },
                    { "tag", od["tag"] }
                });
                allRule.Add(new Dictionary<string, object>
                {
                    { "type", "field" },
                    { "inboundTag", new List<string> { od["tag"] as string } },
                    { "outboundTag", od["tag"] },
                });
            }
            allRouting["rules"] = allRule;
            fullConfig["inbounds"] = allInbounds;
            fullConfig["outbounds"] = allOutbounds;
            fullConfig["routing"] = allRouting;
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(fullConfig, Formatting.Indented));
        }
        #endregion

        #region routingRules 
        void UpdateRuleSetMenuList()
        {
            Debug.WriteLine($"rules count = {routingRuleSets.Count}");
            MenuItem ruleSetMenuItem = mainMenu.Items[routingRuleSetMenuItemIndex] as MenuItem;
            ruleSetMenuItem.Items.Clear();
            int i = 0;
            foreach (Dictionary<string, object> rule in routingRuleSets)
            {
                MenuItem menuItem = new MenuItem
                {
                    Tag = i,
                    Header = (rule["name"] as string).Replace("_", "__"),
                    IsCheckable = true,
                    IsChecked = i == selectedRoutingSet,
                };
                Debug.WriteLine(menuItem.Tag.ToString());
                menuItem.Click += SwitchRoutingRuleSet;
                ruleSetMenuItem.Items.Add(menuItem);
                i += 1;
            }
        }

        void SwitchRoutingRuleSet(object sender, RoutedEventArgs e)
        {
            selectedRoutingSet = (int)(sender as MenuItem).Tag;
            Debug.WriteLine("switch routing rule set");
            this.CoreConfigChanged(sender);
        }

        #endregion

        #region Taskbar ContextMenu
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            base.OnClosing(e);
            PageFrame.Content = null;
            Toggle.DataContext = null;
            ExtUtils.FlushMemory();
        }
        public void QuitScream(object sender, RoutedEventArgs e)
        {
            notifyIcon.Icon = null;
            this.UnloadV2ray();
            if (proxyState && proxyMode != ProxyMode.manual)
            {
                if (enableRestore)
                {
                    RestoreSystemProxy();
                }
                else
                {
                    CancelSystemProxy();
                }
            }
            this.WriteSettings();
            pacFileWatcher.EnableRaisingEvents = false;
            cusFileWatcher.EnableRaisingEvents = false;
            confdirFileWatcher.EnableRaisingEvents = false;
            ExtUtils ext = new ExtUtils();
            if (autoStart)
            {
                ext.SetMeAutoStart();
            }
            else
            {
                ext.SetMeAutoStart(false);
            }
            Application.Current.Shutdown();
        }
        private void ShowHelp(object sender, RoutedEventArgs e)
        {
            Process.Start(Scream.Resources.Strings.V2RayHomePage);
        }

        private void ShowWindow(object sender, RoutedEventArgs e)
        {
            Outbounds outbounds = new Outbounds { mainWindow = this };
            outbounds.InitializeData();
            PageFrame.Content = outbounds;
            Major.SelectionChanged += NavigateMajor_SelectionChanged;
            Major.SelectedIndex = 0;

            Toggle.DataContext = toggle;
            toggle.ColorScheme = colorScheme;
            toggle.AutoStart = autoStart;
            toggle.UDPSupport = udpSupport;
            toggle.ShareOverLan = shareOverLan;
            ResourceLocator.SetColorScheme(Application.Current.Resources, !toggle.ColorScheme ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);

            Show();
            Activate();
        }

        private void ViewCurrentConfig(object sender, RoutedEventArgs e)
        {
            Process.Start("http://127.0.0.1:18000/config.json");
        }

        private void ShowLogMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(AppDomain.CurrentDomain.BaseDirectory + @"log\");
        }

        private void ResetPacButton_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"pac\pac.js", Properties.Resources.simplepac);
            UpdatePacMenuList();
        }
        #endregion
    }
}
