using Plugin.Maui.Audio;
using System.Diagnostics;

namespace Games.Services
{
    public class AudioService
    {
        private readonly IAudioManager _audioManager;
        private IAudioPlayer _backgroundMusic;
        private double _backgroundMusicVolume = 0.5;
        private double _soundEffectVolume = 0.7;
        private bool _isInitialized = false;
        private bool _wasPlaying = false;
        private bool _isDisposed = false;  // 自定义标志跟踪播放器释放状态

        public AudioService()
        {
            _audioManager = new AudioManager();
        }

        public async Task InitializeAsync()
        {
            try
            {
                // 如果已经初始化但音频播放器为空或已被释放，重新创建
                if (_backgroundMusic == null || _isDisposed)
                {
                    _isInitialized = false;
                    _isDisposed = false;
                }

                if (!_isInitialized)
                {
                    // 使用PlatformService打开资源文件，以确保跨平台兼容
                    Stream stream = await PlatformService.OpenResourceFileAsync("background_music.mp3");
                    
                    _backgroundMusic = _audioManager.CreatePlayer(stream);
                    _backgroundMusic.Loop = true;
                    _backgroundMusic.Volume = _backgroundMusicVolume;
                    _isInitialized = true;
                    
                    // 记录初始化成功
                    PlatformService.LogDiagnostic("背景音乐初始化成功");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing audio: {ex.Message}");
                PlatformService.LogDiagnostic($"音频初始化错误: {ex.Message}");
                _isInitialized = false;
            }
        }

        public async Task PlayBackgroundMusic()
        {
            try
            {
                await InitializeAsync();
                
                // 只有当音频播放器存在且未释放时才播放
                if (_backgroundMusic != null && !_isDisposed)
                {
                    _backgroundMusic.Play();
                    _wasPlaying = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing music: {ex.Message}");
                PlatformService.LogDiagnostic($"播放音乐时出错: {ex.Message}");
                
                // 可能是播放器无效，尝试重新初始化
                _isInitialized = false;
                _isDisposed = true;
                await InitializeAsync();
                
                // 再次尝试播放
                try 
                {
                    if (_backgroundMusic != null)
                    {
                        _backgroundMusic.Play();
                        _wasPlaying = true;
                    }
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine($"Failed to play after reinitializing: {innerEx.Message}");
                    PlatformService.LogDiagnostic($"重新初始化后播放失败: {innerEx.Message}");
                }
            }
        }

        public void PauseBackgroundMusic()
        {
            try
            {
                if (_backgroundMusic != null && !_isDisposed)
                {
                    _backgroundMusic.Pause();
                    _wasPlaying = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error pausing music: {ex.Message}");
                // 发生异常可能是播放器已释放
                _isDisposed = true;
            }
        }

        public async Task ResetAudio()
        {
            try
            {
                // 记录当前播放状态
                if (_backgroundMusic != null && !_isDisposed)
                {
                    try
                    {
                        _wasPlaying = _backgroundMusic.IsPlaying;
                    }
                    catch
                    {
                        _wasPlaying = false;
                    }
                    
                    // 释放现有资源
                    _backgroundMusic.Dispose();
                }
                
                _backgroundMusic = null;
                _isInitialized = false;
                _isDisposed = true;
                
                // 重新初始化
                await InitializeAsync();
                
                // 如果之前在播放，则恢复播放
                if (_wasPlaying)
                {
                    await PlayBackgroundMusic();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting audio: {ex.Message}");
            }
        }

        public async Task SetBackgroundMusicVolume(double volume)
        {
            try
            {
                _backgroundMusicVolume = volume;
                if (_backgroundMusic != null && !_isDisposed)
                {
                    _backgroundMusic.Volume = volume;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting background music volume: {ex.Message}");
                await ResetAudio();
            }
        }

        public async Task SetSoundEffectVolume(double volume)
        {
            _soundEffectVolume = volume;
        }

        // 修改音效播放方法
        public async Task PlayItemPickupSound()
        {
            try
            {
                // 使用PlatformService打开音效文件
                Stream stream = await PlatformService.OpenResourceFileAsync("item_pickup.mp3");
                
                var player = _audioManager.CreatePlayer(stream);
                
                // 使用当前设置的音效音量
                player.Volume = _soundEffectVolume;
                player.Play();
                
                // 播放完成后自动释放资源
                player.PlaybackEnded += (sender, e) => {
                    player.Dispose();
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing item pickup sound: {ex.Message}");
                PlatformService.LogDiagnostic($"播放音效时出错: {ex.Message}");
            }
        }
    }
} 