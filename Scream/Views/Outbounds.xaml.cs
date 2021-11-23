using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json;
using Scream.Models;
using Scream.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Controls;

namespace Scream.Views
{
    /// <summary>
    /// Servers.xaml 的交互逻辑
    /// </summary>
    public partial class Outbounds : Page
    {
        public ObservableCollection<OutboundSummary> OutboundsList = new ObservableCollection<OutboundSummary>();
        public Outbound Json = new Outbound();
        public MainWindow mainWindow;
        public Outbounds()
        {
            InitializeComponent();
            ListBoxOutbounds.ItemsSource = OutboundsList;
            TextBoxJson.DataContext = Json;
        }
        public void InitializeData()
        {
            foreach (Dictionary<string, object> outbound in mainWindow.profiles)
            {
                OutboundsList.Add(new OutboundSummary { Protocol = outbound["protocol"] as string, Tag = outbound["tag"] as string });
            }
            ListBoxOutbounds.SelectionChanged += ListBoxOutbounds_SelectionChanged;
        }

        private void ListBoxOutbounds_SelectionChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ListBoxOutbounds.SelectedIndex >= 0 && ListBoxOutbounds.SelectedIndex < mainWindow.profiles.Count)
            {
                Json.OutboundJson = JsonConvert.SerializeObject(mainWindow.profiles[ListBoxOutbounds.SelectedIndex], Formatting.Indented);
            }
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ListBoxOutbounds.SelectedItems.Count <= 0)
            {
                return;
            }
            if (sender == ButtonSave)
            {
                switch (mainWindow.profiles[ListBoxOutbounds.SelectedIndex]["protocol"])
                {
                    case "vmess":
                        Utilities.AddMissingKeysForVmess(mainWindow.profiles[ListBoxOutbounds.SelectedIndex]);
                        break;
                    case "vless":
                        Utilities.AddMissingKeysForVless(mainWindow.profiles[ListBoxOutbounds.SelectedIndex]);
                        break;
                    case "shadowsocks":
                        Utilities.AddMissingKeysForShadowsocks(mainWindow.profiles[ListBoxOutbounds.SelectedIndex]);
                        break;
                    case "trojan":
                        Utilities.AddMissingKeysForTrojan(mainWindow.profiles[ListBoxOutbounds.SelectedIndex]);
                        break;
                    default:
                        break;
                }
                Dictionary<string, object> outbound;
                try
                {
                    outbound = Utilities.javaScriptSerializer.Deserialize<dynamic>(Json.OutboundJson) as Dictionary<string, object>;
                }
                catch (System.Exception)
                {
                    mainWindow.notifyIcon.ShowBalloonTip("", Strings.messagenotvalidjson, BalloonIcon.None);
                    return;
                }
                if (Utilities.RESERVED_TAGS.FindIndex(x => x == outbound["tag"] as string) != -1)
                {
                    mainWindow.notifyIcon.ShowBalloonTip(Strings.messagetag + $" {outbound["tag"]} ", Strings.messagereserved, BalloonIcon.None);
                    return;
                }
                if (OutboundsList.Count(x => x.Tag == outbound["tag"].ToString()) >= 1 && OutboundsList[ListBoxOutbounds.SelectedIndex].Tag != outbound["tag"] as string)
                {
                    mainWindow.notifyIcon.ShowBalloonTip(Strings.messagetag + $" {outbound["tag"]} ", Strings.messagenotunique, BalloonIcon.None);
                    return;
                }
                //test outbound
                Dictionary<string, object> outbounds = new Dictionary<string, object> {
                    { "outbounds" ,new List<object>
                        {
                        outbound
                        }
                    }
                };
                mainWindow.v2rayJsonConfigTest = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(outbounds, Formatting.Indented));
                Process v2rayProcess = new Process();
                v2rayProcess.StartInfo.FileName = Utilities.corePath;
                v2rayProcess.StartInfo.Arguments = "-test -config http://127.0.0.1:18000/test/config.json";
                v2rayProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                try
                {
                    v2rayProcess.Start();
                    v2rayProcess.WaitForExit();
                    if (v2rayProcess.ExitCode != 0)
                    {
                        mainWindow.notifyIcon.ShowBalloonTip("", Strings.messagenotvalidoutbound, BalloonIcon.None);
                        return;
                    }
                }
                catch{};
                v2rayProcess.Dispose();
                GC.SuppressFinalize(this);
                mainWindow.v2rayJsonConfigTest = null;
                mainWindow.profiles[ListBoxOutbounds.SelectedIndex] = outbound;
                int index = ListBoxOutbounds.SelectedIndex;
                OutboundsList[ListBoxOutbounds.SelectedIndex] = new OutboundSummary { Protocol = mainWindow.profiles[ListBoxOutbounds.SelectedIndex]["protocol"].ToString(), Tag = mainWindow.profiles[ListBoxOutbounds.SelectedIndex]["tag"].ToString() };
                ListBoxOutbounds.SelectedIndex = index;
            }
            if (sender == ButtonDelete)
            {
                List<int> selectedItemIndexes = new List<int>();
                foreach (OutboundSummary id in ListBoxOutbounds.SelectedItems)
                {
                    selectedItemIndexes.Add(OutboundsList.IndexOf(id));
                }
                for (int r = selectedItemIndexes.Count - 1; r >= 0; r--)
                {
                    mainWindow.profiles.RemoveAt(selectedItemIndexes.Max());
                    OutboundsList.RemoveAt(selectedItemIndexes.Max());
                    selectedItemIndexes.Remove(selectedItemIndexes.Max());
                }
            }
        }

        private void MenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender == MenuVmess && UniqueDetection("vmess outbound"))
            {
                OutboundsList.Add(new OutboundSummary { Protocol = "vmess", Tag = "vmess outbound" });
                mainWindow.profiles.Add(Utilities.VmessOutboundTemplate());
            }
            if (sender == MenuVless && UniqueDetection("vless outbound"))
            {
                OutboundsList.Add(new OutboundSummary { Protocol = "vless", Tag = "vless outbound" });
                mainWindow.profiles.Add(Utilities.VlessOutboundTemplate());
            }
            if (sender == MenuTrojan && UniqueDetection("trojan outbound"))
            {
                OutboundsList.Add(new OutboundSummary { Protocol = "trojan", Tag = "trojan outbound" });
                mainWindow.profiles.Add(Utilities.TrojanOutboundJson());
            }
            if (sender == MenuShadowsocks && UniqueDetection("shadowsocks outbound"))
            {
                OutboundsList.Add(new OutboundSummary { Protocol = "shadowsocks", Tag = "shadowsocks outbound" });
                mainWindow.profiles.Add(Utilities.ShadowsocksOutboundJson());
            }
            if (sender == MenuOthers && UniqueDetection("others outbound"))
            {
                OutboundsList.Add(new OutboundSummary { Protocol = "Others", Tag = "others outbound" });
                mainWindow.profiles.Add(Utilities.outboundTemplate);
            }
        }

        private bool UniqueDetection(string outboundname)
        {
            bool unique = true;
            if (OutboundsList.Count(x => x.Tag == outboundname) != 0)
            {
                unique = false;
            }
            return unique;
        }

        private void Import_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender == ImportClipboard)
            {
                try
                {
                    foreach (OutboundSummary x in ImportURL(ExtUtils.GetClipboardData()))
                    {
                        OutboundsList.Add(x);
                    }
                }
                catch (Exception)
                {
                    mainWindow.notifyIcon.ShowBalloonTip("", Strings.messageformatexception, BalloonIcon.None);
                    return;
                }
            }
            if (sender == ImportSubscription)
            {
                if (mainWindow.profiles.Count != 0)
                {
                    foreach (string ps in mainWindow.subscriptionTag)
                    {
                        foreach (Dictionary<string, object> tag in mainWindow.profiles.ToArray())
                        {
                            if (tag["tag"].ToString() == ps)
                            {
                                mainWindow.profiles.Remove(tag);
                            }
                        }
                        foreach (OutboundSummary ol in OutboundsList.ToArray())
                        {
                            if (ol.Tag == ps)
                            {
                                OutboundsList.Remove(ol);
                            }
                        }
                    }
                }
                try
                {
                    BackgroundWorker subscriptionWorker = new BackgroundWorker();
                    subscriptionWorker.WorkerSupportsCancellation = true;
                    subscriptionWorker.DoWork += SubscriptionWorker_DoWork;
                    if (subscriptionWorker.IsBusy) return;
                    subscriptionWorker.RunWorkerAsync();
                }
                catch (Exception)
                {
                    mainWindow.notifyIcon.ShowBalloonTip("", Strings.messagerequesttimeout, BalloonIcon.None);
                    return;
                }
            }

        }
        private void Export_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            List<string> URL = new List<string>();
            foreach (OutboundSummary id in ListBoxOutbounds.SelectedItems)
            {
                URL.Add(ExportURL(OutboundsList.IndexOf(id)));
            }
            ExtUtils.SetClipboardData(string.Join("\r\n", URL));
        }

        #region import & subscription &export

        public string ExportURL(int index)
        {
            if (mainWindow.profiles[index]["protocol"] as string == "vless" || mainWindow.profiles[index]["protocol"] as string == "vmess")
            {
                return ExportVless(index);
            }
            if (mainWindow.profiles[index]["protocol"] as string == "shadowsocks")
            {
                return ExportShadowsocks(index);
            }
            if (mainWindow.profiles[index]["protocol"] as string == "trojan")
            {
                return ExportTrojan(index);
            }
            return null;
        }
        public string ExportShadowsocks(int index)
        {
            Dictionary<string, object> ShadowsocksProfiles = mainWindow.profiles[index];
            Dictionary<string, object> settings = ShadowsocksProfiles["settings"] as Dictionary<string, object>;
            Dictionary<string, object> servers = (settings["servers"] as IList<object>)[0] as Dictionary<string, object>;
            string serverinfo = string.Format("{0}:{1}@{2}:{3}", servers["method"], servers["password"], servers["address"], servers["port"]);
            return string.Format(@"ss://{0}#{1}", ExtUtils.Base64Encode(serverinfo), HttpUtility.UrlEncode((string)ShadowsocksProfiles["tag"]));
        }
        public string ExportTrojan(int index)
        {
            Dictionary<string, object> TrojanProfiles = mainWindow.profiles[index];
            Dictionary<string, object> settings = TrojanProfiles["settings"] as Dictionary<string, object>;
            Dictionary<string, object> servers = (settings["servers"] as IList<object>)[0] as Dictionary<string, object>;
            return string.Format(@"trojan://{0}@{1}:{2}#{3}", servers["password"], servers["address"], servers["port"], HttpUtility.UrlEncode(TrojanProfiles["tag"] as string));
        }
        public string ExportVless(int index)
        {
            Dictionary<string, object> selectedProfile = mainWindow.profiles[index];
            Dictionary<string, object> selectedVnext = ((selectedProfile["settings"] as Dictionary<string, object>)["vnext"] as IList<object>)[0] as Dictionary<string, object>;
            Dictionary<string, object> selectedUserInfo = (selectedVnext["users"] as IList<object>)[0] as Dictionary<string, object>;
            Dictionary<string, object> streamSettings = selectedProfile["streamSettings"] as Dictionary<string, object>;
            Dictionary<string, object> kcpSettings = streamSettings["kcpSettings"] as Dictionary<string, object>;
            Dictionary<string, object> kcpSettingsT = kcpSettings["header"] as Dictionary<string, object>;
            Dictionary<string, object> tcpSettings = streamSettings["tcpSettings"] as Dictionary<string, object>;
            Dictionary<string, object> tcpSettingsT = tcpSettings["header"] as Dictionary<string, object>;
            Dictionary<string, object> wsSettings = streamSettings["wsSettings"] as Dictionary<string, object>;
            Dictionary<string, object> wsSettingsT = wsSettings["headers"] as Dictionary<string, object>;
            Dictionary<string, object> httpSettings = streamSettings["httpSettings"] as Dictionary<string, object>;
            Dictionary<string, object> quicSettings = streamSettings["quicSettings"] as Dictionary<string, object>;
            Dictionary<string, object> quicSettingsT = quicSettings["header"] as Dictionary<string, object>;
            Dictionary<string, object> grpcSettings = streamSettings["grpcSettings"] as Dictionary<string, object>;
            string generateurl = selectedProfile["protocol"].ToString() + "://" + HttpUtility.UrlEncode(selectedUserInfo["id"].ToString()) + "@" + selectedVnext["address"].ToString() + ":" + selectedVnext["port"].ToString();
            List<string> specific = new List<string>();
            specific.Add(streamSettings["network"].ToString() != "tcp" ? "type=" + streamSettings["network"].ToString() : null);
            specific.Add(selectedProfile["protocol"].ToString() == "vmess" && selectedUserInfo["security"].ToString() != "auto" ? "encryption=" + selectedUserInfo["security"].ToString() : null);
            specific.Add(selectedProfile["protocol"].ToString() == "vless" && selectedUserInfo.ContainsKey("encryption") && selectedUserInfo["encryption"].ToString() != "none" ? "encryption=" + selectedUserInfo["encryption"].ToString() : null);
            specific.Add(streamSettings["security"].ToString() != "none" ? "security=" + streamSettings["security"].ToString() : null);
            switch (streamSettings["network"])
            {
                case "ws":
                    specific.Add("path=" + HttpUtility.UrlEncode(wsSettings["path"].ToString()));
                    specific.Add("host=" + wsSettingsT["host"].ToString());
                    break;
                case "http":
                    specific.Add("path=" + httpSettings["path"].ToString());
                    specific.Add("host=" + HttpUtility.UrlEncode(String.Join(",", httpSettings["host"])));
                    break;
                case "kcp":
                    specific.Add(kcpSettingsT["type"].ToString() != "none" ? "headerType=" + kcpSettingsT["type"].ToString() : null);
                    specific.Add(kcpSettings.ContainsKey("seed") ? "seed=" + HttpUtility.UrlEncode(kcpSettings["seed"].ToString()) : null);
                    break;
                case "quic":
                    specific.Add(quicSettings["security"].ToString() != "none" ? "quicSecurity=" + quicSettings["security"].ToString() : null);
                    specific.Add(quicSettings["security"].ToString() != "none" ? "key=" + HttpUtility.UrlEncode(quicSettings["key"].ToString()) : null);
                    specific.Add(quicSettingsT["type"].ToString() != "none" ? "headerType=" + quicSettingsT["type"].ToString() : null);
                    break;
                case "grpc":
                    specific.Add("serviceName=" + grpcSettings["serviceName"].ToString());
                    break;
                default:
                    break;
            }

            if (selectedProfile["protocol"] as string == "vless" && selectedUserInfo.ContainsKey("flow") && selectedUserInfo["flow"] as string != "")
            {
                specific.Add((streamSettings["xtlsSettings"] as Dictionary<string, object>)["serverName"].ToString() != selectedVnext["address"].ToString() ? "sni=" + (streamSettings["xtlsSettings"] as Dictionary<string, object>)["serverName"].ToString() : null);
                specific.Add(string.Join(",", (streamSettings["xtlsSettings"] as Dictionary<string, object>)["alpn"] as IList<object>) != @"h2,http/1.1" ? "alpn=" + HttpUtility.UrlEncode(String.Join(",", (streamSettings["xtlsSettings"] as Dictionary<string, object>)["alpn"] as IList<object>)) : null);
                specific.Add("flow=" + selectedUserInfo["flow"].ToString());
            }
            else
            {
                specific.Add((streamSettings["tlsSettings"] as Dictionary<string, object>)["serverName"].ToString() != selectedVnext["address"].ToString() ? "sni=" + (streamSettings["tlsSettings"] as Dictionary<string, object>)["serverName"].ToString() : null);
                specific.Add(string.Join(",", (streamSettings["tlsSettings"] as Dictionary<string, object>)["alpn"] as IList<object>) != @"h2,http/1.1" ? "alpn=" + HttpUtility.UrlEncode(String.Join(",", (streamSettings["tlsSettings"] as Dictionary<string, object>)["alpn"] as IList<object>)) : null);
            }
            specific.RemoveAll(x => x == null);
            if (specific.Count > 0)
            {
                generateurl += "?" + string.Join("&", specific);
            }
            generateurl += "#" + HttpUtility.UrlEncode(selectedProfile["tag"].ToString());
            return generateurl;
        }

        private void SubscriptionWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            mainWindow.subscriptionTag.Clear();
            List<Task> tasks = new List<Task>();
            foreach (string url in mainWindow.subscriptionUrl)
            {
                tasks.Add(Task.Run(() =>
                {
                    foreach (OutboundSummary x in ImportURL(ExtUtils.Base64Decode(ExtUtils.GetUrl(url))))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            OutboundsList.Add(x);
                        });
                        mainWindow.subscriptionTag.Add(x.Tag);
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());
        }

        public List<OutboundSummary> ImportURL(string importUrl)
        {
            List<OutboundSummary> TAG = new List<OutboundSummary>();
            foreach (string link in importUrl.Split(Environment.NewLine.ToCharArray()))
            {
                if (link.StartsWith("ss"))
                {
                    TAG.Add(new OutboundSummary { Protocol = "shadowsocks", Tag = ImportShadowsocks(link) });
                }
                if (link.StartsWith("trojan"))
                {
                    TAG.Add(new OutboundSummary { Protocol = "trojan", Tag = ImportTrojan(link) });
                }
                if (link.StartsWith("vmess"))
                {

                    if (link.Contains("#"))
                    {
                        TAG.Add(new OutboundSummary { Protocol = "vmess", Tag = ImportVless(Utilities.VmessOutboundTemplate(), link) });
                    }
                    else
                    {
                        TAG.Add(new OutboundSummary { Protocol = "vmess", Tag = ImportVmess(link) });
                    }
                }
                if (link.StartsWith("vless"))
                {
                    TAG.Add(new OutboundSummary { Protocol = "vless", Tag = ImportVless(Utilities.VlessOutboundTemplate(), link) });
                }
            }
            return TAG;
        }

        public string ImportShadowsocks(string url)
        {
            string link = url.Contains("#") ? url.Substring(5, url.IndexOf("#") - 5) : url.Substring(5);
            string tag = url.Contains("#") ? url.Substring(url.IndexOf("#") + 1).Trim() : "shadowsocks-" + new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000);
            string[] linkParseArray = ExtUtils.Base64Decode(link).Split(new char[2] { ':', '@' });
            Dictionary<string, object> ShadowsocksProfiles = Utilities.outboundTemplate;
            Dictionary<string, object> ShadowsocksTemplate = Utilities.ShadowsocksOutboundTemplateNew();
            ShadowsocksProfiles["protocol"] = "shadowsocks";
            ShadowsocksProfiles["tag"] = HttpUtility.UrlDecode(tag);
            ShadowsocksTemplate["email"] = "love@server.cc";
            ShadowsocksTemplate["address"] = linkParseArray[2];
            ShadowsocksTemplate["port"] = Convert.ToInt32(linkParseArray[3]);
            ShadowsocksTemplate["method"] = linkParseArray[0];
            ShadowsocksTemplate["password"] = linkParseArray[1];
            ShadowsocksTemplate["ota"] = false;
            ShadowsocksTemplate["level"] = 0;
            ShadowsocksProfiles["settings"] = new Dictionary<string, object> {
                    {"servers",  new List<Dictionary<string, object>>{ ShadowsocksTemplate } }
                };
            mainWindow.profiles.Add(Utilities.DeepClone(ShadowsocksProfiles));
            return HttpUtility.UrlDecode(tag);
        }
        public string ImportTrojan(string url)
        {
            Dictionary<string, object> TrojanProfiles = Utilities.TrojanOutboundJson();
            Dictionary<string, object> settings = TrojanProfiles["settings"] as Dictionary<string, object>;
            Dictionary<string, object> servers = (settings["servers"] as IList<object>)[0] as Dictionary<string, object>;

            string link = url.Contains("#") ? url.Substring(9, url.IndexOf("#") - 9) : url.Substring(9);
            string tag = url.Contains("#") ? url.Substring(url.IndexOf("#") + 1).Trim() : "trojan-" + new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000);
            string[] linkParseArray = link.Split(new char[2] { ':', '@' });

            TrojanProfiles["protocol"] = "trojan";
            TrojanProfiles["tag"] = HttpUtility.UrlDecode(tag);
            servers["address"] = linkParseArray[1];
            servers["port"] = Convert.ToInt32(linkParseArray[2]);
            servers["password"] = linkParseArray[0];
            servers["ota"] = false;
            servers["level"] = 0;
            mainWindow.profiles.Add(Utilities.DeepClone(TrojanProfiles));
            return HttpUtility.UrlDecode(tag);
        }
        public string ImportVmess(string url)
        {
            Dictionary<string, object> VmessProfiles = Utilities.VmessOutboundTemplateNew();
            Dictionary<string, object> streamSettings = VmessProfiles["streamSettings"] as Dictionary<string, object>;
            Dictionary<string, object> settings = VmessProfiles["settings"] as Dictionary<string, object>;
            Dictionary<string, object> vnext = (settings["vnext"] as IList<object>)[0] as Dictionary<string, object>;
            Dictionary<string, object> UserInfo = (vnext["users"] as IList<object>)[0] as Dictionary<string, object>;
            Dictionary<string, object> kcpSettings = streamSettings["kcpSettings"] as Dictionary<string, object>;
            Dictionary<string, object> kcpSettingsT = kcpSettings["header"] as Dictionary<string, object>;
            Dictionary<string, object> tcpSettings = streamSettings["tcpSettings"] as Dictionary<string, object>;
            Dictionary<string, object> tcpSettingsT = tcpSettings["header"] as Dictionary<string, object>;
            Dictionary<string, object> wsSettings = streamSettings["wsSettings"] as Dictionary<string, object>;
            Dictionary<string, object> wsSettingsT = wsSettings["headers"] as Dictionary<string, object>;
            Dictionary<string, object> httpSettings = streamSettings["httpSettings"] as Dictionary<string, object>;
            Dictionary<string, object> quicSettings = streamSettings["quicSettings"] as Dictionary<string, object>;
            Dictionary<string, object> quicSettingsT = quicSettings["header"] as Dictionary<string, object>;
            Dictionary<string, object> grpcSettings = streamSettings["grpcSettings"] as Dictionary<string, object>;
            Dictionary<string, object> tlsSettings = streamSettings["tlsSettings"] as Dictionary<string, object>;

            VmessLink VmessLink = JsonConvert.DeserializeObject<VmessLink>(ExtUtils.Base64Decode(url.Substring(8)));
            UserInfo["id"] = VmessLink.id;
            UserInfo["alterId"] = Convert.ToInt32(VmessLink.aid);
            UserInfo["security"] = VmessLink.scy ?? "auto";
            vnext["address"] = VmessLink.add;
            vnext["port"] = Convert.ToInt32(VmessLink.port);
            streamSettings["network"] = VmessLink.net == "h2" ? "http" : VmessLink.net;
            streamSettings["security"] = VmessLink.tls == "" ? "none" : VmessLink.tls;
            tlsSettings["serverName"] = VmessLink.sni ?? VmessLink.add;
            VmessProfiles["tag"] = HttpUtility.UrlDecode(VmessLink.ps);
            switch (VmessLink.net)
            {
                case "ws":
                    wsSettingsT["host"] = VmessLink.host;
                    wsSettings["path"] = VmessLink.path;
                    break;
                case "h2":
                    httpSettings["host"] = VmessLink.host.Split(',');
                    httpSettings["path"] = VmessLink.path;
                    break;
                case "tcp":
                    tcpSettingsT["type"] = VmessLink.type;
                    break;
                case "kcp":
                    kcpSettingsT["type"] = VmessLink.type;
                    break;
                case "quic":
                    quicSettingsT["type"] = VmessLink.type;
                    quicSettings["securty"] = VmessLink.host;
                    quicSettings["key"] = VmessLink.path;
                    break;
                case "grpc":
                    grpcSettings["serviceName"] = VmessLink.path;
                    tlsSettings["alpn"] = new string[] { @"h2" };
                    break;
                default:
                    break;
            }
            mainWindow.profiles.Add(VmessProfiles);
            return HttpUtility.UrlDecode(VmessLink.ps);
        }

        public string ImportVless(Dictionary<string, object> template, string url)
        {
            Dictionary<string, object> templateProfiles = template;
            Dictionary<string, object> streamSettings = templateProfiles["streamSettings"] as Dictionary<string, object>;
            Dictionary<string, object> settings = templateProfiles["settings"] as Dictionary<string, object>;
            Dictionary<string, object> vnext = (settings["vnext"] as IList<object>)[0] as Dictionary<string, object>;
            Dictionary<string, object> UserInfo = (vnext["users"] as IList<object>)[0] as Dictionary<string, object>;
            Dictionary<string, object> kcpSettings = streamSettings["kcpSettings"] as Dictionary<string, object>;
            Dictionary<string, object> kcpSettingsT = kcpSettings["header"] as Dictionary<string, object>;
            Dictionary<string, object> tcpSettings = streamSettings["tcpSettings"] as Dictionary<string, object>;
            Dictionary<string, object> tcpSettingsT = tcpSettings["header"] as Dictionary<string, object>;
            Dictionary<string, object> wsSettings = streamSettings["wsSettings"] as Dictionary<string, object>;
            Dictionary<string, object> wsSettingsT = wsSettings["headers"] as Dictionary<string, object>;
            Dictionary<string, object> httpSettings = streamSettings["httpSettings"] as Dictionary<string, object>;
            Dictionary<string, object> quicSettings = streamSettings["quicSettings"] as Dictionary<string, object>;
            Dictionary<string, object> quicSettingsT = quicSettings["header"] as Dictionary<string, object>;
            Dictionary<string, object> grpcSettings = streamSettings["grpcSettings"] as Dictionary<string, object>;
            Dictionary<string, object> tlsSettings = streamSettings["tlsSettings"] as Dictionary<string, object>;

            List<string> linkParseArray = url.Substring(8).Split(new char[6] { ':', '@', '?', '&', '#', '=' }).ToList();
            templateProfiles["tag"] = HttpUtility.UrlDecode(linkParseArray[linkParseArray.Count - 1]);
            UserInfo["id"] = linkParseArray[0];
            vnext["address"] = linkParseArray[1];
            vnext["port"] = Convert.ToInt32(linkParseArray[2]);
            streamSettings["network"] = linkParseArray.Contains("type") ? linkParseArray[linkParseArray.IndexOf("type") + 1] : "tcp";
            streamSettings["security"] = linkParseArray.Contains("security") ? linkParseArray[linkParseArray.IndexOf("security") + 1] : "none";
            if (linkParseArray.Contains("encryption"))
            {
                string encryption = linkParseArray[linkParseArray.IndexOf("encryption") + 1];
                if (url.StartsWith("vmess"))
                {
                    UserInfo["security"] = encryption;
                }
                if (url.StartsWith("vless"))
                {
                    UserInfo["encryption"] = encryption;
                }
            }
            tlsSettings["serverName"] = linkParseArray.Contains("sni") ? linkParseArray[linkParseArray.IndexOf("sni") + 1] : linkParseArray[1];
            if (linkParseArray.Contains("alpn"))
            {
                tlsSettings["alpn"] = new string[] { HttpUtility.UrlDecode(linkParseArray[linkParseArray.IndexOf("alpn") + 1]) };
            }
            else
            {
                tlsSettings["alpn"] = new string[] { @"h2", @"http/1.1" };
            }
            if (linkParseArray.Contains("flow"))
            {
                UserInfo["flow"] = linkParseArray[linkParseArray.IndexOf("flow") + 1];
                templateProfiles["streamSettings"] = streamSettings.ToDictionary(k => k.Key == "tlsSettings" ? "xtlsSettings" : k.Key, k => k.Value);
            }
            switch (streamSettings["network"])
            {
                case "ws":
                    wsSettingsT["host"] = linkParseArray.Contains("host") ? linkParseArray[linkParseArray.IndexOf("host") + 1] : linkParseArray[1];
                    wsSettings["path"] = linkParseArray.Contains("path") ? HttpUtility.UrlDecode(linkParseArray[linkParseArray.IndexOf("path") + 1]) : "/";
                    break;
                case "h2":
                    if (linkParseArray.Contains("host"))
                    {
                        httpSettings["host"] = linkParseArray[linkParseArray.IndexOf("host") + 1].Split(',');
                    }
                    else
                    {
                        httpSettings["host"] = linkParseArray[1];
                    }
                    httpSettings["path"] = linkParseArray.Contains("path") ? HttpUtility.UrlDecode(linkParseArray[linkParseArray.IndexOf("path") + 1]) : "/";
                    break;
                case "tcp":

                    break;
                case "kcp":
                    kcpSettingsT["type"] = linkParseArray.Contains("headerType") ? linkParseArray[linkParseArray.IndexOf("headerType") + 1] : "none";
                    if (linkParseArray.Contains("seed"))
                    {
                        kcpSettings["seed"] = HttpUtility.UrlDecode(linkParseArray[linkParseArray.IndexOf("seed") + 1]);
                    }
                    break;
                case "quic":
                    quicSettings["security"] = linkParseArray.Contains("quicSecurity") ? linkParseArray[linkParseArray.IndexOf("quicSecurity") + 1] : "none";
                    quicSettings["key"] = linkParseArray.Contains("key") ? HttpUtility.UrlDecode(linkParseArray[linkParseArray.IndexOf("key") + 1]) : " ";
                    quicSettingsT["type"] = linkParseArray.Contains("headerType") ? linkParseArray[linkParseArray.IndexOf("headerType") + 1] : "none";
                    break;
                case "grpc":
                    grpcSettings["serviceName"] = linkParseArray.Contains("serviceName") ? linkParseArray[linkParseArray.IndexOf("serviceName") + 1] : "GunService";
                    break;
                default:
                    break;
            }
            if (url.StartsWith("vmess"))
            {
                UserInfo["alterId"] = 0;
                tlsSettings["allowInsecure"] = false;
                tlsSettings["allowInsecureCiphers"] = false;
            }
            mainWindow.profiles.Add(templateProfiles);
            return HttpUtility.UrlDecode(linkParseArray[linkParseArray.Count - 1]);
        }
        #endregion
    }
}