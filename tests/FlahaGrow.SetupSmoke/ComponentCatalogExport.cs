using System.Collections;
using System.Reflection;
using System.Text.Json;
using FlahaGrow.Grasshopper;
using FlahaGrow.Grasshopper.Components;
using Grasshopper.Kernel;

internal static class ComponentCatalogExport
{
    internal static void Print(string? panel = null)
    {
        object Port(IGH_Param p, int index)
        {
            var defaults = new List<string>();
            var persistent = p.GetType().GetProperty("PersistentData")?.GetValue(p);
            if (persistent is not null)
            {
                var all = persistent.GetType().GetMethod("AllData", new[] { typeof(bool) })?.Invoke(persistent, new object[] { true }) as IEnumerable;
                if (all is not null) foreach (var item in all) defaults.Add(item?.ToString() ?? "null");
            }
            return new { Index = index, p.Name, p.NickName, p.Description, ParameterType = p.GetType().Name,
                Access = p.Access.ToString(), p.Optional, Defaults = defaults };
        }
        var components = typeof(FlahaGrowAssemblyInfo).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(GH_Component).IsAssignableFrom(t))
            .Select(t => (FlahaGrowComponent)Activator.CreateInstance(t, t.GetConstructors().Single().GetParameters().Select(p => p.DefaultValue).ToArray())!)
            .Where(c => panel is null || c.SubCategory.StartsWith(panel, StringComparison.Ordinal))
            .OrderBy(c => c.SubCategory).ThenBy(c => c.Name).ThenBy(c => c.ComponentGuid)
            .Select(c => new { Type = c.GetType().Name, c.Name, c.NickName, c.Description, c.Category, c.SubCategory,
                Guid = c.ComponentGuid, Exposure = c.Exposure.ToString(), Icon = ComponentIcons.ForComponent(c.Name) is not null,
                c.Revision, Inputs = c.Params.Input.Select(Port), Outputs = c.Params.Output.Select(Port) });
        Console.WriteLine(JsonSerializer.Serialize(components, new JsonSerializerOptions { WriteIndented = true }));
    }
}
