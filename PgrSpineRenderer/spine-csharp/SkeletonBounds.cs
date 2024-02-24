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

/// <summary>
///     Collects each BoundingBoxAttachment that is visible and computes the world vertices for its polygon.
///     The polygon vertices are provided along with convenience methods for doing hit detection.
/// </summary>
public class SkeletonBounds
{
    private readonly ExposedList<Polygon> polygonPool = new();

    public SkeletonBounds()
    {
        BoundingBoxes = new ExposedList<BoundingBoxAttachment>();
        Polygons = new ExposedList<Polygon>();
    }

    public ExposedList<BoundingBoxAttachment> BoundingBoxes { get; }
    public ExposedList<Polygon> Polygons { get; }
    public float MinX { get; set; }

    public float MinY { get; set; }

    public float MaxX { get; set; }

    public float MaxY { get; set; }

    public float Width => MaxX - MinX;
    public float Height => MaxY - MinY;

    /// <summary>
    ///     Clears any previous polygons, finds all visible bounding box attachments,
    ///     and computes the world vertices for each bounding box's polygon.
    /// </summary>
    /// <param name="skeleton">The skeleton.</param>
    /// <param name="updateAabb">
    ///     If true, the axis aligned bounding box containing all the polygons is computed.
    ///     If false, the SkeletonBounds AABB methods will always return true.
    /// </param>
    public void Update(Skeleton skeleton, bool updateAabb)
    {
        var boundingBoxes = BoundingBoxes;
        var polygons = Polygons;
        var slots = skeleton.slots;
        var slotCount = slots.Count;

        boundingBoxes.Clear();
        for (int i = 0, n = polygons.Count; i < n; i++)
            polygonPool.Add(polygons.Items[i]);
        polygons.Clear();

        for (var i = 0; i < slotCount; i++)
        {
            var slot = slots.Items[i];
            if (!slot.bone.active) continue;
            var boundingBox = slot.attachment as BoundingBoxAttachment;
            if (boundingBox == null) continue;
            boundingBoxes.Add(boundingBox);

            Polygon polygon = null;
            var poolCount = polygonPool.Count;
            if (poolCount > 0)
            {
                polygon = polygonPool.Items[poolCount - 1];
                polygonPool.RemoveAt(poolCount - 1);
            }
            else
            {
                polygon = new Polygon();
            }

            polygons.Add(polygon);

            var count = boundingBox.worldVerticesLength;
            polygon.Count = count;
            if (polygon.Vertices.Length < count) polygon.Vertices = new float[count];
            boundingBox.ComputeWorldVertices(slot, polygon.Vertices);
        }

        if (updateAabb)
        {
            AabbCompute();
        }
        else
        {
            MinX = int.MinValue;
            MinY = int.MinValue;
            MaxX = int.MaxValue;
            MaxY = int.MaxValue;
        }
    }

    private void AabbCompute()
    {
        float minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        var polygons = Polygons;
        for (int i = 0, n = polygons.Count; i < n; i++)
        {
            var polygon = polygons.Items[i];
            var vertices = polygon.Vertices;
            for (int ii = 0, nn = polygon.Count; ii < nn; ii += 2)
            {
                var x = vertices[ii];
                var y = vertices[ii + 1];
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }


    /// <summary>Returns true if the axis aligned bounding box contains the point.</summary>
    public bool AabbContainsPoint(float x, float y)
    {
        return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
    }

    /// <summary>Returns true if the axis aligned bounding box intersects the line segment.</summary>
    public bool AabbIntersectsSegment(float x1, float y1, float x2, float y2)
    {
        var minX = MinX;
        var minY = MinY;
        var maxX = MaxX;
        var maxY = MaxY;
        if ((x1 <= minX && x2 <= minX) || (y1 <= minY && y2 <= minY) || (x1 >= maxX && x2 >= maxX) ||
            (y1 >= maxY && y2 >= maxY))
            return false;
        var m = (y2 - y1) / (x2 - x1);
        var y = m * (minX - x1) + y1;
        if (y > minY && y < maxY) return true;
        y = m * (maxX - x1) + y1;
        if (y > minY && y < maxY) return true;
        var x = (minY - y1) / m + x1;
        if (x > minX && x < maxX) return true;
        x = (maxY - y1) / m + x1;
        if (x > minX && x < maxX) return true;
        return false;
    }

    /// <summary>
    ///     Returns true if the axis aligned bounding box intersects the axis aligned bounding box of the specified
    ///     bounds.
    /// </summary>
    public bool AabbIntersectsSkeleton(SkeletonBounds bounds)
    {
        return MinX < bounds.MaxX && MaxX > bounds.MinX && MinY < bounds.MaxY && MaxY > bounds.MinY;
    }

    /// <summary>Returns true if the polygon contains the point.</summary>
    public bool ContainsPoint(Polygon polygon, float x, float y)
    {
        var vertices = polygon.Vertices;
        var nn = polygon.Count;

        var prevIndex = nn - 2;
        var inside = false;
        for (var ii = 0; ii < nn; ii += 2)
        {
            var vertexY = vertices[ii + 1];
            var prevY = vertices[prevIndex + 1];
            if ((vertexY < y && prevY >= y) || (prevY < y && vertexY >= y))
            {
                var vertexX = vertices[ii];
                if (vertexX + (y - vertexY) / (prevY - vertexY) * (vertices[prevIndex] - vertexX) < x) inside = !inside;
            }

            prevIndex = ii;
        }

        return inside;
    }

    /// <summary>
    ///     Returns the first bounding box attachment that contains the point, or null. When doing many checks, it is usually
    ///     more
    ///     efficient to only call this method if {@link #aabbContainsPoint(float, float)} returns true.
    /// </summary>
    public BoundingBoxAttachment ContainsPoint(float x, float y)
    {
        var polygons = Polygons;
        for (int i = 0, n = polygons.Count; i < n; i++)
            if (ContainsPoint(polygons.Items[i], x, y))
                return BoundingBoxes.Items[i];
        return null;
    }

    /// <summary>
    ///     Returns the first bounding box attachment that contains the line segment, or null. When doing many checks, it is
    ///     usually
    ///     more efficient to only call this method if {@link #aabbIntersectsSegment(float, float, float, float)} returns true.
    /// </summary>
    public BoundingBoxAttachment IntersectsSegment(float x1, float y1, float x2, float y2)
    {
        var polygons = Polygons;
        for (int i = 0, n = polygons.Count; i < n; i++)
            if (IntersectsSegment(polygons.Items[i], x1, y1, x2, y2))
                return BoundingBoxes.Items[i];
        return null;
    }

    /// <summary>Returns true if the polygon contains the line segment.</summary>
    public bool IntersectsSegment(Polygon polygon, float x1, float y1, float x2, float y2)
    {
        var vertices = polygon.Vertices;
        var nn = polygon.Count;

        float width12 = x1 - x2, height12 = y1 - y2;
        var det1 = x1 * y2 - y1 * x2;
        float x3 = vertices[nn - 2], y3 = vertices[nn - 1];
        for (var ii = 0; ii < nn; ii += 2)
        {
            float x4 = vertices[ii], y4 = vertices[ii + 1];
            var det2 = x3 * y4 - y3 * x4;
            float width34 = x3 - x4, height34 = y3 - y4;
            var det3 = width12 * height34 - height12 * width34;
            var x = (det1 * width34 - width12 * det2) / det3;
            if (((x >= x3 && x <= x4) || (x >= x4 && x <= x3)) && ((x >= x1 && x <= x2) || (x >= x2 && x <= x1)))
            {
                var y = (det1 * height34 - height12 * det2) / det3;
                if (((y >= y3 && y <= y4) || (y >= y4 && y <= y3)) &&
                    ((y >= y1 && y <= y2) || (y >= y2 && y <= y1))) return true;
            }

            x3 = x4;
            y3 = y4;
        }

        return false;
    }

    public Polygon GetPolygon(BoundingBoxAttachment attachment)
    {
        var index = BoundingBoxes.IndexOf(attachment);
        return index == -1 ? null : Polygons.Items[index];
    }
}

public class Polygon
{
    public Polygon()
    {
        Vertices = new float[16];
    }

    public float[] Vertices { get; set; }
    public int Count { get; set; }
}