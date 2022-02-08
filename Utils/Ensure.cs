namespace DiscordEmoteExtractor.Utils;

public static class Ensure
{
	public static void FileExists(string filePath)
	{
		if (!File.Exists(filePath))
			File.Create(filePath);
	}

	public static void DirectoryExists(string directoryPath)
	{
		if (!Directory.Exists(directoryPath))
			Directory.CreateDirectory(directoryPath);
	}

	public static void DirectoryIsCleared(string directoryPath)
	{
		if (Directory.Exists(directoryPath))
			Directory.Delete(directoryPath, recursive: true);

		Directory.CreateDirectory(directoryPath);
	}

	public static async Task ByteArrayIsWrittenAsync(string filePath, byte[] array)
	{
		if (!File.Exists(filePath))
		{
			await File.WriteAllBytesAsync(filePath, array);
			return;
		}

		string? fileDirectory = Path.GetDirectoryName(filePath);
		string fileNameNoExtension = Path.GetFileNameWithoutExtension(filePath);
		string fileExtension = Path.GetExtension(filePath);
		int counter = 1;
		while (File.Exists(filePath))
		{
			filePath = Path.Combine(fileDirectory ?? string.Empty, $"{fileNameNoExtension}({counter++}){fileExtension}");
		}

		await File.WriteAllBytesAsync(filePath, array);
	}

	public static void UserWantsToKeepFolderContentsIfTheyExist(string directoryPath)
	{
		if (!Directory.EnumerateFiles(directoryPath).Any())
			return;

		string? userResponse = null;
		while (string.IsNullOrWhiteSpace(userResponse) || userResponse is not ("y" or "Y" or "n" or "N"))
		{
			ConsoleUtils.WriteLineColor($"\nFound existing files in the \"{directoryPath}\" directory.\nDo you want to keep them? (y/n)", ConsoleColor.Yellow);
			userResponse = Console.ReadLine();
		}

		if (userResponse.Equals("n", StringComparison.InvariantCultureIgnoreCase))
			DirectoryIsCleared(directoryPath);
	}
}
