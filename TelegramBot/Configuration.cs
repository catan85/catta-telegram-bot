using System;
using System.Collections.Generic;
using System.Text;

public static class Configuration
{
    public readonly static string BotToken = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");
    public readonly static string TempFileFolder = "/tmp/";
}