using System.Globalization;
using GH_IO.Serialization;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Common revision marker for every user-placeable FlahaGrow component.</summary>
public abstract class FlahaGrowComponent : GH_Component
{
    private const string RevisionKey = "FlahaGrow.ComponentRevision";
    private string? savedRevision;

    protected FlahaGrowComponent(string name, string nickname, string description, string category, string subCategory)
        : base(name, nickname, description, category, subCategory) { }

    public ComponentRevision Revision => ComponentRevisionCatalog.Get(ComponentGuid);
    protected override System.Drawing.Bitmap Icon => ComponentIcons.ForComponent(Name)!;
    public string? SavedRevision => savedRevision;
    public bool NeedsRevisionReview => savedRevision is not null && !string.Equals(savedRevision, Revision.Version, StringComparison.Ordinal);

    protected void SetRevisionMessage(string? status = null)
    {
        var prefix = string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim() + " · ";
        var review = NeedsRevisionReview ? $" review {savedRevision}→{Revision.Version}" : string.Empty;
        Message = $"{prefix}{Revision.CanvasLabel}{review}";
    }

    public override void AddedToDocument(GH_Document document)
    {
        base.AddedToDocument(document);
        SetRevisionMessage();
    }

    protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
    {
        base.AppendAdditionalComponentMenuItems(menu);
        var revision = Revision;
        Menu_AppendItem(menu, $"Revision {revision.Version} · updated {revision.UpdatedAt:yyyy-MM-dd HH:mm zzz}", (_, _) => { }, false);
        if (NeedsRevisionReview)
            Menu_AppendItem(menu, $"Saved as {savedRevision}; review this component before replacing it", (_, _) => { }, false);
    }

    public override bool Write(GH_IWriter writer)
    {
        writer.SetString(RevisionKey, Revision.Version);
        return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
        savedRevision = reader.ItemExists(RevisionKey) ? reader.GetString(RevisionKey) : null;
        var result = base.Read(reader);
        SetRevisionMessage();
        return result;
    }
}

public sealed record ComponentRevision(string Version, DateTimeOffset UpdatedAt, string Change)
{
    public string CanvasLabel => $"v{Version} · {UpdatedAt.ToString("MM-dd HH:mm", CultureInfo.InvariantCulture)}";
}

/// <summary>
/// Single component-change ledger. Bump only affected entries when behavior, ports,
/// persistence, or diagnostics change; keep the date and short reason with the bump.
/// </summary>
public static class ComponentRevisionCatalog
{
    private static readonly ComponentRevision Baseline = new("1.0.1", new DateTimeOffset(2026, 9, 11, 12, 48, 0, TimeSpan.FromHours(3)), "Canvas revision labels now include an exact update time.");
    private static readonly ComponentRevision WorkingDirectoryRevision = new("1.0.2", new DateTimeOffset(2026, 9, 11, 12, 50, 0, TimeSpan.FromHours(3)), "Missing workspace manifests provide recovery guidance and no longer block explicit initialization of a legacy folder.");
    private static readonly ComponentRevision AnnualSimulationProgressRevision = new("1.1.0", new DateTimeOffset(2026, 9, 11, 16, 10, 0, TimeSpan.FromHours(3)), "Reports each part's latest 1–8 batch stage and stage coverage instead of only its lifecycle state.");
    private static readonly ComponentRevision SelectorRevision = new("1.0.2", new DateTimeOffset(2026, 9, 11, 17, 45, 0, TimeSpan.FromHours(3)), "Persists the selected library item and re-emits it without reopening its dialog.");
    private static readonly ComponentRevision MaterialTableRevision = new("1.1.0", new DateTimeOffset(2026, 9, 11, 22, 57, 0, TimeSpan.FromHours(3)), "Restores the legacy material-table workflow: preview bitmap, display name, RGB/VLR or RGB/VLT/VLR summary, Python ordering, Select, and Reload.");
    private static readonly ComponentRevision PlantLightRevision = new("1.2.0", new DateTimeOffset(2026, 9, 12, 0, 0, 0, TimeSpan.FromHours(3)), "Adds explicit profile/context readers and shared finite-value photon integration; existing ports and defaults retained. Spectral CSV uses CIE weighting and trapezoidal integration.");
    private static readonly IReadOnlyDictionary<Guid, ComponentRevision> Entries = new Dictionary<Guid, ComponentRevision>
    {
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa410")] = new("1.2.1", new DateTimeOffset(2026, 9, 12, 14, 0, 0, TimeSpan.FromHours(3)), "Renamed Custom Spectral Profile; existing GUID and five inputs retained."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa417")] = new("1.0.1", new DateTimeOffset(2026, 9, 12, 15, 0, 0, TimeSpan.FromHours(3)), "Acknowledged research limitations remain in Status without repeated runtime warning; readable wavelength coverage."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa411")] = new("1.4.0", new DateTimeOffset(2026, 9, 12, 18, 0, 0, TimeSpan.FromHours(3)), "Background cache loading, run-derived weather/time, typed result and data-bound plot attributes; appended compatibility ports."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa412")] = new("1.3.0", new DateTimeOffset(2026, 9, 12, 15, 0, 0, TimeSpan.FromHours(3)), "Strict Hour Index alignment contract, sensor-order guidance; acknowledged assumptions retained in Status."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa413")] = new("1.4.0", new DateTimeOffset(2026, 9, 12, 18, 0, 0, TimeSpan.FromHours(3)), "Background cache loading, run-derived weather/time, typed result and data-bound plot attributes; appended compatibility ports."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa414")] = new("1.4.0", new DateTimeOffset(2026, 9, 12, 18, 0, 0, TimeSpan.FromHours(3)), "Background cache loading, run-derived weather/time, typed result and data-bound plot attributes; appended compatibility ports."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa415")] = new("1.4.0", new DateTimeOffset(2026, 9, 12, 18, 0, 0, TimeSpan.FromHours(3)), "Background cache loading, run-derived weather/time, typed result and data-bound plot attributes; appended compatibility ports."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa416")] = new("1.4.0", new DateTimeOffset(2026, 9, 12, 18, 0, 0, TimeSpan.FromHours(3)), "Background cache loading, run-derived weather/time, typed result and data-bound plot attributes; appended compatibility ports."),
        [new("f32f1cbd-04b5-42ed-9fdf-c194851011b2")] = new("1.3.0", new DateTimeOffset(2026, 9, 13, 16, 0, 0, TimeSpan.FromHours(3)), "Computes the annual mean without overflowing the sum of finite daily values."),
        [new("5747b67c-4aec-4117-83a2-5e30a7308920")] = new("1.6.0", new DateTimeOffset(2026, 9, 13, 16, 0, 0, TimeSpan.FromHours(3)), "Rejects connected invalid or unresolved Plot Attributes instead of falling back to numeric-only plotting."),
        [new("5f1d58a4-064f-4fc9-b79b-640a380a3e43")] = new("1.1.0", new DateTimeOffset(2026, 9, 13, 16, 0, 0, TimeSpan.FromHours(3)), "Rejects nonfinite power/timestep and arithmetic overflow before publishing energy or duration."),
        [new("0e5f7114-fbb9-4a77-a3f4-40ccd0c0c258")] = new("1.4.0", new DateTimeOffset(2026, 9, 12, 18, 0, 0, TimeSpan.FromHours(3)), "Background cache loading, run-derived weather/time, typed result and data-bound plot attributes; appended compatibility ports."),
        [new("ca2ce6ef-a0c8-4d98-87a3-2adf2a91ca45")] = new("1.6.0", new DateTimeOffset(2026, 9, 13, 15, 0, 0, TimeSpan.FromHours(3)), "Shared bounded Radiance discovery; explicit locations never fall back to another installation; no fixed drive paths."),
        [new("1a2d08d7-6d3e-459e-a2c2-62636cbbaf24")] = AnnualSimulationProgressRevision,
        [new("31b19b55-7384-4f37-bb1a-f436c3cbaa8b")] = Baseline,
        [new("0f9f53a1-6fe6-4a4f-ab14-ee19f53c4fdd")] = new("1.1.0", new DateTimeOffset(2026, 9, 13, 16, 0, 0, TimeSpan.FromHours(3)), "Rejects nonfinite DLI values and targets."),
        [new("53402415-6620-4ecd-bfa3-593e7a148f29")] = MaterialTableRevision,
        [new("492e14e7-163e-4c2a-a6d8-c44184da664d")] = SelectorRevision,
        [new("e64e15f4-7cee-48b2-a232-2064d3a9e602")] = new("1.7.0", new DateTimeOffset(2026, 9, 13, 16, 0, 0, TimeSpan.FromHours(3)), "Background edge-triggered IES conversion with bounded process I/O, isolated fresh outputs, input validation and primitive-aware RGB rewriting."),
        [new("3d38a66d-b381-45f2-ad70-57e6be84a6cc")] = new("1.6.0", new DateTimeOffset(2026, 9, 13, 16, 0, 0, TimeSpan.FromHours(3)), "Rejects unknown modes; uses shared locked cache validation including manifest dimensions and order."),
        [new("2bb0d862-d310-4c90-8836-3760fd9870c5")] = new("1.1.0", new DateTimeOffset(2026, 9, 13, 16, 0, 0, TimeSpan.FromHours(3)), "Rejects invalid placement geometry, nonfinite rotations, missing files and unsafe command path characters."),
        [new("ac8f8d0f-c1d7-480c-8f37-4fe4c76247aa")] = new("2.0.0", new DateTimeOffset(2026, 9, 13, 15, 0, 0, TimeSpan.FromHours(3)), "Requires an explicit spectrum-specific factor; no implicit numeric default."),
        [new("29e2836f-5da0-4e4c-bdac-990365a0471e")] = new("1.5.0", new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.FromHours(3)), "Consolidated duplicate components into one tool with shared behavior; canonical GUID and port order retained."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa418")] = new("2.0.0", new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.FromHours(3)), "Removed unused UTC port; Result now follows Run. New GUID prevents misreading older saved wires."),
        [new("e0c7494d-bf04-4bd1-a9ed-9184fd2b9b53")] = new("1.1.0", new DateTimeOffset(2026, 9, 13, 16, 0, 0, TimeSpan.FromHours(3)), "Rejects nonfinite or overflowing marker sizes before geometry operations."),
        [new("71ce89f2-1439-4730-915f-07436692926c")] = Baseline,
        [new("d236c57b-eab1-4329-8e4e-beb2285ba04d")] = WorkingDirectoryRevision,
        [new("9cd39fc4-7c35-4aee-a247-1c8b980c4b17")] = Baseline,
        [new("b87a6c40-49df-4aef-9ee4-99d5d806bb2d")] = new("1.6.0", new DateTimeOffset(2026, 9, 13, 15, 0, 0, TimeSpan.FromHours(3)), "Shared bounded Radiance discovery; explicit locations never fall back to another installation; no fixed drive paths."),
        [new("9f5f9a71-0ec2-4fb7-a481-49625f0871f2")] = PlantLightRevision,
    };

    public static ComponentRevision Get(Guid componentGuid) => Entries.TryGetValue(componentGuid, out var revision)
        ? revision : throw new InvalidOperationException($"No revision entry is registered for FlahaGrow component {componentGuid}.");
    public static bool Contains(Guid componentGuid) => Entries.ContainsKey(componentGuid);
    public static int Count => Entries.Count;
}
