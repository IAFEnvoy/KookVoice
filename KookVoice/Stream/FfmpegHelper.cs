using System.Diagnostics;

namespace KookVoice.Stream {
    public class FfmpegHelper {
        private static string ffmpegPath = Environment.CurrentDirectory + @"\ffmpeg.exe";
        public static void PlayMusic(string rtcpUrl, string musicUrl) {
            Process ffmpeg1 = new();
            ffmpeg1.StartInfo.FileName = ffmpegPath;
            ffmpeg1.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            ffmpeg1.StartInfo.Arguments = string.Join(" ", "-re", "-loglevel", "level+info", "-nostats", "-i", "-", "-map", "0:a:0", "-acodec", "libopus", "-ab", "128k", "-ac", "2", "-ar", "48000", "-f", "tee", $"[select=a:f=rtp:ssrc=1357:payload_type=100]{rtcpUrl}");
            Process ffmpeg2 = new();
            ffmpeg2.StartInfo.FileName = ffmpegPath;
            ffmpeg2.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            ffmpeg2.StartInfo.Arguments = string.Join(" ", "-nostats", "-i", musicUrl, "-filter:a", "volume=0.4", "-ss ${0}", "-format", "pcm_s16le", "-ac", "2", "", "-ar", "48000", "-f", "wav", "-");

            ffmpeg2.OutputDataReceived += (_, e) => {
                ffmpeg1.StandardInput.Write(e.Data);
            };
            ffmpeg1.Start();
            ffmpeg2.Start();
        }
    }
}
