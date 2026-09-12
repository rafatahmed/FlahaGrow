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
    private static readonly ComponentRevision AnnualSimulationRevision = new("1.6.0", new DateTimeOffset(2026, 9, 11, 19, 20, 0, TimeSpan.FromHours(3)), "Uses Ladybug-compatible strict EPW-to-WEA conversion, a verified 146-column ground-plus-sky receiver basis, storage safeguards, and verified persisted-process cancellation.");
    private static readonly ComponentRevision AnnualSimulationProgressRevision = new("1.1.0", new DateTimeOffset(2026, 9, 11, 16, 10, 0, TimeSpan.FromHours(3)), "Reports each part's latest 1–8 batch stage and stage coverage instead of only its lifecycle state.");
    private static readonly ComponentRevision AnnualPlotRevision = new("1.0.1", new DateTimeOffset(2026, 9, 11, 21, 47, 0, TimeSpan.FromHours(3)), "Infers and displays 8,760 hourly, 365 daily, or 12 monthly annual series without changing the existing ports.");
    private static readonly ComponentRevision DliRevision = new("1.1.0", new DateTimeOffset(2026, 9, 11, 17, 40, 0, TimeSpan.FromHours(3)), "Validates complete 365-day PPFD series against its timestep instead of assuming hourly samples.");
    private static readonly ComponentRevision AnnualDliNavigationRevision = new("1.1.1", new DateTimeOffset(2026, 9, 11, 22, 47, 0, TimeSpan.FromHours(3)), "Moved from the mixed Metrics panel to the DLI panel; ports and calculation are unchanged.");
    private static readonly ComponentRevision DliTargetNavigationRevision = new("1.0.1", new DateTimeOffset(2026, 9, 11, 22, 47, 0, TimeSpan.FromHours(3)), "Moved from the mixed Metrics panel to the DLI panel; ports and calculation are unchanged.");
    private static readonly ComponentRevision LightingEnergyNavigationRevision = new("1.0.1", new DateTimeOffset(2026, 9, 11, 22, 47, 0, TimeSpan.FromHours(3)), "Moved from the mixed Metrics panel to the Energy panel; ports and calculation are unchanged.");
    private static readonly ComponentRevision LuxToPpfdNavigationRevision = new("1.0.1", new DateTimeOffset(2026, 9, 11, 22, 47, 0, TimeSpan.FromHours(3)), "Moved from the mixed Metrics panel to the PPFD panel; ports and calculation are unchanged.");
    private static readonly ComponentRevision DliHourlyRevision = new("1.0.1", new DateTimeOffset(2026, 9, 11, 21, 15, 0, TimeSpan.FromHours(3)), "Reads and validates the selected 24-hour cache block once, eliminating repeated whole-cache validation for every sensor while preserving the legacy branches and values.");
    private static readonly ComponentRevision DliEachSensorRevision = new("1.0.1", new DateTimeOffset(2026, 9, 11, 20, 58, 0, TimeSpan.FromHours(3)), "Fixes the native Grasshopper tree read to request IGH_Goo, the exact type supplied by the Generic sensor-points parameter.");
    private static readonly ComponentRevision IesConversionRevision = new("1.1.0", new DateTimeOffset(2026, 9, 11, 17, 40, 0, TimeSpan.FromHours(3)), "Requires ies2rad outputs that match the requested luminaire name; never substitutes the newest unrelated file.");
    private static readonly ComponentRevision SpectralRevision = new("1.1.0", new DateTimeOffset(2026, 9, 11, 21, 45, 0, TimeSpan.FromHours(3)), "Adds explicit custom-CSV path inputs/outputs and edge-triggered file pickers; persists path/hash and recalculates when the selected CSV or interval changes.");
    private static readonly ComponentRevision SelectorRevision = new("1.0.2", new DateTimeOffset(2026, 9, 11, 17, 45, 0, TimeSpan.FromHours(3)), "Persists the selected library item and re-emits it without reopening its dialog.");
    private static readonly ComponentRevision MaterialTableRevision = new("1.1.0", new DateTimeOffset(2026, 9, 11, 22, 57, 0, TimeSpan.FromHours(3)), "Restores the legacy material-table workflow: preview bitmap, display name, RGB/VLR or RGB/VLT/VLR summary, Python ordering, Select, and Reload.");
    private static readonly ComponentRevision AnnualReaderRevision = new("1.1.0", new DateTimeOffset(2026, 9, 11, 18, 5, 0, TimeSpan.FromHours(3)), "Requires manifest, validated-result signature, and cache hash provenance before emitting annual illuminance or PPFD.");
    private static readonly ComponentRevision ElectricAnnualRevision = new("1.0.0", new DateTimeOffset(2026, 9, 11, 19, 45, 0, TimeSpan.FromHours(3)), "Runs one full-output electric Radiance calculation in the background, snapshots a validated 8,760-hour dimming schedule, and writes a provenance-owned annual matrix.");
    private static readonly ComponentRevision CombinedAnnualRevision = new("1.0.0", new DateTimeOffset(2026, 9, 11, 20, 45, 0, TimeSpan.FromHours(3)), "Combines matching completed daylight and electric annual runs into a separately manifest-owned, provenance-validated result.");
    private static readonly ComponentRevision PlantLightRevision = new("1.2.0", new DateTimeOffset(2026, 9, 12, 0, 0, 0, TimeSpan.FromHours(3)), "Adds explicit profile/context readers and shared finite-value photon integration; existing ports and defaults retained. Spectral CSV uses CIE weighting and trapezoidal integration.");
    private static readonly IReadOnlyDictionary<Guid, ComponentRevision> Entries = new Dictionary<Guid, ComponentRevision>
    {
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa410")] = new("1.2.1", new DateTimeOffset(2026, 9, 12, 14, 0, 0, TimeSpan.FromHours(3)), "Renamed Custom Spectral Profile; existing GUID and five inputs retained."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa417")] = new("1.0.1", new DateTimeOffset(2026, 9, 12, 15, 0, 0, TimeSpan.FromHours(3)), "Acknowledged research limitations remain in Status without repeated runtime warning; readable wavelength coverage."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa411")] = new("1.3.0", new DateTimeOffset(2026, 9, 12, 15, 0, 0, TimeSpan.FromHours(3)), "Strict Hour Index alignment contract, sensor-order guidance; acknowledged assumptions retained in Status."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa412")] = new("1.3.0", new DateTimeOffset(2026, 9, 12, 15, 0, 0, TimeSpan.FromHours(3)), "Strict Hour Index alignment contract, sensor-order guidance; acknowledged assumptions retained in Status."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa413")] = new("1.3.0", new DateTimeOffset(2026, 9, 12, 15, 0, 0, TimeSpan.FromHours(3)), "Strict Hour Index alignment contract, sensor-order guidance; acknowledged assumptions retained in Status."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa414")] = new("1.3.0", new DateTimeOffset(2026, 9, 12, 15, 0, 0, TimeSpan.FromHours(3)), "Strict Hour Index alignment contract, sensor-order guidance; acknowledged assumptions retained in Status."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa415")] = new("1.3.0", new DateTimeOffset(2026, 9, 12, 15, 0, 0, TimeSpan.FromHours(3)), "Strict Hour Index alignment contract, sensor-order guidance; acknowledged assumptions retained in Status."),
        [new("a9c4973b-acb7-45be-96ee-a6d8a35fa416")] = new("1.3.0", new DateTimeOffset(2026, 9, 12, 15, 0, 0, TimeSpan.FromHours(3)), "Strict Hour Index alignment contract, sensor-order guidance; acknowledged assumptions retained in Status."),
        [new("f32f1cbd-04b5-42ed-9fdf-c194851011b2")] = PlantLightRevision,
        [new("d4f97934-9fd5-4d9c-a6e0-b550d0c9cedf")] = PlantLightRevision,
        [new("a77d7b17-274a-444b-af3d-063144dcb3fa")] = PlantLightRevision,
        [new("5747b67c-4aec-4117-83a2-5e30a7308920")] = AnnualPlotRevision,
        [new("ce9c1e5d-c2ce-4c29-9c7d-277d19d25e42")] = AnnualPlotRevision,
        [new("5f1d58a4-064f-4fc9-b79b-640a380a3e43")] = LightingEnergyNavigationRevision,
        [new("0e5f7114-fbb9-4a77-a3f4-40ccd0c0c258")] = Baseline,
        [new("ca2ce6ef-a0c8-4d98-87a3-2adf2a91ca45")] = AnnualSimulationRevision,
        [new("1a2d08d7-6d3e-459e-a2c2-62636cbbaf24")] = AnnualSimulationProgressRevision,
        [new("31b19b55-7384-4f37-bb1a-f436c3cbaa8b")] = Baseline,
        [new("0f9f53a1-6fe6-4a4f-ab14-ee19f53c4fdd")] = DliTargetNavigationRevision,
        [new("53402415-6620-4ecd-bfa3-593e7a148f29")] = MaterialTableRevision,
        [new("492e14e7-163e-4c2a-a6d8-c44184da664d")] = SelectorRevision,
        [new("e64e15f4-7cee-48b2-a232-2064d3a9e602")] = IesConversionRevision,
        [new("9e076d21-00df-4ea2-870e-caf9748ac3d3")] = AnnualReaderRevision,
        [new("3d38a66d-b381-45f2-ad70-57e6be84a6cc")] = AnnualReaderRevision,
        [new("3bb97076-25e8-4623-84fa-1245717b5a58")] = PlantLightRevision,
        [new("c1296cd8-151c-46e0-a5a0-0d2e8f54d9f6")] = PlantLightRevision,
        [new("2bb0d862-d310-4c90-8836-3760fd9870c5")] = SelectorRevision,
        [new("ac8f8d0f-c1d7-480c-8f37-4fe4c76247aa")] = PlantLightRevision,
        [new("29e2836f-5da0-4e4c-bdac-990365a0471e")] = MaterialTableRevision,
        [new("97544a63-5ca5-4255-bd41-8b8ea8d0a2ef")] = MaterialTableRevision,
        [new("747f6a35-6fbc-4231-a042-75fb7a18f7b4")] = MaterialTableRevision,
        [new("2befd6cd-dde7-4e45-9f0d-2ff0089c065d")] = MaterialTableRevision,
        [new("9eebe812-eeb5-476c-a4b6-ca0822940f1f")] = PlantLightRevision,
        [new("0bc7a4db-8702-4e8d-a5bc-dd648ba3ec6e")] = PlantLightRevision,
        [new("f6f1d5d4-9a1a-4de7-a090-6299c94e0060")] = Baseline,
        [new("272aa83d-9898-460d-8cbd-7f49374153ba")] = Baseline,
        [new("31f97f51-692b-43b1-9b45-47f1d4ef2d48")] = new("1.3.0", new DateTimeOffset(2026, 9, 12, 15, 0, 0, TimeSpan.FromHours(3)), "Hour/Day/Alignment outputs, interval-start correction, independent persisted selection and edge-triggered picker."),
        [new("66090f6d-e92c-4dde-b72f-d85d033ae1f6")] = new("1.3.0", new DateTimeOffset(2026, 9, 12, 15, 0, 0, TimeSpan.FromHours(3)), "Shares corrected Hour Index timing contract; review old hour selections."),
        [new("e0c7494d-bf04-4bd1-a9ed-9184fd2b9b53")] = Baseline,
        [new("37c57f57-1be3-4eaa-aa88-12a20f0172ef")] = Baseline,
        [new("71ce89f2-1439-4730-915f-07436692926c")] = Baseline,
        [new("59892a1c-7e97-46d5-989c-2283c9476ea4")] = Baseline,
        [new("d236c57b-eab1-4329-8e4e-beb2285ba04d")] = WorkingDirectoryRevision,
        [new("9cd39fc4-7c35-4aee-a247-1c8b980c4b17")] = Baseline,
        [new("71c6a045-9308-4a0c-9f72-cab76ceefa5c")] = Baseline,
        [new("30fa20be-c063-4d37-9002-46d73774f697")] = PlantLightRevision,
        [new("36362f09-1294-4d39-8d9e-0185ec44c538")] = PlantLightRevision,
        [new("061e0342-6d6f-4ecb-a207-a0807393de1f")] = PlantLightRevision,
        [new("3bc3011e-2b2f-4c14-9344-dcb3554f3722")] = Baseline
        ,[new("b87a6c40-49df-4aef-9ee4-99d5d806bb2d")] = PlantLightRevision
        ,[new("9f5f9a71-0ec2-4fb7-a481-49625f0871f2")] = PlantLightRevision
    };

    public static ComponentRevision Get(Guid componentGuid) => Entries.TryGetValue(componentGuid, out var revision)
        ? revision : throw new InvalidOperationException($"No revision entry is registered for FlahaGrow component {componentGuid}.");
    public static bool Contains(Guid componentGuid) => Entries.ContainsKey(componentGuid);
    public static int Count => Entries.Count;
}
