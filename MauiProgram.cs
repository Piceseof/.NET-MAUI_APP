using Microsoft.Extensions.Logging;
using Games.Services;
using Plugin.Maui.Audio;
using System.Diagnostics;

namespace Games
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            try
            {
                var builder = MauiApp.CreateBuilder();
                builder
                    .UseMauiApp<App>()
                    .ConfigureFonts(fonts =>
                    {
                        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    });

                // 配置平台特定设置
                ConfigurePlatformSpecific(builder);

#if DEBUG
                builder.Logging.AddDebug();
#endif

                builder.Services.AddSingleton<SettingDatabase>();
                builder.Services.AddSingleton<InventoryDatabase>();
                builder.Services.AddSingleton<AudioService>();
                return builder.Build();
            }
            catch (Exception ex)
            {
                // 记录初始化错误
                Debug.WriteLine($"应用程序初始化时出错: {ex.Message}");
                Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                // 在严重错误的情况下，我们仍然需要返回一个MauiApp实例
                return MauiApp.CreateBuilder().UseMauiApp<App>().Build();
            }
        }

        /// <summary>
        /// 配置平台特定设置
        /// </summary>
        private static void ConfigurePlatformSpecific(MauiAppBuilder builder)
        {
            try
            {
                // 检查是否为Windows平台
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    Debug.WriteLine("为Windows平台配置特定设置");
                    
                    // 配置Windows特定的服务
                    builder.Services.AddSingleton<IWindowsPlatformService, WindowsPlatformService>();
                    
                    // 这里可以添加更多Windows特定配置
                }
                
                // 确保应用数据目录存在
                PlatformService.EnsureAppDataDirectoryExists();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"配置平台特定设置时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Windows平台特定服务接口
    /// </summary>
    public interface IWindowsPlatformService
    {
        /// <summary>
        /// 检查Windows特定目录是否存在
        /// </summary>
        Task EnsureDirectoriesExist();
    }

    /// <summary>
    /// Windows平台特定服务实现
    /// </summary>
    public class WindowsPlatformService : IWindowsPlatformService
    {
        public async Task EnsureDirectoriesExist()
        {
            try
            {
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    // 确保应用数据目录存在
                    string appDataDir = FileSystem.AppDataDirectory;
                    if (!Directory.Exists(appDataDir))
                    {
                        Directory.CreateDirectory(appDataDir);
                        Debug.WriteLine($"已创建Windows应用数据目录: {appDataDir}");
                    }
                    
                    // 可以添加更多Windows特定的目录检查和创建
                }
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"确保Windows目录存在时出错: {ex.Message}");
            }
        }
    }
}
