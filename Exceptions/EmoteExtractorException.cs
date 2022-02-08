using System;

namespace DiscordEmoteExtractor.Exceptions;

public class EmoteExtractorException : Exception
{
	public EmoteExtractorException(string message)
		: base(message)
	{
	}
}
