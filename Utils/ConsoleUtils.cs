namespace DiscordEmoteExtractor.Utils;

public static class ConsoleUtils
{
	public static void WriteLineColor(string text, ConsoleColor color = ConsoleColor.Blue)
	{
		Console.ForegroundColor = color;
		Console.WriteLine(text);
		Console.ResetColor();
	}
}
