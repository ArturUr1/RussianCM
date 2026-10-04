using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Guidebook.Controls;
using Content.Shared._RMC14.Prototypes;
using Content.Shared.Guidebook;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._RuCM.Guidebook;

/// <summary>Adds the RuCM charter to the public guide window without editing its upstream root list.</summary>
public sealed partial class GOVFORGuidebookSystem : EntitySystem
{
    public const string DrillRegulations = "RuCMGOVFORDrillRegulations";

    [Dependency] private IUserInterfaceManager _ui = default!;
    private readonly Dictionary<GuidebookWindow, Action> _windows = new();

    public override void Initialize()
    {
        base.Initialize();
        _ui.WindowRoot.OnChildAdded += OnWindowAdded;
        _ui.WindowRoot.OnChildRemoved += OnWindowRemoved;
        foreach (var window in _ui.WindowRoot.Children.ToArray())
            OnWindowAdded(window);
    }

    public override void Shutdown()
    {
        _ui.WindowRoot.OnChildAdded -= OnWindowAdded;
        _ui.WindowRoot.OnChildRemoved -= OnWindowRemoved;
        foreach (var (window, handler) in _windows)
            window.OnOpen -= handler;
        _windows.Clear();
        base.Shutdown();
    }

    private void OnWindowAdded(Control control)
    {
        if (control is not GuidebookWindow window || _windows.ContainsKey(window))
            return;

        Action handler = () => AddDrillRegulations(window);
        _windows.Add(window, handler);
        window.OnOpen += handler;
    }

    private void OnWindowRemoved(Control control)
    {
        if (control is GuidebookWindow window && _windows.Remove(window, out var handler))
            window.OnOpen -= handler;
    }

    private void AddDrillRegulations(GuidebookWindow window)
    {
        // Read only the public table of contents, preserving restricted/book-specific views.
        var entries = new Dictionary<ProtoId<GuideEntryPrototype>, GuideEntry>();
        foreach (var item in window.Tree.Items)
        {
            if (item.Metadata is GuideEntry entry)
                entries.TryAdd(entry.Id, entry);
        }

        if (entries.ContainsKey(DrillRegulations))
            return;

        var roots = new HashSet<ProtoId<GuideEntryPrototype>>(entries.Keys);
        foreach (var entry in entries.Values)
            roots.ExceptWith(entry.Children);

        // The ordinary CMU guidebook has both these roots. Single books and rules stay scoped.
        if (!roots.Contains("AU14SOP") || !roots.Contains("AU14UCMJ") ||
            !ProtoMan.TryIndex<GuideEntryPrototype>(DrillRegulations, out var drill))
            return;

        // Retain CM guide entries that are only reached through text links, not the visible tree.
        foreach (var entry in ProtoMan.EnumerateCM<GuideEntryPrototype>())
            entries.TryAdd(entry.Id, entry);

        // UpdateGuides compares entry values before rebuilding roots; give our new root its own entry.
        entries[DrillRegulations] = new GuideEntry
        {
            Id = drill.Id,
            Name = drill.Name,
            Text = drill.Text,
            Priority = drill.Priority,
            Children = new(drill.Children),
            FilterEnabled = drill.FilterEnabled,
            RuleEntry = drill.RuleEntry,
        };
        roots.Add(DrillRegulations);
        window.UpdateGuides(entries, rootEntries: roots.ToList(), selected: window.Selected);
        window.Tree.SetAllExpanded(false);
        window.Tree.SetAllExpanded(true, 1);
    }
}
