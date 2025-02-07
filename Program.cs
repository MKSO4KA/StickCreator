
using System.Globalization;
using Xabe.FFmpeg;

class VideoProcessor
{
    readonly static int maxsize = 256*1024; // 256 Килобайт
    public static async Task VideoBwebm(string videoName)
    {
        int bitrate = 800;
        DeleteFiles(videoName, new int[] { 2, 3, 4 });
        if (!File.Exists($"{videoName}.mp4"))
        {
            Console.WriteLine($"File {videoName}.mp4 doesnt exists");
            return;
        }
        // Step 1: Scale the video
        await ScaleVideo($"{videoName}.mp4", $"{videoName}2.mp4");
        if (!File.Exists($"{videoName}2.mp4"))
        {
            Console.WriteLine($"File {videoName}2.mp4 doesnt exists");
            return;
        }
        // Step 2: Speed up the video
        await SpeedUp($"{videoName}2.mp4", $"{videoName}3.mp4", 2.9);
        if (!File.Exists($"{videoName}3.mp4"))
        {
            Console.WriteLine($"File {videoName}3.mp4 doesnt exists");
            return;
        }
        // Step 3: Convert to webm format

        await ConvertToWebM($"{videoName}3.mp4", $"{videoName}4.webm", bitrate);
        if (!File.Exists($"{videoName}4.webm"))
        {
            Console.WriteLine($"File {videoName}4.webm doesnt exists");
            return;
        }
        // Step 4: Check file size and adjust bitrate if necessary
        long size = new FileInfo($"{videoName}4.webm").Length;
        if (size > maxsize)
        {
            while (size > maxsize)
            {
                Console.WriteLine($"Размер равный - {Math.Round((double)size / 1024)} - больше {maxsize / 1024}");
                if (Math.Round(size / 1024.0) - 256 >= 50)
                {
                    bitrate -= 100;
                }
                else
                {
                    bitrate -= 25;
                }

                File.Delete($"{videoName}4.webm");
                await ConvertToWebM($"{videoName}3.mp4", $"{videoName}4.webm", bitrate);
                if (!File.Exists($"{videoName}4.webm"))
                {
                    Console.WriteLine($"File {videoName}4.webm doesnt exists after cycle");
                    return;
                }
                size = new FileInfo($"{videoName}4.webm").Length;
            }
        }
        DeleteFiles(videoName, new int[] { 2, 3 });
    }
    
    public static async Task SpeedUp(string input, string output, double sec)
    {
        var mediaInfo = await FFmpeg.GetMediaInfo(input);
        var duration = mediaInfo.Duration.TotalSeconds;

        double speedFactor = duration <= sec ? 1 : Math.Round(duration / (sec - 0.01), 3);
        // Speed up the video using setpts filter
        await FFmpeg.Conversions.New()
            .AddParameter($"-i {input}")
            .AddParameter($"-filter:v \"setpts={Math.Round(1 / speedFactor, 5).ToString("F5", CultureInfo.InvariantCulture)}*PTS\"")
            .SetOutput(output)
            .Start();
    }

    public static async Task ScaleVideo(string input, string output)
    {
        // Scale the video using scale filter
        await FFmpeg.Conversions.New()
            .AddParameter($"-i {input}")
            .AddParameter($"-vf scale=512:512")
            .SetOutput(output)
            .Start();
    }

    public static async Task ConvertToWebM(string input, string output, int bitrate)
    {
        await FFmpeg.Conversions.New()
            .AddParameter($"-i {input}")
            .AddParameter($"-t 3")
            .AddParameter($"-vf scale=512:512")
            .AddParameter($"-c:v libvpx-vp9")
            .AddParameter($"-b:v {bitrate}k")
            .AddParameter($"-an")
            .SetOutput(output)
            .Start();
    }
    public static bool DeleteFiles(string videoName, int[] Numbers)
    {
        foreach (var item in Numbers)
        {
            if (!File.Exists($"{videoName}{item}.webm") && !File.Exists($"{videoName}{item}.mp4"))
            {
                continue;
            }
            if (File.Exists($"{videoName}{item}.webm"))
            {
                File.Delete($"{videoName}{item}.webm");
            }
            if (File.Exists($"{videoName}{item}.mp4"))
            {
                File.Delete($"{videoName}{item}.mp4");
            }
        }
        foreach (var item in Numbers)
        {
            if (File.Exists($"{videoName}{item}.webm") || File.Exists($"{videoName}{item}.mp4"))
            {
                return false;
            }
        }
        return true;
    }
    static async Task Main(string[] args)
    {
        // Example usage
        string? path;
        string[] pathArray;
        Console.WriteLine("Укажите путь к файлу");
        path = Console.ReadLine();
        if (path == null) { return; }
        pathArray = path.Split((char)'.');
        path = string.Join(".", pathArray.Take(pathArray.Length - 1)).Replace("\"", "");
        await VideoBwebm(path);
    }
}
