using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper;

/// <summary>Exact-name artwork mapping; originals remain embedded and unchanged.</summary>
public static class ComponentIcons
{
    private const string Prefix = "FlahaGrow.Icons.";
    private static readonly HashSet<string> Resources = typeof(ComponentIcons).Assembly.GetManifestResourceNames().ToHashSet(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, Lazy<Bitmap>> Cache = new(StringComparer.Ordinal);
    public static Bitmap? ForComponent(string name) => Load(name);
    public static Bitmap Logo => Load("FlahaGrow_Icon_logo") ?? throw new InvalidDataException("Missing FlahaGrow logo resource.");
    private static Bitmap? Load(string name)
    {
        var resource = Prefix + name + ".png";
        if (!Resources.Contains(resource)) return null;
        return Cache.GetOrAdd(resource, key => new Lazy<Bitmap>(() => Render(key))).Value;
    }
    private static Bitmap Render(string resource)
    {
        using var stream = typeof(ComponentIcons).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidDataException("Missing icon resource: " + resource);
        using var original = new Bitmap(stream);
        var icon = new Bitmap(24, 24, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(icon);
        graphics.Clear(Color.Transparent);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        using var attributes = new ImageAttributes();
        attributes.SetWrapMode(WrapMode.TileFlipXY);
        var scale = Math.Min(24d / original.Width, 24d / original.Height);
        var width = Math.Max(1, (int)Math.Round(original.Width * scale));
        var height = Math.Max(1, (int)Math.Round(original.Height * scale));
        graphics.DrawImage(original, new Rectangle((24 - width) / 2, (24 - height) / 2, width, height), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
        return icon;
    }
}

public sealed class FlahaGrowAssemblyPriority : GH_AssemblyPriority
{
    public override GH_LoadingInstruction PriorityLoad()
    {
        global::Grasshopper.Instances.ComponentServer.AddCategoryIcon("FlahaGrow", ComponentIcons.Logo);
        return GH_LoadingInstruction.Proceed;
    }
}
