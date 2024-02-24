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

#if (UNITY_5 || UNITY_5_3_OR_NEWER || UNITY_WSA || UNITY_WP8 || UNITY_WP8_1)
#define IS_UNITY
#endif

#if WINDOWS_STOREAPP
using System.Threading.Tasks;
using Windows.Storage;
#endif

namespace Spine;

public class SkeletonJson
{
    public float Scale { get; set; }

    private readonly AttachmentLoader attachmentLoader;
    private readonly List<LinkedMesh> linkedMeshes = new();

    public SkeletonJson(params Atlas[] atlasArray)
        : this(new AtlasAttachmentLoader(atlasArray))
    {
    }

    public SkeletonJson(AttachmentLoader attachmentLoader)
    {
        if (attachmentLoader == null)
            throw new ArgumentNullException("attachmentLoader", "attachmentLoader cannot be null.");
        this.attachmentLoader = attachmentLoader;
        Scale = 1;
    }

#if !IS_UNITY && WINDOWS_STOREAPP
		private async Task<SkeletonData> ReadFile(string path) {
			var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
			var file = await folder.GetFileAsync(path).AsTask().ConfigureAwait(false);
			using (var reader = new StreamReader(await file.OpenStreamForReadAsync().ConfigureAwait(false))) {
				SkeletonData skeletonData = ReadSkeletonData(reader);
				skeletonData.Name = Path.GetFileNameWithoutExtension(path);
				return skeletonData;
			}
		}

		public SkeletonData ReadSkeletonData (string path) {
			return this.ReadFile(path).Result;
		}
#else
    public SkeletonData ReadSkeletonData(string path)
    {
#if WINDOWS_PHONE
			using (var reader = new StreamReader(Microsoft.Xna.Framework.TitleContainer.OpenStream(path))) {
#else
        using (var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
        {
#endif
            var skeletonData = ReadSkeletonData(reader);
            skeletonData.name = Path.GetFileNameWithoutExtension(path);
            return skeletonData;
        }
    }
#endif

    public SkeletonData ReadSkeletonData(TextReader reader)
    {
        if (reader == null) throw new ArgumentNullException("reader", "reader cannot be null.");

        var scale = Scale;
        var skeletonData = new SkeletonData();

        var root = Json.Deserialize(reader) as Dictionary<string, object>;
        if (root == null) throw new Exception("Invalid JSON.");

        // Skeleton.
        if (root.ContainsKey("skeleton"))
        {
            var skeletonMap = (Dictionary<string, object>)root["skeleton"];
            skeletonData.hash = (string)skeletonMap["hash"];
            skeletonData.version = (string)skeletonMap["spine"];
            if ("3.8.75" == skeletonData.version)
                throw new Exception("Unsupported skeleton data, please export with a newer version of Spine.");
            skeletonData.x = GetFloat(skeletonMap, "x", 0);
            skeletonData.y = GetFloat(skeletonMap, "y", 0);
            skeletonData.width = GetFloat(skeletonMap, "width", 0);
            skeletonData.height = GetFloat(skeletonMap, "height", 0);
            skeletonData.fps = GetFloat(skeletonMap, "fps", 30);
            skeletonData.imagesPath = GetString(skeletonMap, "images", null);
            skeletonData.audioPath = GetString(skeletonMap, "audio", null);
        }

        // Bones.
        if (root.ContainsKey("bones"))
            foreach (Dictionary<string, object> boneMap in (List<object>)root["bones"])
            {
                BoneData parent = null;
                if (boneMap.ContainsKey("parent"))
                {
                    parent = skeletonData.FindBone((string)boneMap["parent"]);
                    if (parent == null)
                        throw new Exception("Parent bone not found: " + boneMap["parent"]);
                }

                var data = new BoneData(skeletonData.Bones.Count, (string)boneMap["name"], parent);
                data.length = GetFloat(boneMap, "length", 0) * scale;
                data.x = GetFloat(boneMap, "x", 0) * scale;
                data.y = GetFloat(boneMap, "y", 0) * scale;
                data.rotation = GetFloat(boneMap, "rotation", 0);
                data.scaleX = GetFloat(boneMap, "scaleX", 1);
                data.scaleY = GetFloat(boneMap, "scaleY", 1);
                data.shearX = GetFloat(boneMap, "shearX", 0);
                data.shearY = GetFloat(boneMap, "shearY", 0);

                var tm = GetString(boneMap, "transform", TransformMode.Normal.ToString());
                data.transformMode = (TransformMode)Enum.Parse(typeof(TransformMode), tm, true);
                data.skinRequired = GetBoolean(boneMap, "skin", false);

                skeletonData.bones.Add(data);
            }

        // Slots.
        if (root.ContainsKey("slots"))
            foreach (Dictionary<string, object> slotMap in (List<object>)root["slots"])
            {
                var slotName = (string)slotMap["name"];
                var boneName = (string)slotMap["bone"];
                var boneData = skeletonData.FindBone(boneName);
                if (boneData == null) throw new Exception("Slot bone not found: " + boneName);
                var data = new SlotData(skeletonData.Slots.Count, slotName, boneData);

                if (slotMap.ContainsKey("color"))
                {
                    var color = (string)slotMap["color"];
                    data.r = ToColor(color, 0);
                    data.g = ToColor(color, 1);
                    data.b = ToColor(color, 2);
                    data.a = ToColor(color, 3);
                }

                if (slotMap.ContainsKey("dark"))
                {
                    var color2 = (string)slotMap["dark"];
                    data.r2 = ToColor(color2, 0, 6); // expectedLength = 6. ie. "RRGGBB"
                    data.g2 = ToColor(color2, 1, 6);
                    data.b2 = ToColor(color2, 2, 6);
                    data.hasSecondColor = true;
                }

                data.attachmentName = GetString(slotMap, "attachment", null);
                if (slotMap.ContainsKey("blend"))
                    data.blendMode = (BlendMode)Enum.Parse(typeof(BlendMode), (string)slotMap["blend"], true);
                else
                    data.blendMode = BlendMode.Normal;
                skeletonData.slots.Add(data);
            }

        // IK constraints.
        if (root.ContainsKey("ik"))
            foreach (Dictionary<string, object> constraintMap in (List<object>)root["ik"])
            {
                var data = new IkConstraintData((string)constraintMap["name"]);
                data.order = GetInt(constraintMap, "order", 0);
                data.skinRequired = GetBoolean(constraintMap, "skin", false);

                if (constraintMap.ContainsKey("bones"))
                    foreach (string boneName in (List<object>)constraintMap["bones"])
                    {
                        var bone = skeletonData.FindBone(boneName);
                        if (bone == null) throw new Exception("IK bone not found: " + boneName);
                        data.bones.Add(bone);
                    }

                var targetName = (string)constraintMap["target"];
                data.target = skeletonData.FindBone(targetName);
                if (data.target == null) throw new Exception("IK target bone not found: " + targetName);
                data.mix = GetFloat(constraintMap, "mix", 1);
                data.softness = GetFloat(constraintMap, "softness", 0) * scale;
                data.bendDirection = GetBoolean(constraintMap, "bendPositive", true) ? 1 : -1;
                data.compress = GetBoolean(constraintMap, "compress", false);
                data.stretch = GetBoolean(constraintMap, "stretch", false);
                data.uniform = GetBoolean(constraintMap, "uniform", false);

                skeletonData.ikConstraints.Add(data);
            }

        // Transform constraints.
        if (root.ContainsKey("transform"))
            foreach (Dictionary<string, object> constraintMap in (List<object>)root["transform"])
            {
                var data = new TransformConstraintData((string)constraintMap["name"]);
                data.order = GetInt(constraintMap, "order", 0);
                data.skinRequired = GetBoolean(constraintMap, "skin", false);

                if (constraintMap.ContainsKey("bones"))
                    foreach (string boneName in (List<object>)constraintMap["bones"])
                    {
                        var bone = skeletonData.FindBone(boneName);
                        if (bone == null) throw new Exception("Transform constraint bone not found: " + boneName);
                        data.bones.Add(bone);
                    }

                var targetName = (string)constraintMap["target"];
                data.target = skeletonData.FindBone(targetName);
                if (data.target == null)
                    throw new Exception("Transform constraint target bone not found: " + targetName);

                data.local = GetBoolean(constraintMap, "local", false);
                data.relative = GetBoolean(constraintMap, "relative", false);

                data.offsetRotation = GetFloat(constraintMap, "rotation", 0);
                data.offsetX = GetFloat(constraintMap, "x", 0) * scale;
                data.offsetY = GetFloat(constraintMap, "y", 0) * scale;
                data.offsetScaleX = GetFloat(constraintMap, "scaleX", 0);
                data.offsetScaleY = GetFloat(constraintMap, "scaleY", 0);
                data.offsetShearY = GetFloat(constraintMap, "shearY", 0);

                data.rotateMix = GetFloat(constraintMap, "rotateMix", 1);
                data.translateMix = GetFloat(constraintMap, "translateMix", 1);
                data.scaleMix = GetFloat(constraintMap, "scaleMix", 1);
                data.shearMix = GetFloat(constraintMap, "shearMix", 1);

                skeletonData.transformConstraints.Add(data);
            }

        // Path constraints.
        if (root.ContainsKey("path"))
            foreach (Dictionary<string, object> constraintMap in (List<object>)root["path"])
            {
                var data = new PathConstraintData((string)constraintMap["name"]);
                data.order = GetInt(constraintMap, "order", 0);
                data.skinRequired = GetBoolean(constraintMap, "skin", false);

                if (constraintMap.ContainsKey("bones"))
                    foreach (string boneName in (List<object>)constraintMap["bones"])
                    {
                        var bone = skeletonData.FindBone(boneName);
                        if (bone == null) throw new Exception("Path bone not found: " + boneName);
                        data.bones.Add(bone);
                    }

                var targetName = (string)constraintMap["target"];
                data.target = skeletonData.FindSlot(targetName);
                if (data.target == null) throw new Exception("Path target slot not found: " + targetName);

                data.positionMode = (PositionMode)Enum.Parse(typeof(PositionMode),
                    GetString(constraintMap, "positionMode", "percent"), true);
                data.spacingMode = (SpacingMode)Enum.Parse(typeof(SpacingMode),
                    GetString(constraintMap, "spacingMode", "length"), true);
                data.rotateMode = (RotateMode)Enum.Parse(typeof(RotateMode),
                    GetString(constraintMap, "rotateMode", "tangent"), true);
                data.offsetRotation = GetFloat(constraintMap, "rotation", 0);
                data.position = GetFloat(constraintMap, "position", 0);
                if (data.positionMode == PositionMode.Fixed) data.position *= scale;
                data.spacing = GetFloat(constraintMap, "spacing", 0);
                if (data.spacingMode == SpacingMode.Length || data.spacingMode == SpacingMode.Fixed)
                    data.spacing *= scale;
                data.rotateMix = GetFloat(constraintMap, "rotateMix", 1);
                data.translateMix = GetFloat(constraintMap, "translateMix", 1);

                skeletonData.pathConstraints.Add(data);
            }

        // Skins.
        if (root.ContainsKey("skins"))
            foreach (Dictionary<string, object> skinMap in (List<object>)root["skins"])
            {
                var skin = new Skin((string)skinMap["name"]);
                if (skinMap.ContainsKey("bones"))
                    foreach (string entryName in (List<object>)skinMap["bones"])
                    {
                        var bone = skeletonData.FindBone(entryName);
                        if (bone == null) throw new Exception("Skin bone not found: " + entryName);
                        skin.bones.Add(bone);
                    }

                if (skinMap.ContainsKey("ik"))
                    foreach (string entryName in (List<object>)skinMap["ik"])
                    {
                        var constraint = skeletonData.FindIkConstraint(entryName);
                        if (constraint == null) throw new Exception("Skin IK constraint not found: " + entryName);
                        skin.constraints.Add(constraint);
                    }

                if (skinMap.ContainsKey("transform"))
                    foreach (string entryName in (List<object>)skinMap["transform"])
                    {
                        var constraint = skeletonData.FindTransformConstraint(entryName);
                        if (constraint == null)
                            throw new Exception("Skin transform constraint not found: " + entryName);
                        skin.constraints.Add(constraint);
                    }

                if (skinMap.ContainsKey("path"))
                    foreach (string entryName in (List<object>)skinMap["path"])
                    {
                        var constraint = skeletonData.FindPathConstraint(entryName);
                        if (constraint == null) throw new Exception("Skin path constraint not found: " + entryName);
                        skin.constraints.Add(constraint);
                    }

                if (skinMap.ContainsKey("attachments"))
                    foreach (var slotEntry in (Dictionary<string, object>)skinMap["attachments"])
                    {
                        var slotIndex = skeletonData.FindSlotIndex(slotEntry.Key);
                        foreach (var entry in (Dictionary<string, object>)slotEntry.Value)
                            try
                            {
                                var attachment = ReadAttachment((Dictionary<string, object>)entry.Value, skin,
                                    slotIndex, entry.Key, skeletonData);
                                if (attachment != null) skin.SetAttachment(slotIndex, entry.Key, attachment);
                            }
                            catch (Exception e)
                            {
                                throw new Exception("Error reading attachment: " + entry.Key + ", skin: " + skin, e);
                            }
                    }

                skeletonData.skins.Add(skin);
                if (skin.name == "default") skeletonData.defaultSkin = skin;
            }

        // Linked meshes.
        for (int i = 0, n = linkedMeshes.Count; i < n; i++)
        {
            var linkedMesh = linkedMeshes[i];
            var skin = linkedMesh.skin == null ? skeletonData.defaultSkin : skeletonData.FindSkin(linkedMesh.skin);
            if (skin == null) throw new Exception("Slot not found: " + linkedMesh.skin);
            var parent = skin.GetAttachment(linkedMesh.slotIndex, linkedMesh.parent);
            if (parent == null) throw new Exception("Parent mesh not found: " + linkedMesh.parent);
            linkedMesh.mesh.DeformAttachment = linkedMesh.inheritDeform ? (VertexAttachment)parent : linkedMesh.mesh;
            linkedMesh.mesh.ParentMesh = (MeshAttachment)parent;
            linkedMesh.mesh.UpdateUVs();
        }

        linkedMeshes.Clear();

        // Events.
        if (root.ContainsKey("events"))
            foreach (var entry in (Dictionary<string, object>)root["events"])
            {
                var entryMap = (Dictionary<string, object>)entry.Value;
                var data = new EventData(entry.Key);
                data.Int = GetInt(entryMap, "int", 0);
                data.Float = GetFloat(entryMap, "float", 0);
                data.String = GetString(entryMap, "string", string.Empty);
                data.AudioPath = GetString(entryMap, "audio", null);
                if (data.AudioPath != null)
                {
                    data.Volume = GetFloat(entryMap, "volume", 1);
                    data.Balance = GetFloat(entryMap, "balance", 0);
                }

                skeletonData.events.Add(data);
            }

        // Animations.
        if (root.ContainsKey("animations"))
            foreach (var entry in (Dictionary<string, object>)root["animations"])
                try
                {
                    ReadAnimation((Dictionary<string, object>)entry.Value, entry.Key, skeletonData);
                }
                catch (Exception e)
                {
                    throw new Exception("Error reading animation: " + entry.Key, e);
                }

        skeletonData.bones.TrimExcess();
        skeletonData.slots.TrimExcess();
        skeletonData.skins.TrimExcess();
        skeletonData.events.TrimExcess();
        skeletonData.animations.TrimExcess();
        skeletonData.ikConstraints.TrimExcess();
        return skeletonData;
    }

    private Attachment ReadAttachment(Dictionary<string, object> map, Skin skin, int slotIndex, string name,
        SkeletonData skeletonData)
    {
        var scale = Scale;
        name = GetString(map, "name", name);

        var typeName = GetString(map, "type", "region");
        var type = (AttachmentType)Enum.Parse(typeof(AttachmentType), typeName, true);

        var path = GetString(map, "path", name);

        switch (type)
        {
            case AttachmentType.Region:
                var region = attachmentLoader.NewRegionAttachment(skin, name, path);
                if (region == null) return null;
                region.Path = path;
                region.x = GetFloat(map, "x", 0) * scale;
                region.y = GetFloat(map, "y", 0) * scale;
                region.scaleX = GetFloat(map, "scaleX", 1);
                region.scaleY = GetFloat(map, "scaleY", 1);
                region.rotation = GetFloat(map, "rotation", 0);
                region.width = GetFloat(map, "width", 32) * scale;
                region.height = GetFloat(map, "height", 32) * scale;

                if (map.ContainsKey("color"))
                {
                    var color = (string)map["color"];
                    region.r = ToColor(color, 0);
                    region.g = ToColor(color, 1);
                    region.b = ToColor(color, 2);
                    region.a = ToColor(color, 3);
                }

                region.UpdateOffset();
                return region;
            case AttachmentType.Boundingbox:
                var box = attachmentLoader.NewBoundingBoxAttachment(skin, name);
                if (box == null) return null;
                ReadVertices(map, box, GetInt(map, "vertexCount", 0) << 1);
                return box;
            case AttachmentType.Mesh:
            case AttachmentType.Linkedmesh:
            {
                var mesh = attachmentLoader.NewMeshAttachment(skin, name, path);
                if (mesh == null) return null;
                mesh.Path = path;

                if (map.ContainsKey("color"))
                {
                    var color = (string)map["color"];
                    mesh.r = ToColor(color, 0);
                    mesh.g = ToColor(color, 1);
                    mesh.b = ToColor(color, 2);
                    mesh.a = ToColor(color, 3);
                }

                mesh.Width = GetFloat(map, "width", 0) * scale;
                mesh.Height = GetFloat(map, "height", 0) * scale;

                var parent = GetString(map, "parent", null);
                if (parent != null)
                {
                    linkedMeshes.Add(new LinkedMesh(mesh, GetString(map, "skin", null), slotIndex, parent,
                        GetBoolean(map, "deform", true)));
                    return mesh;
                }

                var uvs = GetFloatArray(map, "uvs", 1);
                ReadVertices(map, mesh, uvs.Length);
                mesh.triangles = GetIntArray(map, "triangles");
                mesh.regionUVs = uvs;
                mesh.UpdateUVs();

                if (map.ContainsKey("hull")) mesh.HullLength = GetInt(map, "hull", 0) * 2;
                if (map.ContainsKey("edges")) mesh.Edges = GetIntArray(map, "edges");
                return mesh;
            }
            case AttachmentType.Path:
            {
                var pathAttachment = attachmentLoader.NewPathAttachment(skin, name);
                if (pathAttachment == null) return null;
                pathAttachment.closed = GetBoolean(map, "closed", false);
                pathAttachment.constantSpeed = GetBoolean(map, "constantSpeed", true);

                var vertexCount = GetInt(map, "vertexCount", 0);
                ReadVertices(map, pathAttachment, vertexCount << 1);

                // potential BOZO see Java impl
                pathAttachment.lengths = GetFloatArray(map, "lengths", scale);
                return pathAttachment;
            }
            case AttachmentType.Point:
            {
                var point = attachmentLoader.NewPointAttachment(skin, name);
                if (point == null) return null;
                point.x = GetFloat(map, "x", 0) * scale;
                point.y = GetFloat(map, "y", 0) * scale;
                point.rotation = GetFloat(map, "rotation", 0);

                //string color = GetString(map, "color", null);
                //if (color != null) point.color = color;
                return point;
            }
            case AttachmentType.Clipping:
            {
                var clip = attachmentLoader.NewClippingAttachment(skin, name);
                if (clip == null) return null;

                var end = GetString(map, "end", null);
                if (end != null)
                {
                    var slot = skeletonData.FindSlot(end);
                    if (slot == null) throw new Exception("Clipping end slot not found: " + end);
                    clip.EndSlot = slot;
                }

                ReadVertices(map, clip, GetInt(map, "vertexCount", 0) << 1);

                //string color = GetString(map, "color", null);
                // if (color != null) clip.color = color;
                return clip;
            }
        }

        return null;
    }

    private void ReadVertices(Dictionary<string, object> map, VertexAttachment attachment, int verticesLength)
    {
        attachment.WorldVerticesLength = verticesLength;
        var vertices = GetFloatArray(map, "vertices", 1);
        var scale = Scale;
        if (verticesLength == vertices.Length)
        {
            if (scale != 1)
                for (var i = 0; i < vertices.Length; i++)
                    vertices[i] *= scale;
            attachment.vertices = vertices;
            return;
        }

        var weights = new ExposedList<float>(verticesLength * 3 * 3);
        var bones = new ExposedList<int>(verticesLength * 3);
        for (int i = 0, n = vertices.Length; i < n;)
        {
            var boneCount = (int)vertices[i++];
            bones.Add(boneCount);
            for (var nn = i + boneCount * 4; i < nn; i += 4)
            {
                bones.Add((int)vertices[i]);
                weights.Add(vertices[i + 1] * Scale);
                weights.Add(vertices[i + 2] * Scale);
                weights.Add(vertices[i + 3]);
            }
        }

        attachment.bones = bones.ToArray();
        attachment.vertices = weights.ToArray();
    }

    private void ReadAnimation(Dictionary<string, object> map, string name, SkeletonData skeletonData)
    {
        var scale = Scale;
        var timelines = new ExposedList<Timeline>();
        float duration = 0;

        // Slot timelines.
        if (map.ContainsKey("slots"))
            foreach (var entry in (Dictionary<string, object>)map["slots"])
            {
                var slotName = entry.Key;
                var slotIndex = skeletonData.FindSlotIndex(slotName);
                var timelineMap = (Dictionary<string, object>)entry.Value;
                foreach (var timelineEntry in timelineMap)
                {
                    var values = (List<object>)timelineEntry.Value;
                    var timelineName = timelineEntry.Key;
                    if (timelineName == "attachment")
                    {
                        var timeline = new AttachmentTimeline(values.Count);
                        timeline.slotIndex = slotIndex;

                        var frameIndex = 0;
                        foreach (Dictionary<string, object> valueMap in values)
                        {
                            var time = GetFloat(valueMap, "time", 0);
                            timeline.SetFrame(frameIndex++, time, (string)valueMap["name"]);
                        }

                        timelines.Add(timeline);
                        duration = Math.Max(duration, timeline.frames[timeline.FrameCount - 1]);
                    }
                    else if (timelineName == "color")
                    {
                        var timeline = new ColorTimeline(values.Count);
                        timeline.slotIndex = slotIndex;

                        var frameIndex = 0;
                        foreach (Dictionary<string, object> valueMap in values)
                        {
                            var time = GetFloat(valueMap, "time", 0);
                            var c = (string)valueMap["color"];
                            timeline.SetFrame(frameIndex, time, ToColor(c, 0), ToColor(c, 1), ToColor(c, 2),
                                ToColor(c, 3));
                            ReadCurve(valueMap, timeline, frameIndex);
                            frameIndex++;
                        }

                        timelines.Add(timeline);
                        duration = Math.Max(duration,
                            timeline.frames[(timeline.FrameCount - 1) * ColorTimeline.ENTRIES]);
                    }
                    else if (timelineName == "twoColor")
                    {
                        var timeline = new TwoColorTimeline(values.Count);
                        timeline.slotIndex = slotIndex;

                        var frameIndex = 0;
                        foreach (Dictionary<string, object> valueMap in values)
                        {
                            var time = GetFloat(valueMap, "time", 0);
                            var light = (string)valueMap["light"];
                            var dark = (string)valueMap["dark"];
                            timeline.SetFrame(frameIndex, time, ToColor(light, 0), ToColor(light, 1), ToColor(light, 2),
                                ToColor(light, 3),
                                ToColor(dark, 0, 6), ToColor(dark, 1, 6), ToColor(dark, 2, 6));
                            ReadCurve(valueMap, timeline, frameIndex);
                            frameIndex++;
                        }

                        timelines.Add(timeline);
                        duration = Math.Max(duration,
                            timeline.frames[(timeline.FrameCount - 1) * TwoColorTimeline.ENTRIES]);
                    }
                    else
                    {
                        throw new Exception("Invalid timeline type for a slot: " + timelineName + " (" + slotName +
                                            ")");
                    }
                }
            }

        // Bone timelines.
        if (map.ContainsKey("bones"))
            foreach (var entry in (Dictionary<string, object>)map["bones"])
            {
                var boneName = entry.Key;
                var boneIndex = skeletonData.FindBoneIndex(boneName);
                if (boneIndex == -1) throw new Exception("Bone not found: " + boneName);
                var timelineMap = (Dictionary<string, object>)entry.Value;
                foreach (var timelineEntry in timelineMap)
                {
                    var values = (List<object>)timelineEntry.Value;
                    var timelineName = timelineEntry.Key;
                    if (timelineName == "rotate")
                    {
                        var timeline = new RotateTimeline(values.Count);
                        timeline.boneIndex = boneIndex;

                        var frameIndex = 0;
                        foreach (Dictionary<string, object> valueMap in values)
                        {
                            timeline.SetFrame(frameIndex, GetFloat(valueMap, "time", 0),
                                GetFloat(valueMap, "angle", 0));
                            ReadCurve(valueMap, timeline, frameIndex);
                            frameIndex++;
                        }

                        timelines.Add(timeline);
                        duration = Math.Max(duration,
                            timeline.frames[(timeline.FrameCount - 1) * RotateTimeline.ENTRIES]);
                    }
                    else if (timelineName == "translate" || timelineName == "scale" || timelineName == "shear")
                    {
                        TranslateTimeline timeline;
                        float timelineScale = 1, defaultValue = 0;
                        if (timelineName == "scale")
                        {
                            timeline = new ScaleTimeline(values.Count);
                            defaultValue = 1;
                        }
                        else if (timelineName == "shear")
                        {
                            timeline = new ShearTimeline(values.Count);
                        }
                        else
                        {
                            timeline = new TranslateTimeline(values.Count);
                            timelineScale = scale;
                        }

                        timeline.boneIndex = boneIndex;

                        var frameIndex = 0;
                        foreach (Dictionary<string, object> valueMap in values)
                        {
                            var time = GetFloat(valueMap, "time", 0);
                            var x = GetFloat(valueMap, "x", defaultValue);
                            var y = GetFloat(valueMap, "y", defaultValue);
                            timeline.SetFrame(frameIndex, time, x * timelineScale, y * timelineScale);
                            ReadCurve(valueMap, timeline, frameIndex);
                            frameIndex++;
                        }

                        timelines.Add(timeline);
                        duration = Math.Max(duration,
                            timeline.frames[(timeline.FrameCount - 1) * TranslateTimeline.ENTRIES]);
                    }
                    else
                    {
                        throw new Exception("Invalid timeline type for a bone: " + timelineName + " (" + boneName +
                                            ")");
                    }
                }
            }

        // IK constraint timelines.
        if (map.ContainsKey("ik"))
            foreach (var constraintMap in (Dictionary<string, object>)map["ik"])
            {
                var constraint = skeletonData.FindIkConstraint(constraintMap.Key);
                var values = (List<object>)constraintMap.Value;
                var timeline = new IkConstraintTimeline(values.Count);
                timeline.ikConstraintIndex = skeletonData.ikConstraints.IndexOf(constraint);
                var frameIndex = 0;
                foreach (Dictionary<string, object> valueMap in values)
                {
                    timeline.SetFrame(frameIndex, GetFloat(valueMap, "time", 0), GetFloat(valueMap, "mix", 1),
                        GetFloat(valueMap, "softness", 0) * scale, GetBoolean(valueMap, "bendPositive", true) ? 1 : -1,
                        GetBoolean(valueMap, "compress", false), GetBoolean(valueMap, "stretch", false));
                    ReadCurve(valueMap, timeline, frameIndex);
                    frameIndex++;
                }

                timelines.Add(timeline);
                duration = Math.Max(duration,
                    timeline.frames[(timeline.FrameCount - 1) * IkConstraintTimeline.ENTRIES]);
            }

        // Transform constraint timelines.
        if (map.ContainsKey("transform"))
            foreach (var constraintMap in (Dictionary<string, object>)map["transform"])
            {
                var constraint = skeletonData.FindTransformConstraint(constraintMap.Key);
                var values = (List<object>)constraintMap.Value;
                var timeline = new TransformConstraintTimeline(values.Count);
                timeline.transformConstraintIndex = skeletonData.transformConstraints.IndexOf(constraint);
                var frameIndex = 0;
                foreach (Dictionary<string, object> valueMap in values)
                {
                    timeline.SetFrame(frameIndex, GetFloat(valueMap, "time", 0), GetFloat(valueMap, "rotateMix", 1),
                        GetFloat(valueMap, "translateMix", 1), GetFloat(valueMap, "scaleMix", 1),
                        GetFloat(valueMap, "shearMix", 1));
                    ReadCurve(valueMap, timeline, frameIndex);
                    frameIndex++;
                }

                timelines.Add(timeline);
                duration = Math.Max(duration,
                    timeline.frames[(timeline.FrameCount - 1) * TransformConstraintTimeline.ENTRIES]);
            }

        // Path constraint timelines.
        if (map.ContainsKey("path"))
            foreach (var constraintMap in (Dictionary<string, object>)map["path"])
            {
                var index = skeletonData.FindPathConstraintIndex(constraintMap.Key);
                if (index == -1) throw new Exception("Path constraint not found: " + constraintMap.Key);
                var data = skeletonData.pathConstraints.Items[index];
                var timelineMap = (Dictionary<string, object>)constraintMap.Value;
                foreach (var timelineEntry in timelineMap)
                {
                    var values = (List<object>)timelineEntry.Value;
                    var timelineName = (string)timelineEntry.Key;
                    if (timelineName == "position" || timelineName == "spacing")
                    {
                        PathConstraintPositionTimeline timeline;
                        float timelineScale = 1;
                        if (timelineName == "spacing")
                        {
                            timeline = new PathConstraintSpacingTimeline(values.Count);
                            if (data.spacingMode == SpacingMode.Length || data.spacingMode == SpacingMode.Fixed)
                                timelineScale = scale;
                        }
                        else
                        {
                            timeline = new PathConstraintPositionTimeline(values.Count);
                            if (data.positionMode == PositionMode.Fixed) timelineScale = scale;
                        }

                        timeline.pathConstraintIndex = index;
                        var frameIndex = 0;
                        foreach (Dictionary<string, object> valueMap in values)
                        {
                            timeline.SetFrame(frameIndex, GetFloat(valueMap, "time", 0),
                                GetFloat(valueMap, timelineName, 0) * timelineScale);
                            ReadCurve(valueMap, timeline, frameIndex);
                            frameIndex++;
                        }

                        timelines.Add(timeline);
                        duration = Math.Max(duration,
                            timeline.frames[(timeline.FrameCount - 1) * PathConstraintPositionTimeline.ENTRIES]);
                    }
                    else if (timelineName == "mix")
                    {
                        var timeline = new PathConstraintMixTimeline(values.Count);
                        timeline.pathConstraintIndex = index;
                        var frameIndex = 0;
                        foreach (Dictionary<string, object> valueMap in values)
                        {
                            timeline.SetFrame(frameIndex, GetFloat(valueMap, "time", 0),
                                GetFloat(valueMap, "rotateMix", 1),
                                GetFloat(valueMap, "translateMix", 1));
                            ReadCurve(valueMap, timeline, frameIndex);
                            frameIndex++;
                        }

                        timelines.Add(timeline);
                        duration = Math.Max(duration,
                            timeline.frames[(timeline.FrameCount - 1) * PathConstraintMixTimeline.ENTRIES]);
                    }
                }
            }

        // Deform timelines.
        if (map.ContainsKey("deform"))
            foreach (var deformMap in (Dictionary<string, object>)map["deform"])
            {
                var skin = skeletonData.FindSkin(deformMap.Key);
                foreach (var slotMap in (Dictionary<string, object>)deformMap.Value)
                {
                    var slotIndex = skeletonData.FindSlotIndex(slotMap.Key);
                    if (slotIndex == -1) throw new Exception("Slot not found: " + slotMap.Key);
                    foreach (var timelineMap in (Dictionary<string, object>)slotMap.Value)
                    {
                        var values = (List<object>)timelineMap.Value;
                        var attachment = (VertexAttachment)skin.GetAttachment(slotIndex, timelineMap.Key);
                        if (attachment == null) throw new Exception("Deform attachment not found: " + timelineMap.Key);
                        var weighted = attachment.bones != null;
                        var vertices = attachment.vertices;
                        var deformLength = weighted ? vertices.Length / 3 * 2 : vertices.Length;

                        var timeline = new DeformTimeline(values.Count);
                        timeline.slotIndex = slotIndex;
                        timeline.attachment = attachment;

                        var frameIndex = 0;
                        foreach (Dictionary<string, object> valueMap in values)
                        {
                            float[] deform;
                            if (!valueMap.ContainsKey("vertices"))
                            {
                                deform = weighted ? new float[deformLength] : vertices;
                            }
                            else
                            {
                                deform = new float[deformLength];
                                var start = GetInt(valueMap, "offset", 0);
                                var verticesValue = GetFloatArray(valueMap, "vertices", 1);
                                Array.Copy(verticesValue, 0, deform, start, verticesValue.Length);
                                if (scale != 1)
                                    for (int i = start, n = i + verticesValue.Length; i < n; i++)
                                        deform[i] *= scale;

                                if (!weighted)
                                    for (var i = 0; i < deformLength; i++)
                                        deform[i] += vertices[i];
                            }

                            timeline.SetFrame(frameIndex, GetFloat(valueMap, "time", 0), deform);
                            ReadCurve(valueMap, timeline, frameIndex);
                            frameIndex++;
                        }

                        timelines.Add(timeline);
                        duration = Math.Max(duration, timeline.frames[timeline.FrameCount - 1]);
                    }
                }
            }

        // Draw order timeline.
        if (map.ContainsKey("drawOrder") || map.ContainsKey("draworder"))
        {
            var values = (List<object>)map[map.ContainsKey("drawOrder") ? "drawOrder" : "draworder"];
            var timeline = new DrawOrderTimeline(values.Count);
            var slotCount = skeletonData.slots.Count;
            var frameIndex = 0;
            foreach (Dictionary<string, object> drawOrderMap in values)
            {
                int[] drawOrder = null;
                if (drawOrderMap.ContainsKey("offsets"))
                {
                    drawOrder = new int[slotCount];
                    for (var i = slotCount - 1; i >= 0; i--)
                        drawOrder[i] = -1;
                    var offsets = (List<object>)drawOrderMap["offsets"];
                    var unchanged = new int[slotCount - offsets.Count];
                    int originalIndex = 0, unchangedIndex = 0;
                    foreach (Dictionary<string, object> offsetMap in offsets)
                    {
                        var slotIndex = skeletonData.FindSlotIndex((string)offsetMap["slot"]);
                        if (slotIndex == -1) throw new Exception("Slot not found: " + offsetMap["slot"]);
                        // Collect unchanged items.
                        while (originalIndex != slotIndex)
                            unchanged[unchangedIndex++] = originalIndex++;
                        // Set changed items.
                        var index = originalIndex + (int)(float)offsetMap["offset"];
                        drawOrder[index] = originalIndex++;
                    }

                    // Collect remaining unchanged items.
                    while (originalIndex < slotCount)
                        unchanged[unchangedIndex++] = originalIndex++;
                    // Fill in unchanged items.
                    for (var i = slotCount - 1; i >= 0; i--)
                        if (drawOrder[i] == -1)
                            drawOrder[i] = unchanged[--unchangedIndex];
                }

                timeline.SetFrame(frameIndex++, GetFloat(drawOrderMap, "time", 0), drawOrder);
            }

            timelines.Add(timeline);
            duration = Math.Max(duration, timeline.frames[timeline.FrameCount - 1]);
        }

        // Event timeline.
        if (map.ContainsKey("events"))
        {
            var eventsMap = (List<object>)map["events"];
            var timeline = new EventTimeline(eventsMap.Count);
            var frameIndex = 0;
            foreach (Dictionary<string, object> eventMap in eventsMap)
            {
                var eventData = skeletonData.FindEvent((string)eventMap["name"]);
                if (eventData == null) throw new Exception("Event not found: " + eventMap["name"]);
                var e = new Event(GetFloat(eventMap, "time", 0), eventData)
                {
                    intValue = GetInt(eventMap, "int", eventData.Int),
                    floatValue = GetFloat(eventMap, "float", eventData.Float),
                    stringValue = GetString(eventMap, "string", eventData.String)
                };
                if (e.data.AudioPath != null)
                {
                    e.volume = GetFloat(eventMap, "volume", eventData.Volume);
                    e.balance = GetFloat(eventMap, "balance", eventData.Balance);
                }

                timeline.SetFrame(frameIndex++, e);
            }

            timelines.Add(timeline);
            duration = Math.Max(duration, timeline.frames[timeline.FrameCount - 1]);
        }

        timelines.TrimExcess();
        skeletonData.animations.Add(new Animation(name, timelines, duration));
    }

    private static void ReadCurve(Dictionary<string, object> valueMap, CurveTimeline timeline, int frameIndex)
    {
        if (!valueMap.ContainsKey("curve"))
            return;
        var curveObject = valueMap["curve"];
        if (curveObject is string)
            timeline.SetStepped(frameIndex);
        else
            timeline.SetCurve(frameIndex, (float)curveObject, GetFloat(valueMap, "c2", 0), GetFloat(valueMap, "c3", 1),
                GetFloat(valueMap, "c4", 1));
    }

    internal class LinkedMesh
    {
        internal bool inheritDeform;
        internal MeshAttachment mesh;
        internal string parent, skin;
        internal int slotIndex;

        public LinkedMesh(MeshAttachment mesh, string skin, int slotIndex, string parent, bool inheritDeform)
        {
            this.mesh = mesh;
            this.skin = skin;
            this.slotIndex = slotIndex;
            this.parent = parent;
            this.inheritDeform = inheritDeform;
        }
    }

    private static float[] GetFloatArray(Dictionary<string, object> map, string name, float scale)
    {
        var list = (List<object>)map[name];
        var values = new float[list.Count];
        if (scale == 1)
            for (int i = 0, n = list.Count; i < n; i++)
                values[i] = (float)list[i];
        else
            for (int i = 0, n = list.Count; i < n; i++)
                values[i] = (float)list[i] * scale;
        return values;
    }

    private static int[] GetIntArray(Dictionary<string, object> map, string name)
    {
        var list = (List<object>)map[name];
        var values = new int[list.Count];
        for (int i = 0, n = list.Count; i < n; i++)
            values[i] = (int)(float)list[i];
        return values;
    }

    private static float GetFloat(Dictionary<string, object> map, string name, float defaultValue)
    {
        if (!map.ContainsKey(name))
            return defaultValue;
        return (float)map[name];
    }

    private static int GetInt(Dictionary<string, object> map, string name, int defaultValue)
    {
        if (!map.ContainsKey(name))
            return defaultValue;
        return (int)(float)map[name];
    }

    private static bool GetBoolean(Dictionary<string, object> map, string name, bool defaultValue)
    {
        if (!map.ContainsKey(name))
            return defaultValue;
        return (bool)map[name];
    }

    private static string GetString(Dictionary<string, object> map, string name, string defaultValue)
    {
        if (!map.ContainsKey(name))
            return defaultValue;
        return (string)map[name];
    }

    private static float ToColor(string hexString, int colorIndex, int expectedLength = 8)
    {
        if (hexString.Length != expectedLength)
            throw new ArgumentException(
                "Color hexidecimal length must be " + expectedLength + ", recieved: " + hexString, "hexString");
        return Convert.ToInt32(hexString.Substring(colorIndex * 2, 2), 16) / (float)255;
    }
}