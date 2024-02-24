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

using Spine.Collections;

namespace Spine;

/// <summary>
///     Stores attachments by slot index and attachment name.
///     <para>
///         See SkeletonData <see cref="Spine.SkeletonData.DefaultSkin" />, Skeleton <see cref="Spine.Skeleton.Skin" />,
///         and
///         <a href="http://esotericsoftware.com/spine-runtime-skins">Runtime skins</a> in the Spine Runtimes Guide.
///     </para>
/// </summary>
public class Skin
{
    internal readonly ExposedList<BoneData> bones = new();
    internal readonly ExposedList<ConstraintData> constraints = new();
    internal string name;

    public Skin(string name)
    {
        if (name == null) throw new ArgumentNullException("name", "name cannot be null.");
        this.name = name;
    }

    public string Name => name;
    public OrderedDictionary<SkinEntry, Attachment> Attachments { get; } = new(SkinEntryComparer.Instance);

    public ExposedList<BoneData> Bones => bones;
    public ExposedList<ConstraintData> Constraints => constraints;

    /// <summary>
    ///     Adds an attachment to the skin for the specified slot index and name.
    ///     If the name already exists for the slot, the previous value is replaced.
    /// </summary>
    public void SetAttachment(int slotIndex, string name, Attachment attachment)
    {
        if (attachment == null) throw new ArgumentNullException("attachment", "attachment cannot be null.");
        if (slotIndex < 0) throw new ArgumentNullException("slotIndex", "slotIndex must be >= 0.");
        Attachments[new SkinEntry(slotIndex, name, attachment)] = attachment;
    }

    ///<summary>Adds all attachments, bones, and constraints from the specified skin to this skin.</summary>
    public void AddSkin(Skin skin)
    {
        foreach (var data in skin.bones)
            if (!bones.Contains(data))
                bones.Add(data);

        foreach (var data in skin.constraints)
            if (!constraints.Contains(data))
                constraints.Add(data);

        foreach (var entry in skin.Attachments.Keys)
            SetAttachment(entry.SlotIndex, entry.Name, entry.Attachment);
    }

    ///<summary>Adds all attachments from the specified skin to this skin. Attachments are deep copied.</summary>
    public void CopySkin(Skin skin)
    {
        foreach (var data in skin.bones)
            if (!bones.Contains(data))
                bones.Add(data);

        foreach (var data in skin.constraints)
            if (!constraints.Contains(data))
                constraints.Add(data);

        foreach (var entry in skin.Attachments.Keys)
            if (entry.Attachment is MeshAttachment)
                SetAttachment(entry.SlotIndex, entry.Name,
                    entry.Attachment != null ? ((MeshAttachment)entry.Attachment).NewLinkedMesh() : null);
            else
                SetAttachment(entry.SlotIndex, entry.Name, entry.Attachment != null ? entry.Attachment.Copy() : null);
    }

    /// <summary>Returns the attachment for the specified slot index and name, or null.</summary>
    /// <returns>May be null.</returns>
    public Attachment GetAttachment(int slotIndex, string name)
    {
        var lookup = new SkinEntry(slotIndex, name, null);
        Attachment attachment = null;
        var containsKey = Attachments.TryGetValue(lookup, out attachment);
        return containsKey ? attachment : null;
    }

    /// <summary> Removes the attachment in the skin for the specified slot index and name, if any.</summary>
    public void RemoveAttachment(int slotIndex, string name)
    {
        if (slotIndex < 0) throw new ArgumentOutOfRangeException("slotIndex", "slotIndex must be >= 0");
        var lookup = new SkinEntry(slotIndex, name, null);
        Attachments.Remove(lookup);
    }

    ///<summary>Returns all attachments contained in this skin.</summary>
    public ICollection<SkinEntry> GetAttachments()
    {
        return Attachments.Keys;
    }

    /// <summary>Returns all attachments in this skin for the specified slot index.</summary>
    /// <param name="slotIndex">
    ///     The target slotIndex. To find the slot index, use <see cref="Spine.Skeleton.FindSlotIndex" />
    ///     or <see cref="Spine.SkeletonData.FindSlotIndex" />
    public void GetAttachments(int slotIndex, List<SkinEntry> attachments)
    {
        foreach (var entry in Attachments.Keys)
            if (entry.SlotIndex == slotIndex)
                attachments.Add(entry);
    }

    ///<summary>Clears all attachments, bones, and constraints.</summary>
    public void Clear()
    {
        Attachments.Clear();
        bones.Clear();
        constraints.Clear();
    }

    public override string ToString()
    {
        return name;
    }

    /// <summary>Attach all attachments from this skin if the corresponding attachment from the old skin is currently attached.</summary>
    internal void AttachAll(Skeleton skeleton, Skin oldSkin)
    {
        foreach (var entry in oldSkin.Attachments.Keys)
        {
            var slotIndex = entry.SlotIndex;
            var slot = skeleton.slots.Items[slotIndex];
            if (slot.Attachment == entry.Attachment)
            {
                var attachment = GetAttachment(slotIndex, entry.Name);
                if (attachment != null) slot.Attachment = attachment;
            }
        }
    }

    /// <summary>Stores an entry in the skin consisting of the slot index, name, and attachment.</summary>
    public struct SkinEntry
    {
        internal readonly int hashCode;

        public SkinEntry(int slotIndex, string name, Attachment attachment)
        {
            SlotIndex = slotIndex;
            Name = name;
            Attachment = attachment;
            hashCode = Name.GetHashCode() + SlotIndex * 37;
        }

        public int SlotIndex { get; }

        /// <summary>The name the attachment is associated with, equivalent to the skin placeholder name in the Spine editor.</summary>
        public string Name { get; }

        public Attachment Attachment { get; }
    }

    // Avoids boxing in the dictionary and is necessary to omit entry.attachment in the comparison.
    private class SkinEntryComparer : IEqualityComparer<SkinEntry>
    {
        internal static readonly SkinEntryComparer Instance = new();

        bool IEqualityComparer<SkinEntry>.Equals(SkinEntry e1, SkinEntry e2)
        {
            if (e1.SlotIndex != e2.SlotIndex) return false;
            if (!string.Equals(e1.Name, e2.Name, StringComparison.Ordinal)) return false;
            return true;
        }

        int IEqualityComparer<SkinEntry>.GetHashCode(SkinEntry e)
        {
            return e.Name.GetHashCode() + e.SlotIndex * 37;
        }
    }
}