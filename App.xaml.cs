using Games.Services;
using Games.Models;
using System.Threading.Tasks;
using System.Timers;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace Games
{
    public partial class App : Application
    {
        public static GameStateDatabase GameStateRepo { get; private set; }
        public static InventoryDatabase InventoryRepo { get; private set; }
        public static SettingDatabase SettingRepo { get; private set; }
        public static AudioService AudioService { get; private set; }
        
        // 游戏时间统计相关
        private static System.Timers.Timer _gameTimeTimer;
        private static long _sessionStartTime;
        private static bool _isTimerRunning = false;

        public App()
        {
            try
            {
                InitializeComponent();
                
                // 全屏显示设置已移至 MainActivity.cs
                
                // 初始化服务
                InitializeServices();

                // 创建Shell
                MainPage = new AppShell();
                
                // 启动音频服务初始化
                _ = InitializeAudioAsync();
                
                // 初始化计时器
                InitGameTimeTimer();
            }
            catch (Exception ex)
            {
                // 在Windows平台上进行额外的错误诊断
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    Debug.WriteLine($"Windows平台应用初始化错误: {ex.Message}");
                    Debug.WriteLine($"详细堆栈跟踪: {ex.StackTrace}");
                }
                Console.WriteLine($"Error initializing app: {ex.Message}");
            }
        }
        
        private void InitializeServices()
        {
            try
            {
                // 确保应用数据目录存在 - 这对Windows平台特别重要
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    string appDataDirectory = FileSystem.AppDataDirectory;
                    if (!Directory.Exists(appDataDirectory))
                    {
                        Directory.CreateDirectory(appDataDirectory);
                        Debug.WriteLine($"创建应用数据目录: {appDataDirectory}");
                    }
                }
                
                // 初始化数据库和服务
                SettingRepo = new SettingDatabase();
                GameStateRepo = new GameStateDatabase();
                InventoryRepo = new InventoryDatabase();
                AudioService = new AudioService();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing services: {ex.Message}");
                // 输出详细诊断信息
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    Debug.WriteLine($"Windows平台服务初始化错误: {ex}");
                }
            }
        }
        
        protected override async void OnStart()
        {
            try
            {
                base.OnStart();
                
                // 添加Windows平台特定的启动逻辑
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    Debug.WriteLine("Windows平台应用启动");
                }
                
                try 
                {
                    // 确保音频服务正确重置和初始化
                    await AudioService.ResetAudio();
                    
                    // 加载音乐设置
                    var setting = await SettingRepo.GetSettingById(1);
                    if (setting != null && setting.IsMusicEnabled)
                    {
                        await AudioService.PlayBackgroundMusic();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing audio on start: {ex.Message}");
                }
                
                // 在应用启动时开始计时
                MainThread.BeginInvokeOnMainThread(async () => {
                    await StartGameTimeTracking();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"应用启动错误: {ex.Message}");
            }
        }
        
        private async Task InitializeAudioAsync()
        {
            try
            {
                // 先初始化音频服务
                await AudioService.InitializeAsync();
                
                // 根据设置决定是否播放背景音乐
                var setting = await SettingRepo.GetSettingById(1);
                if (setting != null && setting.IsMusicEnabled)
                {
                    await AudioService.PlayBackgroundMusic();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing audio: {ex.Message}");
            }
        }

        protected override void OnSleep()
        {
            // 应用进入后台时暂停音乐
            AudioService?.PauseBackgroundMusic();
            
            // 在应用进入后台时停止计时
            MainThread.BeginInvokeOnMainThread(async () => {
                await StopGameTimeTracking();
            });
            
            base.OnSleep();
        }

        protected override async void OnResume()
        {
            base.OnResume();
            
            // 应用恢复时，根据设置决定是否恢复播放音乐
            try
            {
                var setting = await SettingRepo.GetSettingById(1);
                if (setting != null && setting.IsMusicEnabled)
                {
                    await AudioService.PlayBackgroundMusic();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resuming audio: {ex.Message}");
            }
            
            // 在应用恢复时重新开始计时
            MainThread.BeginInvokeOnMainThread(async () => {
                await StartGameTimeTracking();
            });
        }

        private void InitGameTimeTimer()
        {
            _gameTimeTimer = new System.Timers.Timer(10000); // 每10秒更新一次
            _gameTimeTimer.Elapsed += async (sender, e) => {
                if (_isTimerRunning)
                {
                    // 计算当前游戏时长
                    long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    long elapsedSeconds = currentTimestamp - _sessionStartTime;
                    
                    // 获取之前的游戏时长
                    long previousTime = await SettingRepo.GetGamePlayTimeAsync(1);
                    
                    // 更新总游戏时长
                    await SettingRepo.UpdateGamePlayTimeAsync(1, previousTime + elapsedSeconds);
                    
                    // 更新会话开始时间为现在
                    _sessionStartTime = currentTimestamp;
                    await SettingRepo.UpdateLastSessionStartTimeAsync(1, _sessionStartTime);
                }
            };
            _gameTimeTimer.AutoReset = true;
        }
        
        public static async Task StartGameTimeTracking()
        {
            if (!_isTimerRunning)
            {
                _sessionStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await SettingRepo.UpdateLastSessionStartTimeAsync(1, _sessionStartTime);
                _gameTimeTimer.Start();
                _isTimerRunning = true;
            }
        }
        
        public static async Task StopGameTimeTracking()
        {
            if (_isTimerRunning)
            {
                _gameTimeTimer.Stop();
                _isTimerRunning = false;
                
                // 记录最后一次时长更新
                long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long elapsedSeconds = currentTimestamp - _sessionStartTime;
                
                // 获取之前的游戏时长
                long previousTime = await SettingRepo.GetGamePlayTimeAsync(1);
                
                // 更新总游戏时长
                await SettingRepo.UpdateGamePlayTimeAsync(1, previousTime + elapsedSeconds);
            }
        }
        
        public static string FormatGameTime(long totalSeconds)
        {
            try
            {
                TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
                return $"{time.Hours}小时{time.Minutes}分{time.Seconds}秒";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"格式化游戏时间出错: {ex.Message}");
                return "0小时0分0秒"; // 出错时返回默认值
            }
        }
    }
}
