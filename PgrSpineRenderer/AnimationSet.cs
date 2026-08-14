namespace PgrSpineRenderer;

public class AnimationSet
{
    // Idle is a bit of a weird case at times
    private const string Idle = "idle";
    private readonly List<HashSet<string>> _layers = [];
    private readonly HashSet<string> _playable = [];

    public void RegisterLayer(IEnumerable<string> animations)
    {
        RegisterLayer(animations.Select(name => (name, 1f)));
    }

    public void RegisterLayer(IEnumerable<(string Name, float Duration)> animations)
    {
        // We filter out animations with length 1.
        // These are animations like "A", "B", "X", and are single-frame animations for lip-syncing
        // which is out of scope
        var layer = animations.Where(a => a.Name.Length > 1).ToList();
        _layers.Add([..layer.Select(a => a.Name)]);

        // Some layers have empty animations so they're static, just move around by others.
        // If all layers are empty, thats a problem
        _playable.UnionWith(layer.Where(a => a.Duration > 0).Select(a => a.Name));
    }

    public List<string> GetRenderSet()
    {
        if (_layers.Count == 0) return [];

        // Find all unique animations
        var allAnimations = _layers.SelectMany(l => l).ToHashSet();
        var allHaveIdle = _layers.All(l => l.Contains(Idle));

        // If all layers are the same, just render everything
        if (IsUniform() || allHaveIdle) return allAnimations.Where(_playable.Contains).ToList();

        // Return everything except idle
        return allAnimations
            .Where(a => !a.Equals(Idle))
            .Where(_playable.Contains)
            .ToList();
    }

    /// <summary>
    ///     Check if all layers have the same animation set
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