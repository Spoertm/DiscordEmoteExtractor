using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordEmoteExtractor
{
	public static class Program
	{
		private const string _emoteFileName = "Emote text.txt";
		private const string _emotesFolderName = "Emotes";
		private static readonly Regex _emoteNameRegex = new("(?<=alt=\")[^\"]*", RegexOptions.Compiled);
		private static readonly Regex _emoteUrlRegex = new("(?<=src=\")[^\"]*", RegexOptions.Compiled);

		public static async Task Main()
		{
			try
			{
				await RunExtractionProcedure();
			}
			catch (Exception ex)
			{
				WriteLineColor($"Error occured: {ex.Message}", ConsoleColor.Red);
			}

			Console.WriteLine("Press any key to exit the application...");
			Console.ReadKey();
		}

		private static async Task RunExtractionProcedure()
		{
			if (!File.Exists(_emoteFileName))
			{
				WriteLineColor($"No emote text file found.\nFile \"{_emoteFileName}\" will be created for you. Please paste the content in it and run the program again.");
				File.Create(_emoteFileName);
				return;
			}

			Console.Write("Reading text from file...");

			Stopwatch sw = Stopwatch.StartNew();
			string content = await File.ReadAllTextAsync(_emoteFileName);
			Console.Write($"Done ({sw.ElapsedMilliseconds}ms)\n\n");
			if (string.IsNullOrWhiteSpace(content))
				throw new($"File is empty. Please paste the content into the file \"{_emoteFileName}\".");

			Console.Write("Matching with regex...");

			sw.Restart();
			MatchCollection emoteNames = _emoteNameRegex.Matches(content);
			MatchCollection emoteUrls = _emoteUrlRegex.Matches(content);

			Console.Write($"Done ({sw.ElapsedMilliseconds}ms)\n\n");
			if (emoteNames.Count == 0 || emoteUrls.Count != emoteNames.Count)
				throw new("No emotes found.");

			List<Emote> emoteList = new();
			for (int i = 0; i < emoteNames.Count; i++)
			{
				string emoteName = emoteNames[i].Value;
				emoteList.Add(new(emoteName.Replace(":", string.Empty), emoteUrls[i].Value));
			}

			if (Directory.Exists(_emotesFolderName))
				Directory.Delete(_emotesFolderName, recursive: true);

			Directory.CreateDirectory(_emotesFolderName);

			Console.WriteLine("Downloading emotes...");

			sw.Restart();
			using HttpClient client = new();
			int counter = 0;
			int quarter = emoteList.Count / 4;
			byte[][] images = await Task.WhenAll(emoteList.Select(e => client.GetByteArrayAsync(e.Url)));
			for (int i = 0; i < emoteList.Count; i++)
			{
				string extension = emoteList[i].Url[^7..] switch
				{
					"png?v=1" => ".png",
					"gif?v=1" => ".gif",
					_         => null,
				};

				if (extension is null)
					continue;

				string imagePath = Path.Combine(_emotesFolderName, emoteList[i].Name + extension);
				await File.WriteAllBytesAsync(imagePath, images[i]);
				counter++;

				if (counter % quarter == 0)
					Console.WriteLine($"{(float)counter / emoteList.Count * 100:F0}% done...");
			}

			WriteLineColor($"Done ({sw.ElapsedMilliseconds}ms)", ConsoleColor.DarkGreen);
		}

		private static void WriteLineColor(string text, ConsoleColor color = ConsoleColor.Blue)
		{
			Console.ForegroundColor = color;
			Console.WriteLine(text);
			Console.ResetColor();
		}

		private readonly struct Emote
		{
			public string Name { get; }
			public string Url { get; }

			public Emote(string name, string url)
			{
				Name = name;
				Url = url;
			}
		}
	}
}
