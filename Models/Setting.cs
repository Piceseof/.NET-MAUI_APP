using SQLite;

namespace Games.Models
{
    [Table("Setting")]
    public class Setting
    {
        [PrimaryKey]
        public int Id { get; set; }
        
        // 存档编号
        public int Archive { get; set; }
        
        // 返回页面
        public string BackPage { get; set; }
        
        // 音频设置
        public bool IsMusicEnabled { get; set; }
        public double MusicVolume { get; set; }
        public double SoundEffectVolume { get; set; }
        
        // 界面设置
        public double TextSize { get; set; }
        
        // 游戏时长（秒）
        public long GamePlayTimeSeconds { get; set; }
        
        // 游戏开始时间（上次记录）
        public long LastSessionStartTime { get; set; }
        
        // 上次活动的游戏页面
        public string LastActivePage { get; set; }
        
        // 兼容旧版本
        public string SettingBackPage { get; set; }
    }
}