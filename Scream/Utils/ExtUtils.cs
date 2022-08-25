using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using IWshRuntimeLibrary;
using System.Collections.Generic;

namespace Scream
{
    class ExtUtils
    {

        /// <summary>
        /// get Clipboard
        /// </summary>
        /// <returns></returns>
        public static string GetClipboardData()
        {
            string strData = string.Empty;
            try
            {
                IDataObject data = Clipboard.GetDataObject();
                if (data.GetDataPresent(DataFormats.Text))
                {
                    strData = data.GetData(DataFormats.Text).ToString();
                }
                return strData;
            }
            catch
            {
            }
            return strData;
        }

        /// <summary>
        /// set Clipboard
        /// </summary>
        /// <returns></returns>
        public static void SetClipboardData(string strData)
        {
            try
            {
                Clipboard.SetText(strData);
            }
            catch
            {
            }
        }

        /// <summary>
        /// HttpWebRequest
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetUrl(string url)
        {
            string result = "";
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                req.Timeout = 5000;
                Stream stream = resp.GetResponseStream();
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
                stream.Close();
                resp.Close();
                req.Abort();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {

            }
            return result;
        }

        /// <summary>
        /// speed test via http request
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetHttpStatusTime(string url)
        {
            int responseTime = 0;
            int testcount = 0;
            do
            {
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.Timeout = 5000;
                    Stopwatch responsetimer = new Stopwatch();
                    responsetimer.Start();
                    HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                    responsetimer.Stop();
                    TimeSpan ts = responsetimer.Elapsed;
                    responseTime = (int)ts.TotalMilliseconds;
                    resp.Close();
                    req.Abort();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                testcount++;
            } while (testcount < 2);
            return responseTime.ToString();
        }

        /// <summary>
        /// speed test via http proxy request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static string GetHttpStatusTime(string url, int port)
        {
            int responseTime = 0;
            int testcount = 0;
            do
            {
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.Timeout = 5000;
                    WebProxy proxy = new WebProxy("127.0.0.1", port);
                    req.Proxy = proxy;
                    Stopwatch responsetimer = new Stopwatch();
                    responsetimer.Start();
                    HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                    responsetimer.Stop();
                    TimeSpan ts = responsetimer.Elapsed;
                    responseTime = (int)ts.TotalMilliseconds;
                    resp.Close();
                    req.Abort();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                testcount++;
            } while (testcount < 2);
            return responseTime.ToString();
        }

        /// <summary>
        /// Base64 encode
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string Base64Encode(string plainText)
        {
            try
            {
                byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Base64 decode
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string Base64Decode(string plainText)
        {
            try
            {
                plainText = plainText.Trim()
                  .Replace("\n", "")
                  .Replace("\r\n", "")
                  .Replace("\r", "")
                  .Replace(" ", "");

                if (plainText.Length % 4 > 0)
                {
                    plainText = plainText.PadRight(plainText.Length + 4 - plainText.Length % 4, '=');
                }

                byte[] data = Convert.FromBase64String(plainText);
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                return string.Empty;
            }
        }


        /// <summary>
        /// json key to lower
        /// </summary>
        /// <param name="jsonObject"></param>
        public static void ChangePropertiesToLowerCase(JObject jsonObject)
        {
            foreach (JProperty property in jsonObject.Properties().ToList())
            {
                if (property.Value.Type == JTokenType.Object)// replace property names in child object
                    ChangePropertiesToLowerCase((JObject)property.Value);

                property.Replace(new JProperty(property.Name.ToLower(), property.Value));// properties are read-only, so we have to replace them
            }
        }

        /// <summary>
        /// gc collect
        /// </summary>
        public static void FlushMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        #region Set autostart
        //https://blog.csdn.net/liyu3519/article/details/81257839

        private const string QuickName = "Scream";

        /// <summary>
        /// Get the system autostart directory
        /// </summary>
        private string SystemStartPath { get { return Environment.GetFolderPath(Environment.SpecialFolder.Startup); } }

        /// <summary>
        /// Get the full path to the program
        /// </summary>
        private string GetappAllPath(){ return Process.GetCurrentProcess().MainModule.FileName; }
        /// <summary>
        /// Get desktop directory 
        /// </summary>
        private string DesktopPath { get { return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); } }

        /// <summary>
        /// Set boot up automatically
        /// </summary>
        public void SetMeAutoStart(bool onOff = true)
        {
            if (onOff)
            {
                List<string> shortcutPaths = GetQuickFromFolder(SystemStartPath, GetappAllPath());
                if (shortcutPaths.Count >= 2)
                {
                    for (int i = 1; i < shortcutPaths.Count; i++)
                    {
                        DeleteFile(shortcutPaths[i]);
                    }
                }
                else if (shortcutPaths.Count < 1)
                {
                    CreateShortcut(SystemStartPath, QuickName, GetappAllPath(), "GUI for v2ray-core&xray-core on Windows");
                }
            }
            else
            {
                List<string> shortcutPaths = GetQuickFromFolder(SystemStartPath, GetappAllPath());
                if (shortcutPaths.Count > 0)
                {
                    for (int i = 0; i < shortcutPaths.Count; i++)
                    {
                        DeleteFile(shortcutPaths[i]);
                    }
                }
            }
            //CreateDesktopQuick(desktopPath, QuickName, appAllPath);
        }

        /// <summary>
        ///  Create a shortcut to the target path for the specified file
        /// </summary>
        private bool CreateShortcut(string directory, string shortcutName, string targetPath, string description = null, string iconLocation = null)
        {
            try
            {
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                // Com - Windows Script Host Object Model
                string shortcutPath = Path.Combine(directory, string.Format("{0}.lnk", shortcutName));
                WshShell shell = new IWshRuntimeLibrary.WshShell();
                IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;                                                               
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);                                  
                shortcut.WindowStyle = 1;                                                                       
                shortcut.Description = description;
                shortcut.IconLocation = string.IsNullOrWhiteSpace(iconLocation) ? targetPath : iconLocation;
                shortcut.Save();
                return true;
            }
            catch (Exception ex)
            {
                string temp = ex.Message;
                temp = "";
            }
            return false;
        }

        /// <summary>
        /// Get the set of shortcut paths of the specified application in the specified folder
        /// </summary>
        private List<string> GetQuickFromFolder(string directory, string targetPath)
        {
            List<string> tempStrs = new List<string>();
            tempStrs.Clear();
            string tempStr = null;
            string[] files = Directory.GetFiles(directory, "*.lnk");
            if (files == null || files.Length < 1)
            {
                return tempStrs;
            }
            for (int i = 0; i < files.Length; i++)
            {
                //files[i] = string.Format("{0}\\{1}", directory, files[i]);
                tempStr = GetAppPathFromQuick(files[i]);
                if (tempStr == targetPath)
                {
                    tempStrs.Add(files[i]);
                }
            }
            return tempStrs;
        }

        /// <summary>
        /// Get the target file path of the shortcut - used to determine whether autostart has been enabled
        /// </summary>
        /// <param name="shortcutPath"></param>
        /// <returns></returns>
        private string GetAppPathFromQuick(string shortcutPath)
        {
            //Path to shortcut file = @"d:\Test.lnk";
            if (System.IO.File.Exists(shortcutPath))
            {
                WshShell shell = new WshShell();
                IWshShortcut shortct = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                return shortct.TargetPath;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Delete files by path - shortcut for removing programs from the computer's self-start directory when you cancel the self-start
        /// </summary>
        private void DeleteFile(string path)
        {
            FileAttributes attr = System.IO.File.GetAttributes(path);
            if (attr == FileAttributes.Directory)
            {
                Directory.Delete(path, true);
            }
            else
            {
                System.IO.File.Delete(path);
            }
        }

        /// <summary>
        /// Create a shortcut on the desktop
        /// </summary>
        public void CreateDesktopQuick(string desktopPath = "", string quickName = "", string appPath = "")
        {
            List<string> shortcutPaths = GetQuickFromFolder(desktopPath, appPath);
            if (shortcutPaths.Count < 1)
            {
                CreateShortcut(desktopPath, quickName, appPath, "app descripton");
            }
        }
        #endregion
    }
}
