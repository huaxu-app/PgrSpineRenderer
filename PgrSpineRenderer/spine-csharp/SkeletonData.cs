#pragma warning disable
/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated January 1, 2020. Replaces all prior versions.
 *
 * Copyright (c) 2013-2020, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software
 * or otherwise create derivative works of the Spine Runtimes (collectively,
 * "Products"), provided that each user of the Products must obtain their own
 * Spine Editor license and redistribution of the Products in any form must
 * include this license and copyright notice.
 *
 * THE SPINE RUNTIMES ARE PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES,
 * BUSINESS INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THE SPINE RUNTIMES, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

namespace Spine;

/// <summary>Stores the setup pose and all of the stateless data for a skeleton.</summary>
public class SkeletonData
{
    internal ExposedList<Animation> animations = new();
    internal ExposedList<BoneData> bones = new(); // Ordered parents first
    internal Skin defaultSkin;
    internal ExposedList<EventData> events = new();

    // Nonessential.
    internal float fps;
    internal ExposedList<IkConstraintData> ikConstraints = new();
    internal string imagesPath, audioPath;
    internal string name;
    internal ExposedList<PathConstraintData> pathConstraints = new();
    internal ExposedList<Skin> skins = new();
    internal ExposedList<SlotData> slots = new(); // Setup pose draw order.
    internal ExposedList<TransformConstraintData> transformConstraints = new();
    internal string version, hash;
    internal float x, y, width, height;

    public string Name
    {
        get => name;
        set => name = value;
    }

    /// <summary>The skeleton's bones, sorted parent first. The root bone is always the first bone.</summary>
    public ExposedList<BoneData> Bones => bones;

    public ExposedList<SlotData> Slots => slots;

    /// <summary>All skins, including the default skin.</summary>
    public ExposedList<Skin> Skins
    {
        get => skins;
        set => skins = value;
    }

    /// <summary>
    ///     The skeleton's default skin.
    ///     By default this skin contains all attachments that were not in a skin in Spine.
    /// </summary>
    /// <return>May be null.</return>
    public Skin DefaultSkin
    {
        get => defaultSkin;
        set => defaultSkin = value;
    }

    public ExposedList<EventData> Events
    {
        get => events;
        set => events = value;
    }

    public ExposedList<Animation> Animations
    {
        get => animations;
        set => animations = value;
    }

    public ExposedList<IkConstraintData> IkConstraints
    {
        get => ikConstraints;
        set => ikConstraints = value;
    }

    public ExposedList<TransformConstraintData> TransformConstraints
    {
        get => transformConstraints;
        set => transformConstraints = value;
    }

    public ExposedList<PathConstraintData> PathConstraints
    {
        get => pathConstraints;
        set => pathConstraints = value;
    }

    public float X
    {
        get => x;
        set => x = value;
    }

    public float Y
    {
        get => y;
        set => y = value;
    }

    public float Width
    {
        get => width;
        set => width = value;
    }

    public float Height
    {
        get => height;
        set => height = value;
    }

    /// <summary>The Spine version used to export this data, or null.</summary>
    public string Version
    {
        get => version;
        set => version = value;
    }

    public string Hash
    {
        get => hash;
        set => hash = value;
    }

    /// <summary>
    ///     The path to the images directory as defined in Spine. Available only when nonessential data was exported. May
    ///     be null
    /// </summary>
    public string ImagesPath
    {
        get => imagesPath;
        set => imagesPath = value;
    }

    /// <summary>
    ///     The path to the audio directory defined in Spine. Available only when nonessential data was exported. May be
    ///     null.
    /// </summary>
    public string AudioPath
    {
        get => audioPath;
        set => audioPath = value;
    }

    /// <summary>
    ///     The dopesheet FPS in Spine. Available only when nonessential data was exported.
    /// </summary>
    public float Fps
    {
        get => fps;
        set => fps = value;
    }

    // --- Bones.

    /// <summary>
    ///     Finds a bone by comparing each bone's name.
    ///     It is more efficient to cache the results of this method than to call it multiple times.
    /// </summary>
    /// <returns>May be null.</returns>
    public BoneData FindBone(string boneName)
    {
        if (boneName == null) throw new ArgumentNullException("boneName", "boneName cannot be null.");
        var bones = this.bones;
        var bonesItems = bones.Items;
        for (int i = 0, n = bones.Count; i < n; i++)
        {
            var bone = bonesItems[i];
            if (bone.name == boneName) return bone;
        }

        return null;
    }

    /// <returns>-1 if the bone was not found.</returns>
    public int FindBoneIndex(string boneName)
    {
        if (boneName == null) throw new ArgumentNullException("boneName", "boneName cannot be null.");
        var bones = this.bones;
        var bonesItems = bones.Items;
        for (int i = 0, n = bones.Count; i < n; i++)
            if (bonesItems[i].name == boneName)
                return i;
        return -1;
    }

    // --- Slots.

    /// <returns>May be null.</returns>
    public SlotData FindSlot(string slotName)
    {
        if (slotName == null) throw new ArgumentNullException("slotName", "slotName cannot be null.");
        var slots = this.slots;
        for (int i = 0, n = slots.Count; i < n; i++)
        {
            var slot = slots.Items[i];
            if (slot.name == slotName) return slot;
        }

        return null;
    }

    /// <returns>-1 if the slot was not found.</returns>
    public int FindSlotIndex(string slotName)
    {
        if (slotName == null) throw new ArgumentNullException("slotName", "slotName cannot be null.");
        var slots = this.slots;
        for (int i = 0, n = slots.Count; i < n; i++)
            if (slots.Items[i].name == slotName)
                return i;
        return -1;
    }

    // --- Skins.

    /// <returns>May be null.</returns>
    public Skin FindSkin(string skinName)
    {
        if (skinName == null) throw new ArgumentNullException("skinName", "skinName cannot be null.");
        foreach (var skin in skins)
            if (skin.name == skinName)
                return skin;
        return null;
    }

    // --- Events.

    /// <returns>May be null.</returns>
    public EventData FindEvent(string eventDataName)
    {
        if (eventDataName == null) throw new ArgumentNullException("eventDataName", "eventDataName cannot be null.");
        foreach (var eventData in events)
            if (eventData.name == eventDataName)
                return eventData;
        return null;
    }

    // --- Animations.

    /// <returns>May be null.</returns>
    public Animation FindAnimation(string animationName)
    {
        if (animationName == null) throw new ArgumentNullException("animationName", "animationName cannot be null.");
        var animations = this.animations;
        for (int i = 0, n = animations.Count; i < n; i++)
        {
            var animation = animations.Items[i];
            if (animation.name == animationName) return animation;
        }

        return null;
    }

    // --- IK constraints.

    /// <returns>May be null.</returns>
    public IkConstraintData FindIkConstraint(string constraintName)
    {
        if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
        var ikConstraints = this.ikConstraints;
        for (int i = 0, n = ikConstraints.Count; i < n; i++)
        {
            var ikConstraint = ikConstraints.Items[i];
            if (ikConstraint.name == constraintName) return ikConstraint;
        }

        return null;
    }

    // --- Transform constraints.

    /// <returns>May be null.</returns>
    public TransformConstraintData FindTransformConstraint(string constraintName)
    {
        if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
        var transformConstraints = this.transformConstraints;
        for (int i = 0, n = transformConstraints.Count; i < n; i++)
        {
            var transformConstraint = transformConstraints.Items[i];
            if (transformConstraint.name == constraintName) return transformConstraint;
        }

        return null;
    }

    // --- Path constraints.

    /// <returns>May be null.</returns>
    public PathConstraintData FindPathConstraint(string constraintName)
    {
        if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
        var pathConstraints = this.pathConstraints;
        for (int i = 0, n = pathConstraints.Count; i < n; i++)
        {
            var constraint = pathConstraints.Items[i];
            if (constraint.name.Equals(constraintName)) return constraint;
        }

        return null;
    }

    /// <returns>-1 if the path constraint was not found.</returns>
    public int FindPathConstraintIndex(string pathConstraintName)
    {
        if (pathConstraintName == null)
            throw new ArgumentNullException("pathConstraintName", "pathConstraintName cannot be null.");
        var pathConstraints = this.pathConstraints;
        for (int i = 0, n = pathConstraints.Count; i < n; i++)
            if (pathConstraints.Items[i].name.Equals(pathConstraintName))
                return i;
        return -1;
    }

    // ---

    public override string ToString()
    {
        return name ?? base.ToString();
    }
}