namespace PgrSpineRenderer;

public class AnimationSet
{
    private readonly List<HashSet<string>> _layers = [];

    // Idle is a bit of a weird case at times
    private const string Idle = "idle";

    public void RegisterLayer(IEnumerable<string> animations)
    {
        // We filter out animations with length 1.
        // These are animations like "A", "B", "X", and are single-frame animations for lip-syncing
        // which is out of scope
        _layers.Add([..animations.Where(x => x.Length > 1)]);
    }

    public List<string> GetRenderSet()
    {
        if (_layers.Count == 0) return [];

        // Find all unique animations
        var allAnimations = _layers.SelectMany(l => l).ToHashSet();
        var allHaveIdle = _layers.All(l => l.Contains(Idle));

        // If all layers are the same, just render everything
        if (IsUniform() || allHaveIdle) return allAnimations.ToList();

        // Return everything except idle
        return allAnimations
            .Where(a => !a.Equals(Idle))
            .ToList();
    }

    /// <summary>
    /// Check if all layers have the same animation set
    /// </summary>
    /// <returns>true if all layers have the same animation set, false otherwise. In the case of no layers, returns true.</returns>
    private bool IsUniform()
    {
        if (_layers.Count == 0) return true;

        var firstLayerNonIdle = _layers[0].Where(a => !a.Equals(Idle)).ToHashSet();
        return _layers.Skip(1)
            .Select(layer => layer.Where(a => !a.Equals(Idle)).ToHashSet())
            .All(currentNonIdle => firstLayerNonIdle.SetEquals(currentNonIdle));
    }

    public string Resolve(IEnumerable<string> layerAnims, string target)
    {
        var available = new HashSet<string>(layerAnims);
        if (available.Contains(target)) return target;
        if (available.Contains(Idle)) return Idle;
        return available.First();
    }
}