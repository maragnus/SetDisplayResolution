using PowerArgs;
using System.Diagnostics;
using System.Runtime.InteropServices;

var appArgs = new AppArgs();

try
{
    appArgs = Args.Parse<AppArgs>(args);
}
catch (ArgException ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<AppArgs>());
    return;
}

// Remember current resolution
var currentMode = Resolution.CurrentMode();

// Find best mode that the user wants
var idealMode = Resolution.AvailableModes()
    .FirstOrDefault(mode =>
        mode.dmPelsWidth == appArgs.Width
        && mode.dmPelsHeight == appArgs.Height
        && mode.dmBitsPerPel == currentMode.dmBitsPerPel
        && mode.dmDisplayFrequency == currentMode.dmDisplayFrequency
    );

// Fail if we didn't find one
if (idealMode.dmPelsWidth == 0)
{
    Console.Error.WriteLine($"Could not find a mode that matched {appArgs.Width}x{appArgs.Height}");
    return;
}

// Change the resolution
Resolution.SetMode(idealMode);

// Start the process (game)
var startInfo = new ProcessStartInfo()
{
    FileName = appArgs.CommandLine
};
var process = Process.Start(startInfo)!;
await process.WaitForExitAsync();

// Change the resolution back
Resolution.SetMode(currentMode);

/// <summary>
/// This class holds the command-line arguments from PowerArgs
/// </summary>
[ArgExample(@"Set-DisplayResolution 1920 1080 C:\Diablo2\d2.exe", "Start Diablo II in 1920x1080 resolution")]
public class AppArgs
{
    [ArgPosition(0), ArgRequired, ArgDescription("Display Width")]
    public int Width { get; set; }

    [ArgPosition(1), ArgRequired, ArgDescription("Display Height")]
    public int Height { get; set; }

    [ArgPosition(2), ArgRequired, ArgDescription("Path to application to start")]
    public string CommandLine { get; set; } = null!;
}

/// <summary>
/// This class has the Windows API methods and stuff
/// </summary>
public static class Resolution
{
    const int _currentSetting = -1;

    [DllImport("user32.dll")]
    static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);

    [DllImport("user32.dll")]
    static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);

    public static void SetMode(DEVMODE mode)
    {
        ChangeDisplaySettings(ref mode, 0);
    }

    public static DEVMODE CurrentMode()
    {
        DEVMODE devMode = default;
        devMode.dmSize = (short)(Marshal.SizeOf(devMode));
        EnumDisplaySettings(null, _currentSetting, ref devMode);
        return devMode;
    }

    public static IEnumerable<DEVMODE> AvailableModes()
    {
        var index = 0;

        while (true)
        {
            DEVMODE devMode = default;
            devMode.dmSize = (short)(Marshal.SizeOf(devMode));
            if (!EnumDisplaySettings(null, index++, ref devMode))
                yield break;

            yield return devMode;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }
}