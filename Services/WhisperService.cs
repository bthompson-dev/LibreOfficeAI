using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.Logger;

namespace LibreOfficeAI.Services
{
    public class WhisperService : IDisposable
    {
        private readonly WhisperFactory whisperFactory;
        private readonly WhisperProcessor processor;
        private readonly IDisposable whisperLogger;
        public bool IsTranscribing { get; private set; } = false;
        public event Action? IsTranscribingChanged;

        public WhisperService()
        {
            var ggmlType = GgmlType.Base;
            var modelFileName = "ggml-base.bin";
            if (!File.Exists(modelFileName))
            {
                DownloadModel(modelFileName, ggmlType).GetAwaiter().GetResult();
            }
            whisperLogger = LogProvider.AddConsoleLogging(WhisperLogLevel.Debug);
            whisperFactory = WhisperFactory.FromPath(modelFileName);
            processor = whisperFactory.CreateBuilder().WithLanguage("auto").Build();
        }

        public async Task<string?> Transcribe(string wavFilePath)
        {
            try
            {
                IsTranscribing = true;
                IsTranscribingChanged?.Invoke();

                using var fileStream = File.OpenRead(wavFilePath);

                var userMessage = new StringBuilder();

                // This section processes the audio file and prints the results (start time, end time and text) to the console.
                await foreach (var result in processor.ProcessAsync(fileStream))
                {
                    Debug.WriteLine($"{result.Start}->{result.End}: {result.Text}");
                    userMessage.Append(result.Text);
                }

                IsTranscribing = false;
                IsTranscribingChanged?.Invoke();

                return userMessage.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        private static async Task DownloadModel(string fileName, GgmlType ggmlType)
        {
            Console.WriteLine($"Downloading Model {fileName}");
            using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType);
            using var fileWriter = File.OpenWrite(fileName);
            await modelStream.CopyToAsync(fileWriter);
        }

        public void Dispose()
        {
            processor?.Dispose();
            whisperFactory?.Dispose();
            whisperLogger?.Dispose();
        }
    }
}
