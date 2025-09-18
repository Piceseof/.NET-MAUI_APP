using System.Diagnostics;

namespace Games.Services
{
    /// <summary>
    /// 提供平台特定功能的服务类
    /// </summary>
    public static class PlatformService
    {
        /// <summary>
        /// 检查当前平台是否为Windows
        /// </summary>
        public static bool IsWindows => DeviceInfo.Platform == DevicePlatform.WinUI;

        /// <summary>
        /// 确保应用数据目录存在
        /// </summary>
        /// <returns>应用数据目录路径</returns>
        public static string EnsureAppDataDirectoryExists()
        {
            try
            {
                string appDataDir = FileSystem.AppDataDirectory;
                
                // 在Windows平台上，确保目录存在
                if (IsWindows && !Directory.Exists(appDataDir))
                {
                    Directory.CreateDirectory(appDataDir);
                    Debug.WriteLine($"已创建应用数据目录: {appDataDir}");
                }
                
                return appDataDir;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建应用数据目录时出错: {ex.Message}");
                // 返回一个临时目录作为备选
                return Path.GetTempPath();
            }
        }

        /// <summary>
        /// 获取平台特定的资源路径
        /// </summary>
        /// <param name="resourceName">资源名称</param>
        /// <returns>平台特定的资源路径</returns>
        public static string GetPlatformResourcePath(string resourceName)
        {
            if (IsWindows)
            {
                // Windows平台上MAUI的资源路径可能有所不同
                if (resourceName.StartsWith("Resources/"))
                {
                    return resourceName;
                }
                return $"Resources/Raw/{resourceName}";
            }
            
            // 其他平台直接返回原始资源名
            return resourceName;
        }

        /// <summary>
        /// 记录平台特定的诊断信息
        /// </summary>
        /// <param name="message">诊断消息</param>
        public static void LogDiagnostic(string message)
        {
            if (IsWindows)
            {
                Debug.WriteLine($"[Windows] {message}");
            }
            else
            {
                Debug.WriteLine(message);
            }
        }

        /// <summary>
        /// 以平台兼容的方式打开资源文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>文件流</returns>
        public static async Task<Stream> OpenResourceFileAsync(string fileName)
        {
            try
            {
                // 尝试直接打开资源
                return await FileSystem.OpenAppPackageFileAsync(fileName);
            }
            catch (Exception ex)
            {
                if (IsWindows)
                {
                    LogDiagnostic($"Windows平台尝试打开资源文件失败: {fileName}, 错误: {ex.Message}");
                    
                    // 尝试不同的资源路径
                    try
                    {
                        string windowsPath = GetPlatformResourcePath(fileName);
                        LogDiagnostic($"尝试Windows特定路径: {windowsPath}");
                        return await FileSystem.OpenAppPackageFileAsync(windowsPath);
                    }
                    catch (Exception innerEx)
                    {
                        LogDiagnostic($"Windows平台备用路径也失败: {innerEx.Message}");
                        throw new FileNotFoundException($"找不到资源文件: {fileName}", innerEx);
                    }
                }
                
                // 非Windows平台，直接抛出原始异常
                throw;
            }
        }
    }
} 