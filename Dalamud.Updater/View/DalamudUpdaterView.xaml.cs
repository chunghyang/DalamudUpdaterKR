using AutoUpdaterDotNET;
using Newtonsoft.Json;
using Serilog.Core;
using Serilog.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents; 
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using XIVLauncher.Common.Dalamud;
using static System.Net.Mime.MediaTypeNames; 
using System.Drawing;
using System.Windows.Threading;
using System.Xml;

namespace Dalamud.Updater.View
{
    /// <summary>
    /// DalamudUpdaterView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DalamudUpdaterView : Window, INotifyPropertyChanged
    {
        #region PropertyChange
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string Name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
        }

        private string dalaVersion = "0.0.0.0";
        public string DalaVersion
        {
            get => dalaVersion;
            set
            {
                dalaVersion = value;
                OnPropertyChanged("DalaVersion");
            }
        }
        private string updaterVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public string UpdaterVersion
        {
            get => updaterVersion;
            set
            {
                updaterVersion = value;
                OnPropertyChanged("UpdaterVersion");
            }
        }
        #endregion

        #region Cons
        private readonly string updateUrl = "https://dalamud-1253720819.cos.ap-nanjing.myqcloud.com/updater.xml";
        private bool firstHideHint = true;
        private bool isThreadRunning = true;
        private bool dotnetDownloadFinished = false;
        private bool desktopDownloadFinished = false;
        private double injectDelaySeconds = 1;

        private DalamudLoadingOverlay dalamudLoadingOverlay;
        private readonly DirectoryInfo addonDirectory;
        private readonly DirectoryInfo runtimeDirectory;
        private readonly DirectoryInfo assetDirectory;
        private readonly DirectoryInfo configDirectory;
        private readonly DalamudUpdater dalamudUpdater;

        public string windowsTitle = "달라가브KR v" + Assembly.GetExecutingAssembly().GetName().Version;
        #endregion



        public DalamudUpdaterView()
        {
            InitializeComponent();
            this.DataContext = this;

            InitLogging();
            InitializeComponent();
            InitializePIDCheck();
            InitializeDeleteShit();
            InitializeConfig();
            addonDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
            dalamudLoadingOverlay = new DalamudLoadingOverlay();
            dalamudLoadingOverlay.OnProgressBar += setProgressBar;
            dalamudLoadingOverlay.OnSetVisible += setVisible;
            dalamudLoadingOverlay.OnStatusLabel += setStatus;
            addonDirectory = new DirectoryInfo(Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "addon"));
            runtimeDirectory = new DirectoryInfo(Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "runtime"));
            assetDirectory = new DirectoryInfo(Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "XIVLauncher", "dalamudAssets"));
            configDirectory = new DirectoryInfo(Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "XIVLauncher", "pluginConfigs"));
            SetDalamudVersion();
            var strArgs = Environment.GetCommandLineArgs();
            if (strArgs.Length >= 2 && strArgs[1].Equals("-startup"))
            {
                //this.WindowState = FormWindowState.Minimized;
                //this.ShowInTaskbar = false;
                if (firstHideHint)
                {
                    firstHideHint = false;
                    this.notifyIcon.ShowBalloonTip(2000, "자동 실행", "백그라운드에서 자동으로 실행되었습니다.", System.Windows.Forms.ToolTipIcon.Info);
                }
            }

            try
            {
                var localVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var remoteUrl = GetProperUrl(updateUrl);
                var remoteXml = new XmlDocument();
                remoteXml.Load(remoteUrl);

                var nodes = remoteXml.SelectNodes("/item/version");
                if (nodes == null) throw new NullReferenceException("/item/version 노드 없음");

                foreach (XmlNode child in nodes)
                {
                    if (child.InnerText != localVersion.ToString())
                    {
                        AutoUpdater.Start(remoteUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "버전 확인에 실패했습니다.",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }

            dalamudUpdater = new DalamudUpdater(addonDirectory, runtimeDirectory, assetDirectory, configDirectory)
            {
                Overlay = dalamudLoadingOverlay
            };
            dalamudUpdater.OnUpdateEvent += DalamudUpdater_OnUpdateEvent;



        }



        #region Oversea Accelerate Helper

        private bool RemoteFileExists(string url)
        {
            try
            {
                var request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "HEAD";
                var response = request.GetResponse() as HttpWebResponse;
                response.Close();
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                return false;
            }
        }

        private string GetProperUrl(string url)
        {
            if (RemoteFileExists(url))
                return url;
            var accUrl = url.Replace("/updater.xml", "/acce_updater.xml").Replace("ap-nanjing", "accelerate");
            return accUrl;
        }

        #endregion


        #region App Settings
        public static string GetAppSettings(string key, string def = null)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                var ele = settings[key];
                if (ele == null) return def;
                return ele.Value;
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
            }

            return def;
        }

        public static void AddOrUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }

                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }
        #endregion


        #region Check Version
        private void CheckUpdate()
        {
            //여러번 업데이트 체크할 때 띄우는 메세지 삭제 =ㅅ=;;        2022-08-08 16:15
            dalamudUpdater.Run();
        }


        private Version GetUpdaterVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        private Version getVersion()
        {
            var rgx = new Regex(@"^\d+\.\d+\.\d+\.\d+$");
            var di = new DirectoryInfo(Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "addon", "Hooks"));
            var version = new Version("0.0.0.0");
            if (!di.Exists)
                return version;
            var dirs = di.GetDirectories("*", SearchOption.TopDirectoryOnly).Where(dir => rgx.IsMatch(dir.Name)).ToArray();
            foreach (var dir in dirs)
            {
                var newVersion = new Version(dir.Name);
                if (newVersion > version)
                {
                    version = newVersion;
                }
            }

            return version;
        }




        #endregion


        #region NotifyIcon
        System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();
        private void createNotifyIcon()
        {
            this.notifyIcon.BalloonTipTitle = "달라가브KR";
            this.notifyIcon.BalloonTipText= "달라가브KR";
            this.notifyIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.notifyIcon.Text= "달라가브KR";
            this.notifyIcon.Visible = true;



            this.notifyIcon.Icon = Dalamud.Updater.Properties.Resources.dalamud;
            this.notifyIcon.MouseClick += NotifyIcon_MouseClick;
            this.notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            this.notifyIcon.ContextMenuStrip.Items.Add(new System.Windows.Forms.ToolStripMenuItem("메뉴",null, NotifyIcon_Menu_Clicked));
            this.notifyIcon.ContextMenuStrip.Items.Add(new System.Windows.Forms.ToolStripMenuItem("종료",null, NotifyIcon_Close_Clicked));
        }

        private void NotifyIcon_Menu_Clicked(object sender, EventArgs e)
        {
            if (!(this.Visibility == Visibility.Visible)) this.Visibility = Visibility.Visible;
            this.Activate();
        }
        private void NotifyIcon_Close_Clicked(object sender, EventArgs e)
        {
            this.isThreadRunning = false;
            this.notifyIcon.Dispose();
            System.Windows.Application.Current?.Shutdown();
        }

        private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (this.WindowState == WindowState.Minimized)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                }

                this.Activate();
            }
        }

        #endregion

        private void DalamudUpdater_OnUpdateEvent(DalamudUpdater.DownloadState value)
        {
            switch (value)
            {
                case DalamudUpdater.DownloadState.Failed:
                    MessageBox.Show("달라가브 업데이트 실패", windowsTitle);
                    setStatus("플러그인 파일 확인 요망");
                    break;
                case DalamudUpdater.DownloadState.Unknown:
                    setStatus("알수없는 오류");
                    break;
                case DalamudUpdater.DownloadState.NoIntegrity:
                    setStatus("버전 미호환");
                    break;
                case DalamudUpdater.DownloadState.Done:
                    SetDalamudVersion();
                    setStatus("달라가브 업데이트 성공");
                    break;
                case DalamudUpdater.DownloadState.Checking:
                    setStatus("업데이트 체크중...");
                    break;
            }
        }

        public void SetDalamudVersion()
        {
            DalaVersion = getVersion().ToString();
        }

        #region init

        private static void InitLogging()
        {
            var baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var logPath = Path.Combine(baseDirectory, "Dalamud.Updater.log");

            var levelSwitch = new LoggingLevelSwitch();

#if DEBUG
            levelSwitch.MinimumLevel = LogEventLevel.Verbose;
#else
            levelSwitch.MinimumLevel = LogEventLevel.Information;
#endif
            Log.Logger = new LoggerConfiguration()
                         //.WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose)
                         .WriteTo.Async(a => a.File(logPath))
                         .MinimumLevel.ControlledBy(levelSwitch)
                         .CreateLogger();
        }

        private void InitializePluginKR() { }

        private void InitializeConfig()
        {
            if (GetAppSettings("AutoInject", "false") == "true")
            {
                this.AutoApplyCheckBox.IsChecked = true;
            }

            if (GetAppSettings("AutoStart", "false") == "true")
            {
                this.AutoRunCheckBox.IsChecked = true;
            }

            if (GetAppSettings("Accelerate", "false") == "true")
            {
                this.AutoUpdateCheckBox.IsChecked = true;
            }

            var tempInjectDelaySeconds = GetAppSettings("InjectDelaySeconds", "0");
            if (tempInjectDelaySeconds != "0")
            {
                this.injectDelaySeconds = double.Parse(tempInjectDelaySeconds);
            }
        }

        private void InitializeDeleteShit()
        {
            var shitInjector = Path.Combine(Directory.GetCurrentDirectory(), "Dalamud.Injector.exe");
            if (File.Exists(shitInjector))
            {
                File.Delete(shitInjector);
            }

            var shitDalamud = Path.Combine(Directory.GetCurrentDirectory(), "6.3.0.9");
            if (Directory.Exists(shitDalamud))
            {
                Directory.Delete(shitDalamud, true);
            }

            var shitUIRes = Path.Combine(Directory.GetCurrentDirectory(), "XIVLauncher", "dalamudAssets", "UIRes");
            if (Directory.Exists(shitUIRes))
            {
                Directory.Delete(shitUIRes, true);
            }

            var shitRuntime = Path.Combine(Directory.GetCurrentDirectory(), "XIVLauncher", "runtime");
            if (Directory.Exists(shitRuntime))
            {
                Directory.Delete(shitRuntime, true);
            }
        }

        private void InitializePIDCheck()
        {
            var thread = new Thread(() =>
            {
                while (this.isThreadRunning)
                {
                    try
                    {
                        /*var newPidList = Process.GetProcessesByName("ffxiv_dx11").Where(process =>
                        {
                            return !process.MainWindowTitle.Contains("FINAL FANTASY XIV");
                        }).ToList().ConvertAll(process => process.Id.ToString()).ToArray();*/
                        // 한국 클라이언트 PID를 찾을 수 없는 오류 수정. 원리는 모름 2022-08-08 16:19
                        var newPidList = Process.GetProcessesByName("ffxiv_dx11").Select(x => x.Id.ToString()).ToArray();
                        var newHash = string.Join(", ", newPidList).GetHashCode();
                        var oldPidList = this.ProcessPicker.Items.Cast<object>().Select(item => item.ToString()).ToArray();
                        var oldHash = string.Join(", ", oldPidList).GetHashCode();
                        if (oldHash != newHash)
                        {

                            // Running on the UI thread
                            this.ProcessPicker.Items.Clear();
                            //this.ProcessPicker.Items.AddRange(newPidList.Cast<object>().ToArray());
                            newPidList.Cast<object>().ToList().ForEach(fe => this.ProcessPicker.Items.Add(fe));

                            if (newPidList.Length > 0)
                            {
                                if (!this.ProcessPicker.IsDropDownOpen)
                                    this.ProcessPicker.SelectedIndex = 0;

                                if (this.AutoApplyCheckBox.IsChecked.GetValueOrDefault(false))
                                {
                                    foreach (var pidStr in newPidList)
                                    {
                                        var pid = int.Parse(pidStr);
                                        if (this.Inject(pid, (int)this.injectDelaySeconds * 1000))
                                        {
                                            this.notifyIcon.ShowBalloonTip(2000, "자동 Inject", $"프로세스 ID : {pid}，자동적용에 성공했습니다.", System.Windows.Forms.ToolTipIcon.Info);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                    Thread.Sleep(1000);
                }
            })
            {
                IsBackground = true
            };
            thread.Start();
        }

        #endregion
        private void DalamudUpdaterView_Loaded(object sender, RoutedEventArgs e)
        {
            createNotifyIcon();
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;


            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.InstalledVersion = GetUpdaterVersion();
            UpdaterVersion = $"v{Assembly.GetExecutingAssembly().GetName().Version}";

            CheckUpdate();
        }

        private void DalamudUpdaterView_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.WindowState = WindowState.Minimized;
            this.Hide();
            //this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            //this.ShowInTaskbar = false;
            //this.Visible = false;
            if (firstHideHint)
            {
                firstHideHint = false;
                this.notifyIcon.ShowBalloonTip(2000, "프로그램 최소화", "메뉴는 트레이아이콘에서 선택해 주세요.", System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        private void DalamudUpdaterView_Closed(object sender, EventArgs e)
        {
            this.isThreadRunning = false;
        }

        private void AutoUpdater_ApplicationExitEvent()
        {
            Thread.Sleep(5000);
            System.Windows.Application.Current.Shutdown();
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            //TODO: 업데이트 꺼놨는데 나중에 우리가 업데이트할때 이이벤트 참고하여 실행해야됨.
            //이벤트 오버라이딩하지 않으면 AutoUpdater에서 알아서 업데이트창 있다고 띄움
            //그거관련 라이브러리 확인해봐야함. 디컴파일로밖에 확인이안됨..
            //OnCheckForUpdateEvent(args); 
        }

        private void ButtonCheckForUpdate_Click(object sender, EventArgs e)
        {
            if (this.ProcessPicker.SelectedItem != null)
            {
                var pid = int.Parse((string)this.ProcessPicker.SelectedItem);
                var process = Process.GetProcessById(pid);
                if (isInjected(process))
                {
                    var choice = MessageBox.Show("업데이트시 파판을 종료해야합니다. 할까요?","Dalamud",
                                                 MessageBoxButton.YesNo,
                                                 MessageBoxImage.Information);
                    if (choice == MessageBoxResult.Yes)
                    {
                        process.Kill();
                    }
                    else
                    {
                        return;
                    }
                }
            }

            CheckUpdate();
        }
        private void OriginalDeveloperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://qun.qq.com/qqweb/qunpro/share?_wv=3&_wwv=128&inviteCode=CZtWN&from=181074&biz=ka&shareSource=5");
        }
         
        private DalamudStartInfo GeneratingDalamudStartInfo(Process process, string dalamudPath, int injectDelay)
        {
            var ffxivDir = Path.GetDirectoryName(process.MainModule.FileName);
            var appDataDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var xivlauncherDir = Path.Combine(appDataDir, "XIVLauncher");

            var gameVerStr = File.ReadAllText(Path.Combine(ffxivDir, "ffxivgame.ver"));

            var startInfo = new DalamudStartInfo
            {
                ConfigurationPath = Path.Combine(xivlauncherDir, "dalamudConfig.json"),
                PluginDirectory = Path.Combine(xivlauncherDir, "installedPlugins"),
                DefaultPluginDirectory = Path.Combine(xivlauncherDir, "devPlugins"),
                AssetDirectory = this.dalamudUpdater.AssetDirectory.FullName,
                GameVersion = gameVerStr,
                Language = "1", //기존의 4는 중국클라이언트 전용. Dalamud 소스코드 확인 필요. 원리모름 2022-08-08 16:21
                OptOutMbCollection = false,
                GlobalAccelerate = this.AutoUpdateCheckBox.IsChecked.GetValueOrDefault(false),
                WorkingDirectory = dalamudPath,
                DelayInitializeMs = injectDelay
            };

            return startInfo;
        }

        private bool isInjected(Process process)
        {
            try
            {
                for (var j = 0; j < process.Modules.Count; j++)
                {
                    if (process.Modules[j].ModuleName == "Dalamud.dll")
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private bool Inject(int pid, int injectDelay = 0)
        {
            var process = Process.GetProcessById(pid);
            if (isInjected(process))
            {
                return false;
            }

            //DetectSomeShit(process);          지워도 된다고함 2022-08-08 16:23
            //var dalamudStartInfo = Convert.ToBase64String(Encoding.UTF8.GetBytes(GeneratingDalamudStartInfo(process)));
            //var startInfo = new ProcessStartInfo(injectorFile, $"{pid} {dalamudStartInfo}");
            //startInfo.WorkingDirectory = dalamudPath.FullName;
            //Process.Start(startInfo);
            Log.Information($"[Updater] dalamudUpdater.State:{dalamudUpdater.State}");
            if (dalamudUpdater.State == DalamudUpdater.DownloadState.NoIntegrity)
            {
                if (MessageBox.Show("현재 버전이 게임과 호환이 안될수있습니다. 주입합니까？", windowsTitle, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    return false;
                }
            }

            //return false;
            var dalamudStartInfo = GeneratingDalamudStartInfo(process, Directory.GetParent(dalamudUpdater.Runner.FullName).FullName, injectDelay);
            var environment = new Dictionary<string, string>();
            // No use cuz we're injecting instead of launching, the Dalamud.Boot.dll is reading environment variables from ffxiv_dx11.exe
            /*
            var prevDalamudRuntime = Environment.GetEnvironmentVariable("DALAMUD_RUNTIME");
            if (string.IsNullOrWhiteSpace(prevDalamudRuntime))
                environment.Add("DALAMUD_RUNTIME", runtimeDirectory.FullName);
            */
            WindowsDalamudRunner.Inject(dalamudUpdater.Runner, process.Id, environment, DalamudLoadMethod.DllInject, dalamudStartInfo);
            return true;
        }
        private void init_Dalamud_Click(object sender, RoutedEventArgs e)
        {
            if (this.ProcessPicker.SelectedItem != null
       && this.ProcessPicker.SelectedItem.ToString().Length > 0)
            {
                var pidStr = this.ProcessPicker.SelectedItem.ToString();
                if (int.TryParse(pidStr, out var pid))
                {
                    if (Inject(pid))
                    {
                        Log.Information("[DINJECT] Inject finished.");
                    }
                }
                else
                {
                    MessageBox.Show("게임(Process)을 찾을수 없습니다", "Not Search FFXIV",
                                   MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("게임(Process)를 선택해주세요", "Not Selected FFXIV",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        } 

        private void SetAutoRun()
        {
            var strFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            try
            {
                SystemHelper.SetAutoRun($"\"{strFilePath}\"" + " -startup", "DalamudAutoInjector", AutoRunCheckBox.IsChecked.GetValueOrDefault(false));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void checkBoxAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            SetAutoRun();
            AddOrUpdateAppSettings("AutoStart", AutoRunCheckBox.IsChecked.GetValueOrDefault(false) ? "true" : "false");
        }

        private void checkBoxAutoInject_CheckedChanged(object sender, EventArgs e)
        {
            AddOrUpdateAppSettings("AutoInject", AutoApplyCheckBox.IsChecked.GetValueOrDefault(false) ? "true" : "false");
        }

        private void checkBoxAcce_CheckedChanged(object sender, EventArgs e)
        {
            AddOrUpdateAppSettings("Accelerate", AutoUpdateCheckBox.IsChecked.GetValueOrDefault(false) ? "true" : "false");
        }

        private void setProgressBar(int v)
        {
            this.StateProgressBar.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
                {
                    this.StateProgressBar.Value = (double)v;
                    return null;
                }),null);
        }

        private void setStatus(string v)
        {
            this.StateProgerssBar_Staters.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
            {
                this.StateProgerssBar_Staters.Text=v;
                return null;
            }), null);
        }

        private void setVisible(bool v)
        {
            this.StateGrid.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
            {
                this.StateGrid.Visibility = v ? Visibility.Visible : Visibility.Hidden;
                return null;
            }), null);
        }

        private void AutoUpdateCheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void AutoRunCheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void AutoApplyCheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }


    }
}

