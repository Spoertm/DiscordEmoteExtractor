using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordEmoteExtractor
{
	public class Program
	{
		private static readonly string emoteTextPath = Path.Combine(AppContext.BaseDirectory, "Emote text.txt");
		private static readonly string emoteFolderPath = Path.Combine(AppContext.BaseDirectory, "Emotes");
		private static readonly HttpClient client = new();
		private static int counter = 0;
		private static readonly Regex emoteNameRegex = new("(?<=alt=\")[^\"]*", RegexOptions.Compiled);
		private static readonly Regex emoteUrlRegex = new("(?<=src=\")[^\"]*", RegexOptions.Compiled);

		public static async Task Main()
		{
			Directory.Delete(emoteFolderPath, recursive: true);
			Directory.CreateDirectory(emoteFolderPath);

			Stopwatch sw = Stopwatch.StartNew();

			Console.WriteLine("Reading text from file...");

			string content = File.ReadAllText(emoteTextPath);

			Console.WriteLine($"Finished in {sw.ElapsedMilliseconds}ms.\n");

			if (string.IsNullOrWhiteSpace(content))
			{
				Console.WriteLine("File is empty.");
				Environment.Exit(0);
			}

			sw.Restart();
			Console.WriteLine("Matching with Regex...");

			MatchCollection emoteNames = emoteNameRegex.Matches(content);
			MatchCollection emoteUrls = emoteUrlRegex.Matches(content);

			Console.WriteLine($"Finished in {sw.ElapsedMilliseconds}ms.\n");

			if (emoteNames.Count == 0)
			{
				Console.WriteLine("No emotes found.");
				Environment.Exit(0);
			}

			List<Emote> emoteList = new();
			for (int i = 0; i < emoteNames.Count; i++)
				emoteList.Add(new(emoteNames[i].Value.Replace(":", string.Empty), emoteUrls[i].Value));

			sw.Restart();
			Console.WriteLine("Saving emotes...");

			await SaveAllEmotes(emoteList);
			Console.WriteLine($"Finished in {sw.ElapsedMilliseconds}ms.");
			sw.Stop();

			client.Dispose();
		}

		public static async Task SaveAllEmotes(List<Emote> emoteList)
		{
			int quarter = emoteList.Count / 4;
			foreach (Emote emote in emoteList)
			{
				await SaveEmoteAsync(emote);

				if (counter % quarter == 0)
					Console.WriteLine($"{(float)counter / emoteList.Count * 100:F0}% done...");
			}
		}
		public static async Task SaveEmoteAsync(Emote emote)
		{
			string extension = emote.Url[^7..] switch
			{
				"png?v=1" => ".png",
				"gif?v=1" => ".gif",
				_ => null,
			};

			if (extension is null)
				return;

			byte[] image = await client.GetByteArrayAsync(emote.Url);
			string imagePath = Path.Combine(emoteFolderPath, emote.Name + extension);
			File.WriteAllBytes(imagePath, image);
			counter++;
		}
	}

	public record Emote(string Name, string Url);
}
