using CliWrap;
using System.Text.RegularExpressions;

var folder = "";
while (string.IsNullOrWhiteSpace(folder))
{
    if (args.Length == 0)
    {
        Console.WriteLine("Please provide a folder to search");
        folder = Console.ReadLine();
    }
    else
    {
        folder = args[0];
    }
}

await using var stdOut = Console.OpenStandardOutput();
await using var stdErr = Console.OpenStandardError();

var yt = new FileInfo("yt-dlp.exe");

if (!yt.Exists)
{
    Console.WriteLine("Downloading yt-dlp...");
    using var client = new HttpClient();
    using var stream = new FileStream("yt-dlp.exe", FileMode.Create, FileAccess.Write, FileShare.None);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36 Edge/16.16299");
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new("application/octet-stream"));
    using var response = await client.GetStreamAsync("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe");
    await response.CopyToAsync(stream);
    Console.WriteLine("Downloaded yt-dlp");
    yt.Refresh();
}

var files = Directory.GetFiles(folder, "*.txt", SearchOption.AllDirectories);
Console.WriteLine($"Processing {files.Length} files");
Console.WriteLine();

foreach (var file in files.Select(x => new FileInfo(x)))
{
    try
    {
        Console.WriteLine($"# Start {file.Name}");

        var mp3Name = "";
        var videoId = "";
        foreach (var line in File.ReadLines(file.FullName))
        {
            var mp3NameMatch = Mp3NameRegex().Match(line);

            if (mp3NameMatch.Success)
            {
                mp3Name = mp3NameMatch.Groups[1].Value;
            }

            var match = VideoIdRegex().Match(line);
            if (match.Success)
            {
                videoId = match.Groups[1].Value;
            }

            if (!string.IsNullOrWhiteSpace(mp3Name) && !string.IsNullOrWhiteSpace(videoId))
            {
                if (!mp3Name.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                    mp3Name += ".mp3";

                var mp3NameFull = Path.Combine(file.DirectoryName!, mp3Name);

                if (File.Exists(mp3NameFull))
                {
                    Console.WriteLine($"# {mp3Name} already exists");
                    mp3Name = "";
                    videoId = "";
                    continue;
                }

                var cmd = Cli
                    .Wrap("yt-dlp.exe")
                    .WithArguments($"https://youtu.be/{videoId} -x --audio-format mp3 -o \"{mp3NameFull}\"")
                    | (stdOut, stdErr);
                await cmd.ExecuteAsync();

                mp3Name = "";
                videoId = "";
            }
        }

        Console.WriteLine($"# Finish {file.Name}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"# Error reading {file.FullName}: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("Finished");

partial class Program
{

    [GeneratedRegex("#VIDEO:[va]=([A-Za-z0-9_-]+)")]
    public static partial Regex VideoIdRegex();

    [GeneratedRegex("#MP3:(.*)")]
    public static partial Regex Mp3NameRegex();
}