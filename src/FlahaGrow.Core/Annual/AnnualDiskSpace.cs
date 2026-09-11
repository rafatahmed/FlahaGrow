namespace FlahaGrow.Core.Annual;

/// <summary>Conservative working-space guard for an annual Radiance run.</summary>
public static class AnnualDiskSpace
{
    private const long BaseReserveBytes = 1L << 30; // logs, scene copies, weather/sun matrices, and filesystem overhead
    private const long BytesPerSensorHour = 200; // scalar intermediate matrices and parallel-part overhead

    public static long RequiredFreeBytes(int hours, int sensors)
    {
        if (hours <= 0) throw new ArgumentOutOfRangeException(nameof(hours));
        if (sensors <= 0) throw new ArgumentOutOfRangeException(nameof(sensors));
        return checked(BaseReserveBytes + checked((long)hours * sensors * BytesPerSensorHour));
    }

    public static void Require(string runContainer, int hours, int sensors)
    {
        var fullPath = Path.GetFullPath(runContainer);
        var root = Path.GetPathRoot(fullPath);
        if (string.IsNullOrWhiteSpace(root)) throw new IOException("Could not determine the drive for the annual run folder.");
        var drive = new DriveInfo(root);
        if (!drive.IsReady) throw new IOException($"Annual run drive is not ready: {root}");
        var required = RequiredFreeBytes(hours, sensors);
        if (drive.AvailableFreeSpace >= required) return;
        throw new IOException($"Insufficient disk space for Annual Simulation on {drive.Name}. Available {Format(drive.AvailableFreeSpace)}; at least {Format(required)} is required before launching. Remove terminal runs or choose a run folder on a drive with enough free space.");
    }

    private static string Format(long bytes) => (bytes / (double)(1L << 30)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " GB";
}
