using DiscordEmoteExtractor.Exceptions;
using DiscordEmoteExtractor.Utils;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DiscordEmoteExtractor;

public static class Program
{
	private const string _emoteFileName = "Emote text.txt";
	private const string _emotesFolderName = "Emotes";
	private static readonly Regex _emoteNameRegex = new("(?<=alt=\")[^\"]*", RegexOptions.Compiled);
	private static readonly Regex _emoteUrlRegex = new("(?<=src=\")[^\"]*", RegexOptions.Compiled);
	private static readonly Regex _emoteUrlReplaceSizeRegex = new("(?<=size=)\\d+", RegexOptions.Compiled);

	public static async Task Main()
	{
		if (OperatingSystem.IsWindows())
		{
			Console.Title = Assembly.GetEntryAssembly()?.GetName().Name ?? "Program";
			Console.SetWindowSize(80, 25);
			Console.SetBufferSize(80, 100);
		}

		try
		{
			await Run();
		}
		catch (Exception ex)
		{
			if (ex is EmoteExtractorException)
				ConsoleUtils.WriteLineColor($"\n{ex.Message}");
			else
				ConsoleUtils.WriteLineColor($"\nError occured: {ex.Message}", ConsoleColor.Red);
		}
		finally
		{
			Console.WriteLine("\nPress any key to exit...");
			Console.ReadKey();
		}
	}

	private static async Task Run()
	{
		Ensure.FileExists(_emoteFileName);

		Console.Write("Reading file...");
		Stopwatch sw = Stopwatch.StartNew();

		string content = await Validate.ReadNonEmptyFileAsync(_emoteFileName, $"\nFile is empty. Please paste the content into the file \"{_emoteFileName}\".");

		Console.Write($"Done ({sw.ElapsedMilliseconds}ms)\n");
		Console.Write("\nSearching for emotes...");

		sw.Restart();

		MatchCollection emoteNames = _emoteNameRegex.Matches(content);
		MatchCollection emoteUrls = _emoteUrlRegex.Matches(content);

		Console.Write($"Done ({sw.ElapsedMilliseconds}ms)\n");

		if (emoteNames.Count == 0 || emoteUrls.Count != emoteNames.Count)
			throw new EmoteExtractorException("No emotes found.");

		Console.Write($"Found {emoteNames.Count} emotes\n");

		List<Emote> emoteList = new(Math.Max(emoteNames.Count, emoteUrls.Count));
		for (int i = 0; i < emoteNames.Count; i++)
		{
			if (emoteUrls[i].Value.Contains("icons") || !Uri.TryCreate(emoteUrls[i].Value, UriKind.Absolute, out Uri? uriResult) || uriResult.Scheme != Uri.UriSchemeHttps)
				continue;

			string emoteName = emoteNames[i].Value.Replace(":", string.Empty);
			string emoteUrl = _emoteUrlReplaceSizeRegex.Replace(emoteUrls[i].Value, "1024");
			emoteList.Add(new(emoteName, emoteUrl));
		}

		Ensure.DirectoryExists(_emotesFolderName);
		Ensure.UserWantsToKeepFolderContentsIfTheyExist(_emotesFolderName);

		Console.WriteLine("\nDownloading emotes...");

		sw.Restart();

		using HttpClient client = new();
		List<Task<byte[]>> getByteArrayTasks = new(emoteList.Count);
		getByteArrayTasks.AddRange(emoteList.Select(emote => client.GetByteArrayAsync(emote.Url)));
		byte[][] images = await Task.WhenAll(getByteArrayTasks);

		int counter = 0;
		int quarter = emoteList.Count / 4;
		for (int i = 0; i < emoteList.Count; i++)
		{
			string extension = emoteList[i].Url.Split('?')[0].Split('.')[^1];
			if (extension is not ("png" or "gif" or "webp" or "jpg" or "jpeg"))
				continue;

			extension = extension == "jpeg" ? "jpg" : extension;

			string imagePath = Path.Combine(_emotesFolderName, $"{emoteList[i].Name}.{extension}");
			await Ensure.ByteArrayIsWrittenAsync(imagePath, images[i]);
			counter++;

			if (counter != emoteList.Count && counter % quarter == 0)
				Console.WriteLine($"{(float)counter / emoteList.Count * 100:F0}%...");
		}

		ConsoleUtils.WriteLineColor($"Done ({sw.ElapsedMilliseconds}ms)", ConsoleColor.DarkGreen);
	}

	private readonly record struct Emote(string Name, string Url);
}
