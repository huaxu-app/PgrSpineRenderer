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

/// <summary>
///     Stores a bone's current pose.
///     <para>
///         A bone has a local transform which is used to compute its world transform. A bone also has an applied
///         transform, which is a
///         local transform that can be applied to compute the world transform. The local transform and applied transform
///         may differ if a
///         constraint or application code modifies the world transform after it was computed from the local transform.
///     </para>
/// </summary>
public class Bone : IUpdatable
{
    public static bool yDown;

    internal float a, b, worldX;
    internal bool appliedValid;
    internal float ax, ay, arotation, ascaleX, ascaleY, ashearX, ashearY;
    internal float c, d, worldY;
    internal ExposedList<Bone> children = new();

    internal BoneData data;
    internal Bone parent;
    internal Skeleton skeleton;

    internal bool sorted, active;
    internal float x, y, rotation, scaleX, scaleY, shearX, shearY;

    /// <param name="parent">May be null.</param>
    public Bone(BoneData data, Skeleton skeleton, Bone parent)
    {
        if (data == null) throw new ArgumentNullException("data", "data cannot be null.");
        if (skeleton == null) throw new ArgumentNullException("skeleton", "skeleton cannot be null.");
        this.data = data;
        this.skeleton = skeleton;
        this.parent = parent;
        SetToSetupPose();
    }

    public BoneData Data => data;
    public Skeleton Skeleton => skeleton;
    public Bone Parent => parent;
    public ExposedList<Bone> Children => children;

    /// <summary>The local X translation.</summary>
    public float X
    {
        get => x;
        set => x = value;
    }

    /// <summary>The local Y translation.</summary>
    public float Y
    {
        get => y;
        set => y = value;
    }

    /// <summary>The local rotation.</summary>
    public float Rotation
    {
        get => rotation;
        set => rotation = value;
    }

    /// <summary>The local scaleX.</summary>
    public float ScaleX
    {
        get => scaleX;
        set => scaleX = value;
    }

    /// <summary>The local scaleY.</summary>
    public float ScaleY
    {
        get => scaleY;
        set => scaleY = value;
    }

    /// <summary>The local shearX.</summary>
    public float ShearX
    {
        get => shearX;
        set => shearX = value;
    }

    /// <summary>The local shearY.</summary>
    public float ShearY
    {
        get => shearY;
        set => shearY = value;
    }

    /// <summary>The rotation, as calculated by any constraints.</summary>
    public float AppliedRotation
    {
        get => arotation;
        set => arotation = value;
    }

    /// <summary>The applied local x translation.</summary>
    public float AX
    {
        get => ax;
        set => ax = value;
    }

    /// <summary>The applied local y translation.</summary>
    public float AY
    {
        get => ay;
        set => ay = value;
    }

    /// <summary>The applied local scaleX.</summary>
    public float AScaleX
    {
        get => ascaleX;
        set => ascaleX = value;
    }

    /// <summary>The applied local scaleY.</summary>
    public float AScaleY
    {
        get => ascaleY;
        set => ascaleY = value;
    }

    /// <summary>The applied local shearX.</summary>
    public float AShearX
    {
        get => ashearX;
        set => ashearX = value;
    }

    /// <summary>The applied local shearY.</summary>
    public float AShearY
    {
        get => ashearY;
        set => ashearY = value;
    }

    public float A => a;
    public float B => b;
    public float C => c;
    public float D => d;

    public float WorldX => worldX;
    public float WorldY => worldY;
    public float WorldRotationX => MathUtils.Atan2(c, a) * MathUtils.RadDeg;
    public float WorldRotationY => MathUtils.Atan2(d, b) * MathUtils.RadDeg;

    /// <summary>Returns the magnitide (always positive) of the world scale X.</summary>
    public float WorldScaleX => (float)Math.Sqrt(a * a + c * c);

    /// <summary>Returns the magnitide (always positive) of the world scale Y.</summary>
    public float WorldScaleY => (float)Math.Sqrt(b * b + d * d);

    public float WorldToLocalRotationX
    {
        get
        {
            var parent = this.parent;
            if (parent == null) return arotation;
            float pa = parent.a, pb = parent.b, pc = parent.c, pd = parent.d, a = this.a, c = this.c;
            return MathUtils.Atan2(pa * c - pc * a, pd * a - pb * c) * MathUtils.RadDeg;
        }
    }

    public float WorldToLocalRotationY
    {
        get
        {
            var parent = this.parent;
            if (parent == null) return arotation;
            float pa = parent.a, pb = parent.b, pc = parent.c, pd = parent.d, b = this.b, d = this.d;
            return MathUtils.Atan2(pa * d - pc * b, pd * b - pb * d) * MathUtils.RadDeg;
        }
    }

    /// <summary>
    ///     Returns false when the bone has not been computed because <see cref="BoneData.SkinRequired" /> is true and the
    ///     <see cref="Skeleton.Skin">active skin</see> does not <see cref="Skin.Bones">contain</see> this bone.
    /// </summary>
    public bool Active => active;

    /// <summary>
    ///     Same as <see cref="UpdateWorldTransform" />. This method exists for Bone to implement
    ///     <see cref="Spine.IUpdatable" />.
    /// </summary>
    public void Update()
    {
        UpdateWorldTransform(x, y, rotation, scaleX, scaleY, shearX, shearY);
    }

    /// <summary>Computes the world transform using the parent bone and this bone's local transform.</summary>
    public void UpdateWorldTransform()
    {
        UpdateWorldTransform(x, y, rotation, scaleX, scaleY, shearX, shearY);
    }

    /// <summary>Computes the world transform using the parent bone and the specified local transform.</summary>
    public void UpdateWorldTransform(float x, float y, float rotation, float scaleX, float scaleY, float shearX,
        float shearY)
    {
        ax = x;
        ay = y;
        arotation = rotation;
        ascaleX = scaleX;
        ascaleY = scaleY;
        ashearX = shearX;
        ashearY = shearY;
        appliedValid = true;
        var skeleton = this.skeleton;

        var parent = this.parent;
        if (parent == null)
        {
            // Root bone.
            float rotationY = rotation + 90 + shearY, sx = skeleton.ScaleX, sy = skeleton.ScaleY;
            a = MathUtils.CosDeg(rotation + shearX) * scaleX * sx;
            b = MathUtils.CosDeg(rotationY) * scaleY * sx;
            c = MathUtils.SinDeg(rotation + shearX) * scaleX * sy;
            d = MathUtils.SinDeg(rotationY) * scaleY * sy;
            worldX = x * sx + skeleton.x;
            worldY = y * sy + skeleton.y;
            return;
        }

        float pa = parent.a, pb = parent.b, pc = parent.c, pd = parent.d;
        worldX = pa * x + pb * y + parent.worldX;
        worldY = pc * x + pd * y + parent.worldY;

        switch (data.transformMode)
        {
            case TransformMode.Normal:
            {
                var rotationY = rotation + 90 + shearY;
                var la = MathUtils.CosDeg(rotation + shearX) * scaleX;
                var lb = MathUtils.CosDeg(rotationY) * scaleY;
                var lc = MathUtils.SinDeg(rotation + shearX) * scaleX;
                var ld = MathUtils.SinDeg(rotationY) * scaleY;
                a = pa * la + pb * lc;
                b = pa * lb + pb * ld;
                c = pc * la + pd * lc;
                d = pc * lb + pd * ld;
                return;
            }
            case TransformMode.OnlyTranslation:
            {
                var rotationY = rotation + 90 + shearY;
                a = MathUtils.CosDeg(rotation + shearX) * scaleX;
                b = MathUtils.CosDeg(rotationY) * scaleY;
                c = MathUtils.SinDeg(rotation + shearX) * scaleX;
                d = MathUtils.SinDeg(rotationY) * scaleY;
                break;
            }
            case TransformMode.NoRotationOrReflection:
            {
                float s = pa * pa + pc * pc, prx;
                if (s > 0.0001f)
                {
                    s = Math.Abs(pa * pd - pb * pc) / s;
                    pa /= skeleton.ScaleX;
                    pc /= skeleton.ScaleY;
                    pb = pc * s;
                    pd = pa * s;
                    prx = MathUtils.Atan2(pc, pa) * MathUtils.RadDeg;
                }
                else
                {
                    pa = 0;
                    pc = 0;
                    prx = 90 - MathUtils.Atan2(pd, pb) * MathUtils.RadDeg;
                }

                var rx = rotation + shearX - prx;
                var ry = rotation + shearY - prx + 90;
                var la = MathUtils.CosDeg(rx) * scaleX;
                var lb = MathUtils.CosDeg(ry) * scaleY;
                var lc = MathUtils.SinDeg(rx) * scaleX;
                var ld = MathUtils.SinDeg(ry) * scaleY;
                a = pa * la - pb * lc;
                b = pa * lb - pb * ld;
                c = pc * la + pd * lc;
                d = pc * lb + pd * ld;
                break;
            }
            case TransformMode.NoScale:
            case TransformMode.NoScaleOrReflection:
            {
                float cos = MathUtils.CosDeg(rotation), sin = MathUtils.SinDeg(rotation);
                var za = (pa * cos + pb * sin) / skeleton.ScaleX;
                var zc = (pc * cos + pd * sin) / skeleton.ScaleY;
                var s = (float)Math.Sqrt(za * za + zc * zc);
                if (s > 0.00001f) s = 1 / s;
                za *= s;
                zc *= s;
                s = (float)Math.Sqrt(za * za + zc * zc);
                if (data.transformMode == TransformMode.NoScale
                    && pa * pd - pb * pc < 0 != skeleton.ScaleX < 0 != skeleton.ScaleY < 0) s = -s;

                var r = MathUtils.PI / 2 + MathUtils.Atan2(zc, za);
                var zb = MathUtils.Cos(r) * s;
                var zd = MathUtils.Sin(r) * s;
                var la = MathUtils.CosDeg(shearX) * scaleX;
                var lb = MathUtils.CosDeg(90 + shearY) * scaleY;
                var lc = MathUtils.SinDeg(shearX) * scaleX;
                var ld = MathUtils.SinDeg(90 + shearY) * scaleY;
                a = za * la + zb * lc;
                b = za * lb + zb * ld;
                c = zc * la + zd * lc;
                d = zc * lb + zd * ld;
                break;
            }
        }

        a *= skeleton.ScaleX;
        b *= skeleton.ScaleX;
        c *= skeleton.ScaleY;
        d *= skeleton.ScaleY;
    }

    public void SetToSetupPose()
    {
        var data = this.data;
        x = data.x;
        y = data.y;
        rotation = data.rotation;
        scaleX = data.scaleX;
        scaleY = data.scaleY;
        shearX = data.shearX;
        shearY = data.shearY;
    }

    /// <summary>
    ///     Computes the individual applied transform values from the world transform. This can be useful to perform processing
    ///     using
    ///     the applied transform after the world transform has been modified directly (eg, by a constraint)..
    ///     Some information is ambiguous in the world transform, such as -1,-1 scale versus 180 rotation.
    /// </summary>
    internal void UpdateAppliedTransform()
    {
        appliedValid = true;
        var parent = this.parent;
        if (parent == null)
        {
            ax = worldX;
            ay = worldY;
            arotation = MathUtils.Atan2(c, a) * MathUtils.RadDeg;
            ascaleX = (float)Math.Sqrt(a * a + c * c);
            ascaleY = (float)Math.Sqrt(b * b + d * d);
            ashearX = 0;
            ashearY = MathUtils.Atan2(a * b + c * d, a * d - b * c) * MathUtils.RadDeg;
            return;
        }

        float pa = parent.a, pb = parent.b, pc = parent.c, pd = parent.d;
        var pid = 1 / (pa * pd - pb * pc);
        float dx = worldX - parent.worldX, dy = worldY - parent.worldY;
        ax = dx * pd * pid - dy * pb * pid;
        ay = dy * pa * pid - dx * pc * pid;
        var ia = pid * pd;
        var id = pid * pa;
        var ib = pid * pb;
        var ic = pid * pc;
        var ra = ia * a - ib * c;
        var rb = ia * b - ib * d;
        var rc = id * c - ic * a;
        var rd = id * d - ic * b;
        ashearX = 0;
        ascaleX = (float)Math.Sqrt(ra * ra + rc * rc);
        if (ascaleX > 0.0001f)
        {
            var det = ra * rd - rb * rc;
            ascaleY = det / ascaleX;
            ashearY = MathUtils.Atan2(ra * rb + rc * rd, det) * MathUtils.RadDeg;
            arotation = MathUtils.Atan2(rc, ra) * MathUtils.RadDeg;
        }
        else
        {
            ascaleX = 0;
            ascaleY = (float)Math.Sqrt(rb * rb + rd * rd);
            ashearY = 0;
            arotation = 90 - MathUtils.Atan2(rd, rb) * MathUtils.RadDeg;
        }
    }

    public void WorldToLocal(float worldX, float worldY, out float localX, out float localY)
    {
        float a = this.a, b = this.b, c = this.c, d = this.d;
        var invDet = 1 / (a * d - b * c);
        float x = worldX - this.worldX, y = worldY - this.worldY;
        localX = x * d * invDet - y * b * invDet;
        localY = y * a * invDet - x * c * invDet;
    }

    public void LocalToWorld(float localX, float localY, out float worldX, out float worldY)
    {
        worldX = localX * a + localY * b + this.worldX;
        worldY = localX * c + localY * d + this.worldY;
    }

    public float WorldToLocalRotation(float worldRotation)
    {
        float sin = MathUtils.SinDeg(worldRotation), cos = MathUtils.CosDeg(worldRotation);
        return MathUtils.Atan2(a * sin - c * cos, d * cos - b * sin) * MathUtils.RadDeg + rotation - shearX;
    }

    public float LocalToWorldRotation(float localRotation)
    {
        localRotation -= rotation - shearX;
        float sin = MathUtils.SinDeg(localRotation), cos = MathUtils.CosDeg(localRotation);
        return MathUtils.Atan2(cos * c + sin * d, cos * a + sin * b) * MathUtils.RadDeg;
    }

    /// <summary>
    ///     Rotates the world transform the specified amount and sets isAppliedValid to false.
    /// </summary>
    /// <param name="degrees">Degrees.</param>
    public void RotateWorld(float degrees)
    {
        float a = this.a, b = this.b, c = this.c, d = this.d;
        float cos = MathUtils.CosDeg(degrees), sin = MathUtils.SinDeg(degrees);
        this.a = cos * a - sin * c;
        this.b = cos * b - sin * d;
        this.c = sin * a + cos * c;
        this.d = sin * b + cos * d;
        appliedValid = false;
    }

    public override string ToString()
    {
        return data.name;
    }
}