﻿using DiscordEmoteExtractor.Exceptions;
using System.IO;
using System.Threading.Tasks;

namespace DiscordEmoteExtractor.Utils;

public static class Validate
{
	public static async Task<string> ReadFileThrowIfEmpty(string filePath, string? message = null)
	{
		string fileContents = await File.ReadAllTextAsync(filePath);
		if (fileContents.Length == 0)
			throw new EmoteExtractorException(message ?? $"File {filePath} is empty.");

		return fileContents;
	}
}