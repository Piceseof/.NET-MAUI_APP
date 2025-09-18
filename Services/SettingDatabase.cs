using SQLite;
using Games.Models;

namespace Games.Services;

public class SettingDatabase
{
    private SQLiteAsyncConnection _database;

    public SettingDatabase()
    {
    }

    private async Task Init()
    {
        if (_database is not null)
            return;

        _database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
        await _database.CreateTableAsync<Setting>();
    }

    public async Task AddNewSetting(int id, int archive, string backPage)
    {
        try
        {
            await Init();
            await _database.InsertAsync(new Setting 
            { 
                Id = id, 
                Archive = archive, 
                BackPage = backPage,
                IsMusicEnabled = true,
                MusicVolume = 0.5,
                SoundEffectVolume = 0.7,
                TextSize = 1.0
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public async Task<int> GetArchiveById(int id)
    {
        try
        {
            await Init();
            var setting = await _database.Table<Setting>().FirstOrDefaultAsync(x => x.Id == id);
            return setting?.Archive ?? 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 0;
        }
    }

    public async Task UpdateArchive(int id, int archive)
    {
        try
        {
            await Init();
            var setting = await _database.Table<Setting>().FirstOrDefaultAsync(x => x.Id == id);
            if (setting != null)
            {
                setting.Archive = archive;
                await _database.UpdateAsync(setting);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public async Task<Setting> GetSettingById(int id)
    {
        await Init();
        var setting = await _database.Table<Setting>()
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync();

        if (setting == null)
        {
            // 如果没有设置，创建默认设置
            setting = new Setting
            {
                Id = id,
                IsMusicEnabled = true,
                MusicVolume = 0.5,
                SoundEffectVolume = 0.7,
                TextSize = 1.0,
                BackPage = "StartGamePage",
                Archive = 0
            };
            await _database.InsertAsync(setting);
        }

        return setting;
    }

    public async Task<int> UpdateMusicSetting(int id, bool isEnabled)
    {
        await Init();
        var setting = await GetSettingById(id);
        setting.IsMusicEnabled = isEnabled;
        return await _database.UpdateAsync(setting);
    }

    public async Task<int> UpdateMusicVolume(int id, double volume)
    {
        await Init();
        var setting = await GetSettingById(id);
        setting.MusicVolume = volume;
        return await _database.UpdateAsync(setting);
    }

    public async Task<int> UpdateSoundEffectVolume(int id, double volume)
    {
        await Init();
        var setting = await GetSettingById(id);
        setting.SoundEffectVolume = volume;
        return await _database.UpdateAsync(setting);
    }

    public async Task<int> UpdateTextSize(int id, double size)
    {
        await Init();
        var setting = await GetSettingById(id);
        setting.TextSize = size;
        return await _database.UpdateAsync(setting);
    }

    public async Task<string> GetBackPageById(int id)
    {
        await Init();
        var setting = await GetSettingById(id);
        return setting.BackPage;
    }

    public async Task<int> UpdateBackPage(int id, string backPage)
    {
        await Init();
        var setting = await GetSettingById(id);
        setting.BackPage = backPage;
        // 同时更新旧版本的属性以保持兼容性
        setting.SettingBackPage = backPage;
        return await _database.UpdateAsync(setting);
    }

    public async Task ResetSettingsAsync()
    {
        await Init();
        await _database.DeleteAllAsync<Setting>();
    }

    public async Task<bool> IsSettingExists(int id)
    {
        try
        {
            await Init();
            var setting = await _database.Table<Setting>().FirstOrDefaultAsync(x => x.Id == id);
            return setting != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }

    public async Task ResetGame(int id)
    {
        try
        {
            await Init();
            var setting = await _database.Table<Setting>().FirstOrDefaultAsync(x => x.Id == id);
            if (setting != null)
            {
                setting.Archive = 0;
                setting.BackPage = "StartGamePage";
                setting.SettingBackPage = "StartGamePage";
                await _database.UpdateAsync(setting);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public async Task<long> GetGamePlayTimeAsync(int id)
    {
        await Init();
        var setting = await GetSettingById(id);
        return setting.GamePlayTimeSeconds;
    }
    
    public async Task<long> GetLastSessionStartTimeAsync(int id)
    {
        await Init();
        var setting = await GetSettingById(id);
        return setting.LastSessionStartTime;
    }
    
    public async Task UpdateGamePlayTimeAsync(int id, long seconds)
    {
        await Init();
        var setting = await GetSettingById(id);
        setting.GamePlayTimeSeconds = seconds;
        await _database.UpdateAsync(setting);
    }
    
    public async Task UpdateLastSessionStartTimeAsync(int id, long timestamp)
    {
        await Init();
        var setting = await GetSettingById(id);
        setting.LastSessionStartTime = timestamp;
        await _database.UpdateAsync(setting);
    }

    public async Task<int> UpdateSetting(Setting setting)
    {
        await Init();
        return await _database.UpdateAsync(setting);
    }
} 