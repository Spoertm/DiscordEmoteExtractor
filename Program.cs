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
			try
			{
				await RunExtractionProcedure();
			}
			catch (Exception ex)
			{
				WriteError($"Failed to extract emotes.\nError: {ex.Message ?? ex.ToString()}", ConsoleColor.DarkRed);
				Console.WriteLine("\nPress any key to exit...");
				Console.ReadKey();
				Environment.Exit(0);
			}
		}

		private static async Task RunExtractionProcedure()
		{
			Stopwatch sw = Stopwatch.StartNew();

			Console.Write("Reading text from file...");

			string content = File.ReadAllText(emoteTextPath);

			Console.Write($"Done. ({sw.ElapsedMilliseconds}ms)\n\n");

			if (string.IsNullOrWhiteSpace(content))
			{
				WriteError("File is empty.");
				Console.WriteLine("\nPress any key to exit...");
				Console.ReadKey();
				Environment.Exit(0);
			}

			sw.Restart();
			Console.Write("Matching with Regex...");

			MatchCollection emoteNames = emoteNameRegex.Matches(content);
			MatchCollection emoteUrls = emoteUrlRegex.Matches(content);

			Console.Write($"Done. ({sw.ElapsedMilliseconds}ms)\n\n");

			if (emoteNames.Count == 0)
			{
				WriteError("No emotes found.");
				Console.WriteLine("\nPress any key to exit...");
				Console.ReadKey();
				Environment.Exit(0);
			}

			List<Emote> emoteList = new();
			for (int i = 0; i < emoteNames.Count; i++)
				emoteList.Add(new(emoteNames[i].Value.Replace(":", string.Empty), emoteUrls[i].Value));

			sw.Restart();
			Console.WriteLine("Saving emotes...");

			if (Directory.Exists(emoteFolderPath))
				Directory.Delete(emoteFolderPath, recursive: true);

			Directory.CreateDirectory(emoteFolderPath);

			await SaveAllEmotes(emoteList);
			WriteError($"Done. ({sw.ElapsedMilliseconds}ms)", ConsoleColor.DarkGreen);
			sw.Stop();

			client.Dispose();
			Console.WriteLine("\nPress any key to exit...");
			Console.ReadKey();
		}

		private static void WriteError(string text, ConsoleColor color = ConsoleColor.Blue)
		{
			Console.ForegroundColor = color;
			Console.WriteLine(text);
			Console.ResetColor();
		}

		private static async Task SaveAllEmotes(List<Emote> emoteList)
		{
			int quarter = emoteList.Count / 4;
			foreach (Emote emote in emoteList)
			{
				await SaveEmoteAsync(emote);

				if (counter % quarter == 0)
					Console.WriteLine($"{(float)counter / emoteList.Count * 100:F0}% done...");
			}
		}
		private static async Task SaveEmoteAsync(Emote emote)
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
