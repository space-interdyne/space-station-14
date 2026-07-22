using Content.Server.Power.Components;
using Content.Shared.Power;

namespace Content.Server.Power.EntitySystems;

internal sealed partial class PowerMonitoringConsoleSystem
{
    /// <summary>
    ///     Collects LV power consumers linked to an APC (and its collection siblings),
    ///     aggregates identical prototypes, and sorts them by demand descending.
    /// </summary>
    private void GetApcConsumers(EntityUid uid, PowerMonitoringDeviceComponent device, List<PowerMonitoringConsoleEntry> consumers)
    {
        var apcs = new List<EntityUid> { uid };

        if (device.IsCollectionMaster)
        {
            foreach (var child in device.ChildDevices.Keys)
                apcs.Add(child);
        }

        // Keyed by prototype id. Aggregates identical machines
        // so focusing an APC with ten miners shows one "Miner [10]" row, not spam.
        var aggregates = new Dictionary<string, ConsumerAggregate>();

        foreach (var apc in apcs)
        {
            if (!TryComp<ApcPowerProviderComponent>(apc, out var provider))
                continue;

            foreach (var receiver in provider.LinkedReceivers)
            {
                if (receiver.PowerDisabled)
                    continue;

                var load = receiver.Load;
                if (load <= 0f)
                    continue;

                var ent = receiver.Owner;
                var meta = MetaData(ent);
                var key = meta.EntityPrototype?.ID ?? meta.EntityName;
                var name = meta.EntityName;

                if (aggregates.TryGetValue(key, out var existing))
                {
                    // Keep the highest-draw entity as the map representative.
                    if (load > existing.RepresentativeLoad)
                    {
                        existing.Representative = ent;
                        existing.RepresentativeLoad = load;
                    }

                    existing.TotalLoad += load;
                    existing.Count++;
                    aggregates[key] = existing;
                    continue;
                }

                aggregates[key] = new ConsumerAggregate
                {
                    Representative = ent,
                    RepresentativeLoad = load,
                    TotalLoad = load,
                    Count = 1,
                    Name = name,
                };
            }
        }

        if (aggregates.Count == 0)
            return;

        foreach (var aggregate in aggregates.Values)
        {
            var displayName = aggregate.Count > 1
                ? Loc.GetString("power-monitoring-window-consumer-array", ("name", aggregate.Name), ("count", aggregate.Count))
                : aggregate.Name;

            var xform = Transform(aggregate.Representative);
            var metaData = new PowerMonitoringDeviceMetaData(
                displayName,
                GetNetCoordinates(xform.Coordinates),
                PowerMonitoringConsoleGroup.Consumer,
                string.Empty,
                string.Empty)
            {
                Prototype = MetaData(aggregate.Representative).EntityPrototype?.ID,
            };

            consumers.Add(new PowerMonitoringConsoleEntry(
                GetNetEntity(aggregate.Representative),
                PowerMonitoringConsoleGroup.Consumer,
                aggregate.TotalLoad)
            {
                MetaData = metaData,
            });
        }

        consumers.Sort((a, b) => b.PowerValue.CompareTo(a.PowerValue));
    }

    private struct ConsumerAggregate
    {
        public EntityUid Representative;
        public float RepresentativeLoad;
        public double TotalLoad;
        public int Count;
        public string Name;
    }
}
