using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Storage;

namespace LibreOfficeAI.Services
{
    public class AudioService
    {
        private MediaCapture? _mediaCapture;
        public bool IsRecording { get; private set; } = false;

        private async Task InitialiseMediaCapture()
        {
            Debug.WriteLine("Initializing MediaCapture...");
            _mediaCapture = new MediaCapture();

            // Explicitly specify audio capture settings
            var settings = new Windows.Media.Capture.MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.Audio,
            };

            await _mediaCapture.InitializeAsync(settings);
            Debug.WriteLine("MediaCapture initialized successfully");
        }

        private async Task StartRecording()
        {
            try
            {
                Debug.WriteLine("Starting recording...");

                // Use temp folder which is accessible in unpackaged apps
                var tempPath = Path.GetTempPath();
                Debug.WriteLine($"Temp path: {tempPath}");

                var tempFolder = await StorageFolder.GetFolderFromPathAsync(tempPath);
                Debug.WriteLine("Got temp folder successfully");

                var audioFile = await tempFolder.CreateFileAsync(
                    "recordedAudio.wav",
                    CreationCollisionOption.ReplaceExisting
                );
                Debug.WriteLine($"Created audio file: {audioFile.Path}");

                await _mediaCapture.StartRecordToStorageFileAsync(
                    Windows.Media.MediaProperties.MediaEncodingProfile.CreateWav(
                        Windows.Media.MediaProperties.AudioEncodingQuality.Auto
                    ),
                    audioFile
                );

                IsRecording = true;
                Debug.WriteLine("Recording started successfully");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Microphone access denied: {ex.Message}");
            }
        }

        private async Task StopRecording()
        {
            Debug.WriteLine("Stopping recording...");
            await _mediaCapture.StopRecordAsync();
            IsRecording = false;
            Debug.WriteLine("Recording stopped successfully");

            // Log the final file location
            var tempPath = Path.GetTempPath();
            var finalFilePath = Path.Combine(tempPath, "recordedAudio.wav");
            if (File.Exists(finalFilePath))
            {
                var fileInfo = new FileInfo(finalFilePath);
                Debug.WriteLine($"Audio file created successfully at: {finalFilePath}");
                Debug.WriteLine($"File size: {fileInfo.Length} bytes");
            }
            else
            {
                Debug.WriteLine($"Audio file NOT found at expected location: {finalFilePath}");
            }
        }

        public async Task ToggleRecordingAsync()
        {
            try
            {
                if (_mediaCapture == null)
                {
                    await InitialiseMediaCapture();
                }

                if (!IsRecording)
                {
                    await StartRecording();
                }
                else
                {
                    await StopRecording();
                }
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine($"MediaCapture was disposed: {ex.Message}");
                _mediaCapture = null; // Reset so it can be recreated
                IsRecording = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Audio recording error: {ex}");
                Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Reset state on error
                IsRecording = false;
                if (_mediaCapture != null)
                {
                    try
                    {
                        _mediaCapture.Dispose();
                    }
                    catch { }
                    _mediaCapture = null;
                }
            }
        }
    }
}
