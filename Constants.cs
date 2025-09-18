using System;
using System.IO;

namespace Games;

public static class Constants
{
    // 数据库文件名
    private const string DbFileName = "games.db3";

    // 获取跨平台兼容的数据库路径
    public static string DatabasePath
    {
        get
        {
            // 使用Path.Combine确保路径分隔符在不同平台上正确
            string basePath = FileSystem.AppDataDirectory;
            
            // 在Windows平台上特殊处理路径
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                // 确保路径存在
                Directory.CreateDirectory(basePath);
            }
            
            return Path.Combine(basePath, DbFileName);
        }
    }

    // SQLite连接标志
    public static SQLite.SQLiteOpenFlags Flags =
        // 打开数据库以读写操作
        SQLite.SQLiteOpenFlags.ReadWrite |
        // 如果数据库不存在则创建
        SQLite.SQLiteOpenFlags.Create |
        // 启用多线程数据库访问
        SQLite.SQLiteOpenFlags.SharedCache;
} 