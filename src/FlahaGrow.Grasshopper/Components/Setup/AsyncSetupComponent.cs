using System.Text.Json;
using FlahaGrow.Core.Operations;
using FlahaGrow.Grasshopper.Components;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components.Setup;

public abstract class AsyncSetupComponent<T> : FlahaGrowComponent where T : class
{
    private readonly SetupOperation<T> operation = new();
    private string? currentKey;
    private long publishedRevision = -1;
    protected T? Result { get; private set; }
    protected string Status { get; private set; } = "Waiting for inputs.";
    protected AsyncSetupComponent(string name, string nickname, string description, string panel = "00 Setup")
        : base(name, nickname, description, "FlahaGrow", panel) { }

    protected static string Key(object value) => JsonSerializer.Serialize(value);
    protected bool Changed(string key) => key != currentKey;

    protected void Update(string key, bool start, Func<CancellationToken, Task<T>> work, string waiting)
    {
        if (Changed(key))
        {
            operation.Cancel(); currentKey = key; Result = null; Status = waiting;
        }
        if (start)
        {
            Result = null; Status = "Working…";
            var task = operation.Start(work);
            var revision = operation.Revision;
            var document = OnPingDocument();
            if (document is not null) _ = task.ContinueWith(_ => Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
            {
                if (operation.Revision != revision || document is null || OnPingDocument() != document) return;
                document.ScheduleSolution(1, _ =>
                {
                    if (operation.Revision == revision && OnPingDocument() == document) ExpireSolution(false);
                });
            })), TaskScheduler.Default);
        }
        if (operation.Current is { IsCompletedSuccessfully: true } finished && publishedRevision != operation.Revision && Status == "Working…")
        {
            publishedRevision = operation.Revision;
            var outcome = finished.Result;
            Result = outcome.Value;
            Status = outcome.Cancelled ? "Cancelled." : outcome.Error ?? "Ready.";
            if (outcome.Error is not null) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, outcome.Error);
        }
        SetRevisionMessage(Status == "Working…" ? "Working" : Result is null ? "Not ready" : "Ready");
    }

    protected void Invalidate()
    { operation.Cancel(); currentKey = null; Result = null; Status = "Waiting for inputs."; }

    public override void RemovedFromDocument(GH_Document document)
    { Invalidate(); base.RemovedFromDocument(document); }
    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
        if (context is GH_DocumentContext.Close or GH_DocumentContext.Unloaded) Invalidate();
        base.DocumentContextChanged(document, context);
    }
}
