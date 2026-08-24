using Spine;

namespace PgrSpineRenderer;

/// <summary>
///     From 4.7 onwards PGR stopped shipping separate body/mouth skeletons and packs all the parts into a
///     single skeleton instead, namespacing its animations "By/" (body), "Fa/" (face) and "Mo/" (mouth).
///     The three are meant to play together on separate tracks. Played on their own they're nonsense:
///     "Fa/weixiao" only keys a handful of eye slots, so the whole body falls back to the setup pose.
///     <para>
///         This flattens such a skeleton down to plain animation names ("By/weixiao" -> "weixiao") and maps
///         a name back onto the animations to play per track. Skeletons that don't use the scheme pass
///         straight through, including ones that merely put animations in folders (uinewbietasksailika).
///     </para>
/// </summary>
public static class LayeredAnimations
{
    // The body is what decides the name and length of the animation.
    private const string BodyPrefix = "By/";

    // Track order: body first, face and mouth layer on top of it.
    private static readonly string[] Prefixes = ["By/", "Fa/", "Mo/"];

    /// <summary>
    ///     The animations a skeleton offers, with the duration of their body track.
    /// </summary>
    public static IEnumerable<(string Name, float Duration)> Flatten(SkeletonData data)
    {
        var animations = data.Animations.Select(a => (a.Name, a.Duration));
        if (!IsLayered(data)) return animations;

        return animations
            .Where(a => a.Name.StartsWith(BodyPrefix))
            .Select(a => (a.Name[BodyPrefix.Length..], a.Duration));
    }

    /// <summary>
    ///     The animations to play for a flattened name, in track order. Track 0 is always the one that
    ///     carries the duration; missing face/mouth parts are simply left out.
    /// </summary>
    public static List<Animation> Tracks(SkeletonData data, string name)
    {
        if (!IsLayered(data)) return [data.Animations.Find(a => a.Name == name)];

        return
        [
            .. Prefixes
                .Select(prefix => data.Animations.Find(a => a.Name == prefix + name))
                .OfType<Animation>()
        ];
    }

    /// <summary>
    ///     A skeleton only counts as layered when every single animation on it is namespaced, so a rig that
    ///     just happens to have a folder or two keeps being treated as a plain one.
    /// </summary>
    private static bool IsLayered(SkeletonData data)
    {
        return data.Animations.Count > 0 &&
               data.Animations.All(a => Prefixes.Any(prefix => a.Name.StartsWith(prefix)));
    }
}