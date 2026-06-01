using System.Globalization;

public class DateTimeTools
{
    public string GetDate() =>
        DateTime.Now.ToString("dddd, MMMM dd, yyyy", CultureInfo.GetCultureInfo("en-US"));

    public string GetTime() =>
        DateTime.Now.ToString("HH:mm:ss");

    public string Execute(string name) => name switch
    {
        "GetDate" => GetDate(),
        "GetTime" => GetTime(),
        _ => $"Unknown tool: {name}"
    };
}
