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
    public class WhisperService
    {
        public static async Task<string?> Transcribe(string wavFilePath)
        {
            try
            {
                var ggmlType = GgmlType.Base;
                var modelFileName = "ggml-base.bin";

                // This section detects whether the "ggml-base.bin" file exists in our project disk. If it doesn't, it downloads it from the internet
                if (!File.Exists(modelFileName))
                {
                    await DownloadModel(modelFileName, ggmlType);
                }

                // Optional logging from the native library
                using var whisperLogger = LogProvider.AddConsoleLogging(WhisperLogLevel.Debug);

                // This section creates the whisperFactory object which is used to create the processor object.
                using var whisperFactory = WhisperFactory.FromPath("ggml-base.bin");

                // This section creates the processor object which is used to process the audio file, it uses language `auto` to detect the language of the audio file.
                using var processor = whisperFactory.CreateBuilder().WithLanguage("auto").Build();

                using var fileStream = File.OpenRead(wavFilePath);

                var userMessage = new StringBuilder();

                // This section processes the audio file and prints the results (start time, end time and text) to the console.
                await foreach (var result in processor.ProcessAsync(fileStream))
                {
                    Debug.WriteLine($"{result.Start}->{result.End}: {result.Text}");
                    userMessage.Append(result.Text);
                }

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
    }
}
