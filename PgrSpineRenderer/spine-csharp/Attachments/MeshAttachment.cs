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

/// <summary>Attachment that displays a texture region using a mesh.</summary>
public class MeshAttachment : VertexAttachment, IHasRendererObject
{
    internal int hulllength;
    private MeshAttachment parentMesh;
    internal float r = 1, g = 1, b = 1, a = 1;
    internal float regionOffsetX, regionOffsetY, regionWidth, regionHeight, regionOriginalWidth, regionOriginalHeight;
    internal int[] triangles;
    internal float[] uvs, regionUVs;

    public MeshAttachment(string name)
        : base(name)
    {
    }

    public int HullLength
    {
        get => hulllength;
        set => hulllength = value;
    }

    public float[] RegionUVs
    {
        get => regionUVs;
        set => regionUVs = value;
    }

    /// <summary>
    ///     The UV pair for each vertex, normalized within the entire texture. <seealso cref="MeshAttachment.UpdateUVs" />
    /// </summary>
    public float[] UVs
    {
        get => uvs;
        set => uvs = value;
    }

    public int[] Triangles
    {
        get => triangles;
        set => triangles = value;
    }

    public float R
    {
        get => r;
        set => r = value;
    }

    public float G
    {
        get => g;
        set => g = value;
    }

    public float B
    {
        get => b;
        set => b = value;
    }

    public float A
    {
        get => a;
        set => a = value;
    }

    public string Path { get; set; }
    public float RegionU { get; set; }
    public float RegionV { get; set; }
    public float RegionU2 { get; set; }
    public float RegionV2 { get; set; }
    public bool RegionRotate { get; set; }
    public int RegionDegrees { get; set; }

    public float RegionOffsetX
    {
        get => regionOffsetX;
        set => regionOffsetX = value;
    }

    public float RegionOffsetY
    {
        get => regionOffsetY;
        set => regionOffsetY = value;
    } // Pixels stripped from the bottom left, unrotated.

    public float RegionWidth
    {
        get => regionWidth;
        set => regionWidth = value;
    }

    public float RegionHeight
    {
        get => regionHeight;
        set => regionHeight = value;
    } // Unrotated, stripped size.

    public float RegionOriginalWidth
    {
        get => regionOriginalWidth;
        set => regionOriginalWidth = value;
    }

    public float RegionOriginalHeight
    {
        get => regionOriginalHeight;
        set => regionOriginalHeight = value;
    } // Unrotated, unstripped size.

    public MeshAttachment ParentMesh
    {
        get => parentMesh;
        set
        {
            parentMesh = value;
            if (value != null)
            {
                bones = value.bones;
                vertices = value.vertices;
                worldVerticesLength = value.worldVerticesLength;
                regionUVs = value.regionUVs;
                triangles = value.triangles;
                HullLength = value.HullLength;
                Edges = value.Edges;
                Width = value.Width;
                Height = value.Height;
            }
        }
    }

    // Nonessential.
    public int[] Edges { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public object RendererObject { get; set; }

    public void UpdateUVs()
    {
        var regionUVs = this.regionUVs;
        if (this.uvs == null || this.uvs.Length != regionUVs.Length) this.uvs = new float[regionUVs.Length];
        var uvs = this.uvs;
        float u = RegionU, v = RegionV, width = 0, height = 0;

        if (RegionDegrees == 90)
        {
            var textureHeight = regionWidth / (RegionV2 - RegionV);
            var textureWidth = regionHeight / (RegionU2 - RegionU);
            u -= (RegionOriginalHeight - RegionOffsetY - RegionHeight) / textureWidth;
            v -= (RegionOriginalWidth - RegionOffsetX - RegionWidth) / textureHeight;
            width = RegionOriginalHeight / textureWidth;
            height = RegionOriginalWidth / textureHeight;

            for (int i = 0, n = uvs.Length; i < n; i += 2)
            {
                uvs[i] = u + regionUVs[i + 1] * width;
                uvs[i + 1] = v + (1 - regionUVs[i]) * height;
            }
        }
        else if (RegionDegrees == 180)
        {
            var textureWidth = regionWidth / (RegionU2 - RegionU);
            var textureHeight = regionHeight / (RegionV2 - RegionV);
            u -= (RegionOriginalWidth - RegionOffsetX - RegionWidth) / textureWidth;
            v -= RegionOffsetY / textureHeight;
            width = RegionOriginalWidth / textureWidth;
            height = RegionOriginalHeight / textureHeight;

            for (int i = 0, n = uvs.Length; i < n; i += 2)
            {
                uvs[i] = u + (1 - regionUVs[i]) * width;
                uvs[i + 1] = v + (1 - regionUVs[i + 1]) * height;
            }
        }
        else if (RegionDegrees == 270)
        {
            var textureWidth = regionWidth / (RegionU2 - RegionU);
            var textureHeight = regionHeight / (RegionV2 - RegionV);
            u -= RegionOffsetY / textureWidth;
            v -= RegionOffsetX / textureHeight;
            width = RegionOriginalHeight / textureWidth;
            height = RegionOriginalWidth / textureHeight;

            for (int i = 0, n = uvs.Length; i < n; i += 2)
            {
                uvs[i] = u + (1 - regionUVs[i + 1]) * width;
                uvs[i + 1] = v + regionUVs[i] * height;
            }
        }
        else
        {
            var textureWidth = regionWidth / (RegionU2 - RegionU);
            var textureHeight = regionHeight / (RegionV2 - RegionV);
            u -= RegionOffsetX / textureWidth;
            v -= (RegionOriginalHeight - RegionOffsetY - RegionHeight) / textureHeight;
            width = RegionOriginalWidth / textureWidth;
            height = RegionOriginalHeight / textureHeight;

            for (int i = 0, n = uvs.Length; i < n; i += 2)
            {
                uvs[i] = u + regionUVs[i] * width;
                uvs[i + 1] = v + regionUVs[i + 1] * height;
            }
        }
    }

    public override Attachment Copy()
    {
        if (parentMesh != null) return NewLinkedMesh();

        var copy = new MeshAttachment(Name);
        copy.RendererObject = RendererObject;
        copy.regionOffsetX = regionOffsetX;
        copy.regionOffsetY = regionOffsetY;
        copy.regionWidth = regionWidth;
        copy.regionHeight = regionHeight;
        copy.regionOriginalWidth = regionOriginalWidth;
        copy.regionOriginalHeight = regionOriginalHeight;
        copy.RegionRotate = RegionRotate;
        copy.RegionDegrees = RegionDegrees;
        copy.RegionU = RegionU;
        copy.RegionV = RegionV;
        copy.RegionU2 = RegionU2;
        copy.RegionV2 = RegionV2;

        copy.Path = Path;
        copy.r = r;
        copy.g = g;
        copy.b = b;
        copy.a = a;

        CopyTo(copy);
        copy.regionUVs = new float[regionUVs.Length];
        Array.Copy(regionUVs, 0, copy.regionUVs, 0, regionUVs.Length);
        copy.uvs = new float[uvs.Length];
        Array.Copy(uvs, 0, copy.uvs, 0, uvs.Length);
        copy.triangles = new int[triangles.Length];
        Array.Copy(triangles, 0, copy.triangles, 0, triangles.Length);
        copy.HullLength = HullLength;

        // Nonessential.
        if (Edges != null)
        {
            copy.Edges = new int[Edges.Length];
            Array.Copy(Edges, 0, copy.Edges, 0, Edges.Length);
        }

        copy.Width = Width;
        copy.Height = Height;
        return copy;
    }

    /// <summary>Returns a new mesh with this mesh set as the <see cref="ParentMesh" />.
    public MeshAttachment NewLinkedMesh()
    {
        var mesh = new MeshAttachment(Name);
        mesh.RendererObject = RendererObject;
        mesh.regionOffsetX = regionOffsetX;
        mesh.regionOffsetY = regionOffsetY;
        mesh.regionWidth = regionWidth;
        mesh.regionHeight = regionHeight;
        mesh.regionOriginalWidth = regionOriginalWidth;
        mesh.regionOriginalHeight = regionOriginalHeight;
        mesh.RegionDegrees = RegionDegrees;
        mesh.RegionRotate = RegionRotate;
        mesh.RegionU = RegionU;
        mesh.RegionV = RegionV;
        mesh.RegionU2 = RegionU2;
        mesh.RegionV2 = RegionV2;

        mesh.Path = Path;
        mesh.r = r;
        mesh.g = g;
        mesh.b = b;
        mesh.a = a;

        mesh.deformAttachment = deformAttachment;
        mesh.ParentMesh = parentMesh != null ? parentMesh : this;
        mesh.UpdateUVs();
        return mesh;
    }
}