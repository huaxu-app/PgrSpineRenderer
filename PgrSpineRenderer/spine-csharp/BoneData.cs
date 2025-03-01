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

public class BoneData
{
    internal int index;
    internal float length;
    internal string name;
    internal BoneData parent;
    internal bool skinRequired;
    internal TransformMode transformMode = TransformMode.Normal;
    internal float x, y, rotation, scaleX = 1, scaleY = 1, shearX, shearY;

    /// <param name="parent">May be null.</param>
    public BoneData(int index, string name, BoneData parent)
    {
        if (index < 0) throw new ArgumentException("index must be >= 0", "index");
        if (name == null) throw new ArgumentNullException("name", "name cannot be null.");
        this.index = index;
        this.name = name;
        this.parent = parent;
    }

    /// <summary>The index of the bone in Skeleton.Bones</summary>
    public int Index => index;

    /// <summary>The name of the bone, which is unique across all bones in the skeleton.</summary>
    public string Name => name;

    /// <summary>May be null.</summary>
    public BoneData Parent => parent;

    public float Length
    {
        get => length;
        set => length = value;
    }

    /// <summary>Local X translation.</summary>
    public float X
    {
        get => x;
        set => x = value;
    }

    /// <summary>Local Y translation.</summary>
    public float Y
    {
        get => y;
        set => y = value;
    }

    /// <summary>Local rotation.</summary>
    public float Rotation
    {
        get => rotation;
        set => rotation = value;
    }

    /// <summary>Local scaleX.</summary>
    public float ScaleX
    {
        get => scaleX;
        set => scaleX = value;
    }

    /// <summary>Local scaleY.</summary>
    public float ScaleY
    {
        get => scaleY;
        set => scaleY = value;
    }

    /// <summary>Local shearX.</summary>
    public float ShearX
    {
        get => shearX;
        set => shearX = value;
    }

    /// <summary>Local shearY.</summary>
    public float ShearY
    {
        get => shearY;
        set => shearY = value;
    }

    /// <summary>The transform mode for how parent world transforms affect this bone.</summary>
    public TransformMode TransformMode
    {
        get => transformMode;
        set => transformMode = value;
    }

    /// <summary>
    ///     When true, <see cref="Skeleton.UpdateWorldTransform()" /> only updates this bone if the
    ///     <see cref="Skeleton.Skin" /> contains this
    ///     bone.
    /// </summary>
    /// <seealso cref="Skin.Bones" />
    public bool SkinRequired
    {
        get => skinRequired;
        set => skinRequired = value;
    }

    public override string ToString()
    {
        return name;
    }
}

[Flags]
public enum TransformMode
{
    //0000 0 Flip Scale Rotation
    Normal = 0, // 0000
    OnlyTranslation = 7, // 0111
    NoRotationOrReflection = 1, // 0001
    NoScale = 2, // 0010
    NoScaleOrReflection = 6 // 0110
}