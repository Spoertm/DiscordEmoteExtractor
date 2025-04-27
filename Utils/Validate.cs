using DiscordEmoteExtractor.Exceptions;

namespace DiscordEmoteExtractor.Utils;

public static class Validate
{
	/// <summary>
	/// Reads the contents of a file and throws an exception if the file is empty.
	/// </summary>
	/// <param name="filePath">The path to the file to check.</param>
	/// <param name="message">
	/// Optional custom error message to use if the file is empty.
	/// If not provided, a default message will be used.
	/// </param>
	/// <returns>The contents of the file as a string.</returns>
	/// <exception cref="EmoteExtractorException">Thrown when the file is empty.</exception>
	public static async Task<string> ReadNonEmptyFileAsync(string filePath, string? message = null)
	{
		string fileContents = await File.ReadAllTextAsync(filePath);
		if (fileContents.Length == 0)
			throw new EmoteExtractorException(message ?? $"File \"{filePath}\" is empty.");

		return fileContents;
	}
}
