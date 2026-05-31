// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：DBLiteRuntime 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

using System;
using System.Collections.Generic;
using UnityEngine;

namespace rimrush
{
    public sealed class DBLiteFactory
    {
        private static DBLiteFactory instance;

        private readonly Dictionary<string, DBLiteSkeleton> skeletons = new Dictionary<string, DBLiteSkeleton>();
        private readonly Dictionary<string, DBLiteTextureAtlas> textureAtlases = new Dictionary<string, DBLiteTextureAtlas>();
        private readonly Dictionary<string, DBLiteArmatureData> armatures = new Dictionary<string, DBLiteArmatureData>();

        public static DBLiteFactory Instance => instance ?? (instance = new DBLiteFactory());

        /// <summary>
        /// Executes Ensure Loaded for the DBLiteFactory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void EnsureLoaded()
        {
            Load("sk2", "texture2", "texture2");
        }

        /// <summary>
        /// Executes Load for the DBLiteFactory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="skeletonKey">Input value used by this step of the workflow.</param>
        /// <param name="textureJsonKey">Input value used by this step of the workflow.</param>
        /// <param name="textureImageKey">Input value used by this step of the workflow.</param>
        public void Load(string skeletonKey, string textureJsonKey, string textureImageKey)
        {
            if (skeletons.ContainsKey(skeletonKey))
            {
                return;
            }

            var skeletonAsset = Resources.Load<TextAsset>($"rimrush/DragonBones/{skeletonKey}");
            var textureJsonAsset = Resources.Load<TextAsset>($"rimrush/DragonBones/{textureJsonKey}");
            var texture = Resources.Load<Texture2D>($"rimrush/DragonBones/{textureImageKey}");
            if (skeletonAsset == null || textureJsonAsset == null || texture == null)
            {
                Debug.LogError($"Missing DragonBones set {skeletonKey}/{textureJsonKey}/{textureImageKey}");
                return;
            }

            var textureAtlas = DBLiteTextureAtlas.Parse(textureJsonKey, texture, textureJsonAsset.text);
            textureAtlases[textureJsonKey] = textureAtlas;

            var skeleton = DBLiteSkeleton.Parse(skeletonAsset.text, textureAtlas);
            skeletons[skeletonKey] = skeleton;
            foreach (var pair in skeleton.Armatures)
            {
                armatures[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// Executes Build Armature for the DBLiteFactory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="armatureName">Input value used by this step of the workflow.</param>
        /// <param name="objectName">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public DBLiteArmature BuildArmature(string armatureName, string objectName = null)
        {
            EnsureLoaded();
            if (!armatures.TryGetValue(armatureName, out var data))
            {
                Debug.LogWarning($"Missing DragonBones armature {armatureName}");
                return null;
            }

            var go = new GameObject(objectName ?? armatureName);
            var armature = go.AddComponent<DBLiteArmature>();
            armature.Init(this, data);
            return armature;
        }

        /// <summary>
        /// Executes Try Get Armature for the DBLiteFactory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="armatureName">Input value used by this step of the workflow.</param>
        /// <param name="data">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool TryGetArmature(string armatureName, out DBLiteArmatureData data)
        {
            EnsureLoaded();
            return armatures.TryGetValue(armatureName, out data);
        }
    }

    public sealed class DBLiteArmature : MonoBehaviour
    {
        private const float SlotDepthStep = 0.001f;
        private DBLiteFactory factory;
        private DBLiteArmatureData data;
        private readonly Dictionary<string, DBLiteSlotInstance> slots = new Dictionary<string, DBLiteSlotInstance>();
        private readonly HashSet<string> hiddenSlots = new HashSet<string>();
        private readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        private DBLiteAnimationData currentAnimation;
        private float elapsedFrames;
        private bool animationCompleteSent;
        private bool playing = true;

        public string ArmatureName => data != null ? data.Name : string.Empty;
        public event Action<string> AnimationComplete;
        public event Action<string, string> FrameEvent;

        /// <summary>
        /// Executes Init for the DBLiteArmature workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="factory">Input value used by this step of the workflow.</param>
        /// <param name="data">Input value used by this step of the workflow.</param>
        public void Init(DBLiteFactory factory, DBLiteArmatureData data)
        {
            this.factory = factory;
            this.data = data;
            BuildBonesAndSlots();
            var firstAnimation = data.FirstAnimationName;
            if (!string.IsNullOrEmpty(firstAnimation))
            {
                Play(firstAnimation);
            }
            else
            {
                ApplyPose(0f);
            }
        }

        /// <summary>
        /// Executes Get Child Armature for the DBLiteArmature workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="slotName">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public DBLiteArmature GetChildArmature(string slotName)
        {
            return slots.TryGetValue(slotName, out var slot) ? slot.ChildArmature : null;
        }

        /// <summary>
        /// Executes Set Slot Hidden for the DBLiteArmature workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="slotName">Input value used by this step of the workflow.</param>
        /// <param name="hidden">Input value used by this step of the workflow.</param>
        public void SetSlotHidden(string slotName, bool hidden)
        {
            if (string.IsNullOrEmpty(slotName))
            {
                return;
            }

            if (hidden)
            {
                hiddenSlots.Add(slotName);
            }
            else
            {
                hiddenSlots.Remove(slotName);
            }

            if (!slots.TryGetValue(slotName, out var slot))
            {
                return;
            }

            if (hidden)
            {
                slot.SetAlpha(0f);
            }
            else
            {
                RefreshPose();
            }
        }

        /// <summary>
        /// Executes Play for the DBLiteArmature workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="animationName">Input value used by this step of the workflow.</param>
        /// <param name="restart">Input value used by this step of the workflow.</param>
        public void Play(string animationName, bool restart = true)
        {
            if (data == null)
            {
                return;
            }

            if (!data.Animations.TryGetValue(animationName, out var animation))
            {
                return;
            }

            if (currentAnimation != animation || restart)
            {
                currentAnimation = animation;
                elapsedFrames = 0f;
                animationCompleteSent = false;
            }

            playing = true;
            ApplyPose(elapsedFrames);
        }

        /// <summary>
        /// Executes Stop At Start for the DBLiteArmature workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="animationName">Input value used by this step of the workflow.</param>
        public void StopAtStart(string animationName)
        {
            Play(animationName);
            playing = false;
            elapsedFrames = 0f;
            animationCompleteSent = false;
            ApplyPose(0f);
        }

        /// <summary>
        /// Executes Refresh Pose for the DBLiteArmature workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void RefreshPose()
        {
            ApplyPose(elapsedFrames);
        }

        /// <summary>
        /// Executes Update for the DBLiteArmature workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void Update()
        {
            if (currentAnimation == null)
            {
                return;
            }

            if (playing)
            {
                var previousFrame = elapsedFrames;
                elapsedFrames += Time.deltaTime * data.FrameRate;
                DispatchFrameEvents(previousFrame, elapsedFrames);
                TryDispatchAnimationComplete(previousFrame, elapsedFrames);
            }

            ApplyPose(elapsedFrames);
        }

        /// <summary>
        /// Executes Dispatch Frame Events for the DBLiteArmature workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="previousFrame">Input value used by this step of the workflow.</param>
        /// <param name="currentFrame">Input value used by this step of the workflow.</param>
        private void DispatchFrameEvents(float previousFrame, float currentFrame)
        {
            if (currentAnimation == null || currentAnimation.FrameEvents.Count == 0)
            {
                return;
            }

            var duration = Mathf.Max(1f, currentAnimation.Duration);
            if (currentAnimation.Loops)
            {
                var previousLoop = Mathf.FloorToInt(previousFrame / duration);
                var currentLoop = Mathf.FloorToInt(currentFrame / duration);
                if (currentLoop == previousLoop)
                {
                    EmitFrameEventsInRange(Mod(previousFrame, duration), Mod(currentFrame, duration), duration);
                    return;
                }

                EmitFrameEventsInRange(Mod(previousFrame, duration), duration, duration);
                for (var loop = previousLoop + 1; loop < currentLoop; loop++)
                {
                    EmitFrameEventsInRange(0f, duration, duration);
                }

                EmitFrameEventsInRange(0f, Mod(currentFrame, duration), duration);
                return;
            }

            var start = Mathf.Clamp(previousFrame, 0f, duration);
            var end = Mathf.Clamp(currentFrame, 0f, duration);
            EmitFrameEventsInRange(start, end, duration);
        }

        /// <summary>
        /// Executes Try Dispatch Animation Complete for the DBLiteArmature workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="previousFrame">Input value used by this step of the workflow.</param>
        /// <param name="currentFrame">Input value used by this step of the workflow.</param>
        private void TryDispatchAnimationComplete(float previousFrame, float currentFrame)
        {
            if (currentAnimation == null || currentAnimation.Loops || animationCompleteSent)
            {
                return;
            }

            var duration = Mathf.Max(1f, currentAnimation.Duration);
            if (previousFrame < duration && currentFrame >= duration)
            {
                animationCompleteSent = true;
                AnimationComplete?.Invoke(currentAnimation.Name);
            }
        }

        /// <summary>
        /// Executes Emit Frame Events In Range for the DBLiteArmature workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="start">Input value used by this step of the workflow.</param>
        /// <param name="end">Input value used by this step of the workflow.</param>
        /// <param name="duration">Input value used by this step of the workflow.</param>
        private void EmitFrameEventsInRange(float start, float end, float duration)
        {
            if (end <= start)
            {
                return;
            }

            for (var i = 0; i < currentAnimation.FrameEvents.Count; i++)
            {
                var frameEvent = currentAnimation.FrameEvents[i];
                var frame = Mathf.Clamp(frameEvent.Frame, 0f, duration);
                if (frame > start && frame <= end)
                {
                    FrameEvent?.Invoke(currentAnimation.Name, frameEvent.EventName);
                }
            }
        }

        /// <summary>
        /// Executes Mod for the DBLiteArmature workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="value">Input value used by this step of the workflow.</param>
        /// <param name="divisor">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static float Mod(float value, float divisor)
        {
            return (value % divisor + divisor) % divisor;
        }

        /// <summary>
        /// Executes Build Bones And Slots for the DBLiteArmature workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void BuildBonesAndSlots()
        {
            foreach (var boneData in data.Bones)
            {
                var boneGo = new GameObject(boneData.Name);
                boneGo.transform.SetParent(transform, false);
                bones[boneData.Name] = boneGo.transform;
            }

            foreach (var boneData in data.Bones)
            {
                if (!string.IsNullOrEmpty(boneData.Parent) && bones.TryGetValue(boneData.Parent, out var parent))
                {
                    bones[boneData.Name].SetParent(parent, false);
                }
            }

            foreach (var slotData in data.Slots)
            {
                if (!bones.TryGetValue(slotData.Parent, out var parent))
                {
                    continue;
                }

                var slotGo = new GameObject(slotData.Name);
                slotGo.transform.SetParent(parent, false);
                slotGo.transform.localPosition = new Vector3(0f, 0f, -slotData.Order * SlotDepthStep);
                var slot = new DBLiteSlotInstance(slotData, data.GetDisplays(slotData.Name), slotGo.transform, factory);
                slots[slotData.Name] = slot;
            }
        }

        /// <summary>
        /// Executes Apply Pose for the DBLiteArmature workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="frame">Input value used by this step of the workflow.</param>
        private void ApplyPose(float frame)
        {
            if (data == null)
            {
                return;
            }

            var animFrame = 0f;
            if (currentAnimation != null)
            {
                var duration = Mathf.Max(1, currentAnimation.Duration);
                animFrame = currentAnimation.Loops ? frame % duration : Mathf.Min(frame, duration - 0.001f);
            }

            foreach (var boneData in data.Bones)
            {
                if (!bones.TryGetValue(boneData.Name, out var transform))
                {
                    continue;
                }

                var pose = boneData.Transform;
                if (currentAnimation != null && currentAnimation.BoneTracks.TryGetValue(boneData.Name, out var track))
                {
                    pose = pose.Combine(track.Sample(animFrame));
                }

                transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(
                    transform.parent,
                    new Vector3(pose.X, -pose.Y, 0f));
                transform.localRotation = Quaternion.Euler(0f, 0f, -pose.Rotation);
                transform.localScale = new Vector3(pose.ScaleX, pose.ScaleY, 1f);
            }

            foreach (var pair in slots)
            {
                pair.Value.ResetToSetupPose();
                if (currentAnimation != null && currentAnimation.SlotTracks.TryGetValue(pair.Key, out var slotTrack))
                {
                    var displayIndex = slotTrack.SampleDisplay(animFrame);
                    if (displayIndex != int.MinValue)
                    {
                        pair.Value.SetDisplay(displayIndex);
                    }

                    pair.Value.SetAlpha(slotTrack.SampleAlpha(animFrame));
                }

                if (hiddenSlots.Contains(pair.Key))
                {
                    pair.Value.SetAlpha(0f);
                }
            }
        }
    }

    public sealed class DBLiteTextureAtlas
    {
        private readonly Texture2D texture;
        private readonly float pixelsPerUnit;
        private readonly Dictionary<string, DBLiteSubTexture> subTextures = new Dictionary<string, DBLiteSubTexture>();
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        /// <summary>
        /// Executes DBLite Texture Atlas for the DBLiteTextureAtlas workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="texture">Input value used by this step of the workflow.</param>
        private DBLiteTextureAtlas(Texture2D texture, float pixelsPerUnit)
        {
            this.texture = texture;
            this.pixelsPerUnit = Mathf.Max(0.0001f, pixelsPerUnit);
        }

        /// <summary>
        /// Executes Parse for the DBLiteTextureAtlas workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="texture">Input value used by this step of the workflow.</param>
        /// <param name="json">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static DBLiteTextureAtlas Parse(string name, Texture2D texture, string json)
        {
            var root = rimrushJson.AsDict(rimrushJson.Parse(json));
            var atlas = new DBLiteTextureAtlas(texture, rimrushJson.Float(root, "pixelsPerUnit", 1f));
            var list = rimrushJson.List(root, "SubTexture");
            if (list == null)
            {
                return atlas;
            }

            foreach (var item in list)
            {
                var dict = rimrushJson.AsDict(item);
                if (dict == null)
                {
                    continue;
                }

                var sub = new DBLiteSubTexture
                {
                    Name = rimrushJson.String(dict, "name"),
                    X = rimrushJson.Float(dict, "x"),
                    Y = rimrushJson.Float(dict, "y"),
                    Width = rimrushJson.Float(dict, "width"),
                    Height = rimrushJson.Float(dict, "height")
                };
                atlas.subTextures[sub.Name] = sub;
            }

            return atlas;
        }

        /// <summary>
        /// Executes Sprite for the DBLiteTextureAtlas workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public Sprite Sprite(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (sprites.TryGetValue(name, out var cached))
            {
                return cached;
            }

            if (!subTextures.TryGetValue(name, out var sub))
            {
                return null;
            }

            var rect = new Rect(sub.X, texture.height - sub.Y - sub.Height, sub.Width, sub.Height);
            var sprite = UnityEngine.Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            sprites[name] = sprite;
            return sprite;
        }
    }

    public sealed class DBLiteSkeleton
    {
        public readonly Dictionary<string, DBLiteArmatureData> Armatures = new Dictionary<string, DBLiteArmatureData>();

        /// <summary>
        /// Executes Parse for the DBLiteSkeleton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="json">Input value used by this step of the workflow.</param>
        /// <param name="textureAtlas">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static DBLiteSkeleton Parse(string json, DBLiteTextureAtlas textureAtlas)
        {
            var skeleton = new DBLiteSkeleton();
            var root = rimrushJson.AsDict(rimrushJson.Parse(json));
            var frameRate = rimrushJson.Int(root, "frameRate", 30);
            var armatureList = rimrushJson.List(root, "armature");
            if (armatureList == null)
            {
                return skeleton;
            }

            foreach (var item in armatureList)
            {
                var dict = rimrushJson.AsDict(item);
                if (dict == null)
                {
                    continue;
                }

                var armature = DBLiteArmatureData.Parse(dict, textureAtlas, frameRate);
                skeleton.Armatures[armature.Name] = armature;
            }

            return skeleton;
        }
    }

    public sealed class DBLiteArmatureData
    {
        public string Name;
        public int FrameRate;
        public readonly List<DBLiteBoneData> Bones = new List<DBLiteBoneData>();
        public readonly List<DBLiteSlotData> Slots = new List<DBLiteSlotData>();
        public readonly Dictionary<string, List<DBLiteDisplayData>> DisplaysBySlot = new Dictionary<string, List<DBLiteDisplayData>>();
        public readonly Dictionary<string, DBLiteAnimationData> Animations = new Dictionary<string, DBLiteAnimationData>();
        public DBLiteTextureAtlas TextureAtlas;
        public string FirstAnimationName;

        /// <summary>
        /// Executes Get Displays for the DBLiteArmatureData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="slotName">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public List<DBLiteDisplayData> GetDisplays(string slotName)
        {
            return DisplaysBySlot.TryGetValue(slotName, out var displays) ? displays : null;
        }

        /// <summary>
        /// Executes Parse for the DBLiteArmatureData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dict">Input value used by this step of the workflow.</param>
        /// <param name="atlas">Input value used by this step of the workflow.</param>
        /// <param name="frameRate">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static DBLiteArmatureData Parse(Dictionary<string, object> dict, DBLiteTextureAtlas atlas, int frameRate)
        {
            var data = new DBLiteArmatureData
            {
                Name = rimrushJson.String(dict, "name"),
                FrameRate = frameRate,
                TextureAtlas = atlas
            };

            var boneList = rimrushJson.List(dict, "bone");
            if (boneList != null)
            {
                foreach (var item in boneList)
                {
                    var bone = rimrushJson.AsDict(item);
                    if (bone == null)
                    {
                        continue;
                    }

                    data.Bones.Add(new DBLiteBoneData
                    {
                        Name = rimrushJson.String(bone, "name"),
                        Parent = rimrushJson.String(bone, "parent"),
                        Transform = DBLiteTransform.FromJson(rimrushJson.Dict(bone, "transform"))
                    });
                }
            }

            var slotList = rimrushJson.List(dict, "slot");
            if (slotList != null)
            {
                for (var i = 0; i < slotList.Count; i++)
                {
                    var item = slotList[i];
                    var slot = rimrushJson.AsDict(item);
                    if (slot == null)
                    {
                        continue;
                    }

                    data.Slots.Add(new DBLiteSlotData
                    {
                        Name = rimrushJson.String(slot, "name"),
                        Parent = rimrushJson.String(slot, "parent"),
                        DisplayIndex = rimrushJson.Int(slot, "displayIndex", 0),
                        Order = i
                    });
                }
            }

            ParseSkin(dict, data);
            ParseAnimations(dict, data);
            return data;
        }

        /// <summary>
        /// Executes Parse Skin for the DBLiteArmatureData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dict">Input value used by this step of the workflow.</param>
        /// <param name="data">Input value used by this step of the workflow.</param>
        private static void ParseSkin(Dictionary<string, object> dict, DBLiteArmatureData data)
        {
            var skins = rimrushJson.List(dict, "skin");
            if (skins == null || skins.Count == 0)
            {
                return;
            }

            var firstSkin = rimrushJson.AsDict(skins[0]);
            var skinSlots = rimrushJson.List(firstSkin, "slot");
            if (skinSlots == null)
            {
                return;
            }

            foreach (var item in skinSlots)
            {
                var slot = rimrushJson.AsDict(item);
                if (slot == null)
                {
                    continue;
                }

                var slotName = rimrushJson.String(slot, "name");
                var displays = new List<DBLiteDisplayData>();
                var displayList = rimrushJson.List(slot, "display");
                if (displayList != null)
                {
                    foreach (var displayItem in displayList)
                    {
                        var displayDict = rimrushJson.AsDict(displayItem);
                        if (displayDict == null)
                        {
                            continue;
                        }

                        displays.Add(new DBLiteDisplayData
                        {
                            Name = rimrushJson.String(displayDict, "name"),
                            Type = rimrushJson.String(displayDict, "type", "image"),
                            Transform = DBLiteTransform.FromJson(rimrushJson.Dict(displayDict, "transform"))
                        });
                    }
                }

                data.DisplaysBySlot[slotName] = displays;
            }
        }

        /// <summary>
        /// Executes Parse Animations for the DBLiteArmatureData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dict">Input value used by this step of the workflow.</param>
        /// <param name="data">Input value used by this step of the workflow.</param>
        private static void ParseAnimations(Dictionary<string, object> dict, DBLiteArmatureData data)
        {
            var animationList = rimrushJson.List(dict, "animation");
            if (animationList == null)
            {
                return;
            }

            foreach (var item in animationList)
            {
                var animDict = rimrushJson.AsDict(item);
                if (animDict == null)
                {
                    continue;
                }

                var animation = DBLiteAnimationData.Parse(animDict);
                data.Animations[animation.Name] = animation;
                if (string.IsNullOrEmpty(data.FirstAnimationName))
                {
                    data.FirstAnimationName = animation.Name;
                }
            }
        }
    }

    public sealed class DBLiteAnimationData
    {
        public string Name;
        public int Duration = 1;
        public bool Loops;
        public readonly Dictionary<string, DBLiteBoneTrack> BoneTracks = new Dictionary<string, DBLiteBoneTrack>();
        public readonly Dictionary<string, DBLiteSlotTrack> SlotTracks = new Dictionary<string, DBLiteSlotTrack>();
        public readonly List<DBLiteAnimationFrameEvent> FrameEvents = new List<DBLiteAnimationFrameEvent>();

        /// <summary>
        /// Executes Parse for the DBLiteAnimationData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dict">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static DBLiteAnimationData Parse(Dictionary<string, object> dict)
        {
            var animation = new DBLiteAnimationData
            {
                Name = rimrushJson.String(dict, "name"),
                Duration = Mathf.Max(1, rimrushJson.Int(dict, "duration", 1)),
                Loops = rimrushJson.Int(dict, "playTimes", 1) == 0
            };

            var bones = rimrushJson.List(dict, "bone");
            if (bones != null)
            {
                foreach (var item in bones)
                {
                    var bone = rimrushJson.AsDict(item);
                    if (bone == null)
                    {
                        continue;
                    }

                    animation.BoneTracks[rimrushJson.String(bone, "name")] = DBLiteBoneTrack.Parse(bone);
                }
            }

            var slots = rimrushJson.List(dict, "slot");
            if (slots != null)
            {
                foreach (var item in slots)
                {
                    var slot = rimrushJson.AsDict(item);
                    if (slot == null)
                    {
                        continue;
                    }

                    animation.SlotTracks[rimrushJson.String(slot, "name")] = DBLiteSlotTrack.Parse(slot);
                }
            }

            var frames = rimrushJson.List(dict, "frame");
            if (frames != null)
            {
                var start = 0f;
                foreach (var item in frames)
                {
                    var frame = rimrushJson.AsDict(item);
                    if (frame == null)
                    {
                        continue;
                    }

                    var eventName = rimrushJson.String(frame, "event");
                    if (!string.IsNullOrEmpty(eventName))
                    {
                        animation.FrameEvents.Add(new DBLiteAnimationFrameEvent
                        {
                            Frame = start,
                            EventName = eventName
                        });
                    }

                    start += Mathf.Max(1, rimrushJson.Int(frame, "duration", 1));
                }
            }

            return animation;
        }
    }

    public struct DBLiteAnimationFrameEvent
    {
        public float Frame;
        public string EventName;
    }

    public sealed class DBLiteBoneTrack
    {
        private readonly List<DBLiteTimedTransform> translate = new List<DBLiteTimedTransform>();
        private readonly List<DBLiteTimedTransform> rotate = new List<DBLiteTimedTransform>();
        private readonly List<DBLiteTimedTransform> scale = new List<DBLiteTimedTransform>();

        /// <summary>
        /// Executes Parse for the DBLiteBoneTrack workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dict">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static DBLiteBoneTrack Parse(Dictionary<string, object> dict)
        {
            var track = new DBLiteBoneTrack();
            ParseTransformFrames(rimrushJson.List(dict, "translateFrame"), track.translate, FrameKind.Translate);
            ParseTransformFrames(rimrushJson.List(dict, "rotateFrame"), track.rotate, FrameKind.Rotate);
            ParseTransformFrames(rimrushJson.List(dict, "scaleFrame"), track.scale, FrameKind.Scale);
            return track;
        }

        /// <summary>
        /// Executes Sample for the DBLiteBoneTrack workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="frame">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public DBLiteTransform Sample(float frame)
        {
            var result = DBLiteTransform.Identity;
            var translation = SampleList(translate, frame, FrameKind.Translate);
            var rotation = SampleList(rotate, frame, FrameKind.Rotate);
            var scaling = SampleList(scale, frame, FrameKind.Scale);

            result.X = translation.X;
            result.Y = translation.Y;
            result.Rotation = rotation.Rotation;
            result.ScaleX = scaling.ScaleX;
            result.ScaleY = scaling.ScaleY;
            return result;
        }

        /// <summary>
        /// Executes Parse Transform Frames for the DBLiteBoneTrack workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="list">Input value used by this step of the workflow.</param>
        /// <param name="output">Input value used by this step of the workflow.</param>
        /// <param name="kind">Input value used by this step of the workflow.</param>
        private static void ParseTransformFrames(List<object> list, List<DBLiteTimedTransform> output, FrameKind kind)
        {
            if (list == null)
            {
                return;
            }

            var start = 0f;
            foreach (var item in list)
            {
                var dict = rimrushJson.AsDict(item);
                if (dict == null)
                {
                    continue;
                }

                var transform = DBLiteTransform.Identity;
                transform.X = rimrushJson.Float(dict, "x", 0f);
                transform.Y = rimrushJson.Float(dict, "y", 0f);
                transform.Rotation = rimrushJson.Float(dict, "rotate", 0f) + rimrushJson.Float(dict, "skew", 0f);
                transform.ScaleX = rimrushJson.Float(dict, "x", 1f);
                transform.ScaleY = rimrushJson.Float(dict, "y", 1f);

                if (kind == FrameKind.Translate)
                {
                    transform.ScaleX = 1f;
                    transform.ScaleY = 1f;
                    transform.Rotation = 0f;
                }
                else if (kind == FrameKind.Rotate)
                {
                    transform.X = 0f;
                    transform.Y = 0f;
                    transform.ScaleX = 1f;
                    transform.ScaleY = 1f;
                }
                else
                {
                    transform.X = 0f;
                    transform.Y = 0f;
                    transform.Rotation = 0f;
                }

                var duration = Mathf.Max(1, rimrushJson.Int(dict, "duration", 1));
                output.Add(new DBLiteTimedTransform
                {
                    Start = start,
                    Duration = duration,
                    Transform = transform,
                    // DragonBones treats missing tweenEasing and tweenEasing: 0 as linear tweening.
                    // Only an explicit null disables interpolation between transform keyframes.
                    Tween = !dict.ContainsKey("tweenEasing") || dict["tweenEasing"] != null
                });
                start += duration;
            }
        }

        /// <summary>
        /// Executes Sample List for the DBLiteBoneTrack workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="list">Input value used by this step of the workflow.</param>
        /// <param name="frame">Input value used by this step of the workflow.</param>
        /// <param name="kind">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static DBLiteTransform SampleList(List<DBLiteTimedTransform> list, float frame, FrameKind kind)
        {
            if (list.Count == 0)
            {
                return DBLiteTransform.Identity;
            }

            var current = list[0];
            DBLiteTimedTransform? next = null;
            for (var i = 0; i < list.Count; i++)
            {
                if (frame >= list[i].Start)
                {
                    current = list[i];
                    next = i + 1 < list.Count ? list[i + 1] : (DBLiteTimedTransform?)null;
                }
            }

            if (!current.Tween || !next.HasValue || current.Duration <= 0f)
            {
                return current.Transform;
            }

            var t = Mathf.Clamp01((frame - current.Start) / current.Duration);
            return DBLiteTransform.Lerp(current.Transform, next.Value.Transform, t, kind);
        }
    }

    public sealed class DBLiteSlotTrack
    {
        private readonly List<DBLiteDisplayFrame> displayFrames = new List<DBLiteDisplayFrame>();
        private readonly List<DBLiteColorFrame> colorFrames = new List<DBLiteColorFrame>();

        /// <summary>
        /// Executes Parse for the DBLiteSlotTrack workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dict">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static DBLiteSlotTrack Parse(Dictionary<string, object> dict)
        {
            var track = new DBLiteSlotTrack();
            var list = rimrushJson.List(dict, "displayFrame");
            if (list != null)
            {
                var start = 0f;
                foreach (var item in list)
                {
                    var frame = rimrushJson.AsDict(item);
                    if (frame == null)
                    {
                        continue;
                    }

                    var duration = Mathf.Max(1, rimrushJson.Int(frame, "duration", 1));
                    track.displayFrames.Add(new DBLiteDisplayFrame
                    {
                        Start = start,
                        Duration = duration,
                        Value = rimrushJson.Int(frame, "value", 0)
                    });
                    start += duration;
                }
            }

            var colorList = rimrushJson.List(dict, "colorFrame");
            if (colorList != null)
            {
                var start = 0f;
                foreach (var item in colorList)
                {
                    var frame = rimrushJson.AsDict(item);
                    if (frame == null)
                    {
                        continue;
                    }

                    var duration = Mathf.Max(1, rimrushJson.Int(frame, "duration", 1));
                    var value = rimrushJson.Dict(frame, "value");
                    var alphaMultiplier = Mathf.Clamp01(rimrushJson.Int(value, "aM", 100) / 100f);
                    track.colorFrames.Add(new DBLiteColorFrame
                    {
                        Start = start,
                        Duration = duration,
                        Alpha = alphaMultiplier
                    });
                    start += duration;
                }
            }

            return track;
        }

        /// <summary>
        /// Executes Sample Display for the DBLiteSlotTrack workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="frame">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public int SampleDisplay(float frame)
        {
            if (displayFrames.Count == 0)
            {
                return int.MinValue;
            }

            var current = displayFrames[0];
            for (var i = 0; i < displayFrames.Count; i++)
            {
                if (frame >= displayFrames[i].Start)
                {
                    current = displayFrames[i];
                }
            }

            return current.Value;
        }

        /// <summary>
        /// Executes Sample Alpha for the DBLiteSlotTrack workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="frame">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public float SampleAlpha(float frame)
        {
            if (colorFrames.Count == 0)
            {
                return 1f;
            }

            var current = colorFrames[0];
            for (var i = 0; i < colorFrames.Count; i++)
            {
                if (frame >= colorFrames[i].Start)
                {
                    current = colorFrames[i];
                }
            }

            return current.Alpha;
        }
    }

    public sealed class DBLiteSlotInstance
    {
        private readonly DBLiteSlotData slotData;
        private readonly List<DBLiteDisplayData> displays;
        private readonly Transform slotTransform;
        private readonly DBLiteFactory factory;
        private GameObject currentDisplayObject;
        private int currentDisplay = int.MinValue;
        private float currentAlpha = 1f;

        public DBLiteArmature ChildArmature { get; private set; }

        /// <summary>
        /// Executes DBLite Slot Instance for the DBLiteSlotInstance workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="slotData">Input value used by this step of the workflow.</param>
        /// <param name="displays">Input value used by this step of the workflow.</param>
        /// <param name="slotTransform">Input value used by this step of the workflow.</param>
        /// <param name="factory">Input value used by this step of the workflow.</param>
        public DBLiteSlotInstance(DBLiteSlotData slotData, List<DBLiteDisplayData> displays, Transform slotTransform, DBLiteFactory factory)
        {
            this.slotData = slotData;
            this.displays = displays ?? new List<DBLiteDisplayData>();
            this.slotTransform = slotTransform;
            this.factory = factory;
            SetDisplay(slotData.DisplayIndex);
        }

        /// <summary>
        /// Executes Set Display for the DBLiteSlotInstance workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="index">Input value used by this step of the workflow.</param>
        public void SetDisplay(int index)
        {
            if (index == currentDisplay)
            {
                return;
            }

            currentDisplay = index;
            if (currentDisplayObject != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEngine.Object.DestroyImmediate(currentDisplayObject);
                }
                else
#endif
                {
                    UnityEngine.Object.Destroy(currentDisplayObject);
                }

                currentDisplayObject = null;
                ChildArmature = null;
            }

            if (index < 0 || index >= displays.Count)
            {
                return;
            }

            var display = displays[index];
            if (display.Type == "armature")
            {
                var child = factory.BuildArmature(display.Name, $"{slotData.Name}:{display.Name}");
                if (child == null)
                {
                    return;
                }

                currentDisplayObject = child.gameObject;
                ChildArmature = child;
                currentDisplayObject.transform.SetParent(slotTransform, false);
                ApplyDisplayTransform(currentDisplayObject.transform, display.Transform);
            }
            else
            {
                var go = new GameObject($"{slotData.Name}:{display.Name}");
                go.transform.SetParent(slotTransform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = FindTextureAtlasSprite(display);
                renderer.sortingOrder = 20 + slotData.Order;
                currentDisplayObject = go;
                ApplyDisplayTransform(go.transform, display.Transform);
            }

            ApplyAlphaToCurrentDisplay(currentAlpha);
        }

        /// <summary>
        /// Executes Set Alpha for the DBLiteSlotInstance workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="alpha">Input value used by this step of the workflow.</param>
        public void SetAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            if (Mathf.Abs(currentAlpha - alpha) <= 0.0001f && currentDisplayObject != null)
            {
                return;
            }

            currentAlpha = alpha;
            ApplyAlphaToCurrentDisplay(currentAlpha);
        }

        /// <summary>
        /// Executes Reset To Setup Pose for the DBLiteSlotInstance workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void ResetToSetupPose()
        {
            SetDisplay(slotData.DisplayIndex);
            SetAlpha(1f);
        }

        /// <summary>
        /// Executes Find Texture Atlas Sprite for the DBLiteSlotInstance workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="display">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private Sprite FindTextureAtlasSprite(DBLiteDisplayData display)
        {
            if (display != null && display.Name == ".Game/ball/BallClip")
            {
                var themedBall = rimrushGameplaySpriteLoader.LoadBallThemeSprite(
                    rimrushInventory.Instance.MatchData.BallTheme,
                    0.5f,
                    0.5f);
                if (themedBall != null)
                {
                    return themedBall;
                }
            }

            var armature = slotTransform.GetComponentInParent<DBLiteArmature>();
            var dataField = typeof(DBLiteArmature).GetField("data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var armatureData = dataField != null ? dataField.GetValue(armature) as DBLiteArmatureData : null;
            return armatureData != null ? armatureData.TextureAtlas.Sprite(display.Name) : null;
        }

        /// <summary>
        /// Executes Apply Display Transform for the DBLiteSlotInstance workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="transform">Input value used by this step of the workflow.</param>
        /// <param name="displayTransform">Input value used by this step of the workflow.</param>
        private static void ApplyDisplayTransform(Transform transform, DBLiteTransform displayTransform)
        {
            transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(
                transform.parent,
                new Vector3(displayTransform.X, -displayTransform.Y, 0f));
            transform.localRotation = Quaternion.Euler(0f, 0f, -displayTransform.Rotation);
            transform.localScale = new Vector3(displayTransform.ScaleX, displayTransform.ScaleY, 1f);
        }

        /// <summary>
        /// Executes Apply Alpha To Current Display for the DBLiteSlotInstance workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="alpha">Input value used by this step of the workflow.</param>
        private void ApplyAlphaToCurrentDisplay(float alpha)
        {
            if (currentDisplayObject == null)
            {
                return;
            }

            if (ChildArmature != null)
            {
                currentDisplayObject.SetActive(alpha > 0.001f);
                return;
            }

            var renderers = currentDisplayObject.GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var color = renderer.color;
                color.a = alpha;
                renderer.color = color;
                renderer.enabled = alpha > 0.001f;
            }
        }
    }

    public sealed class DBLiteBoneData
    {
        public string Name;
        public string Parent;
        public DBLiteTransform Transform;
    }

    public sealed class DBLiteSlotData
    {
        public string Name;
        public string Parent;
        public int DisplayIndex;
        public int Order;
    }

    public sealed class DBLiteDisplayData
    {
        public string Name;
        public string Type;
        public DBLiteTransform Transform;
    }

    public struct DBLiteTransform
    {
        public float X;
        public float Y;
        public float Rotation;
        public float ScaleX;
        public float ScaleY;

        public static DBLiteTransform Identity => new DBLiteTransform
        {
            X = 0f,
            Y = 0f,
            Rotation = 0f,
            ScaleX = 1f,
            ScaleY = 1f
        };

        /// <summary>
        /// Executes From Json for the DBLiteDisplayData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dict">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static DBLiteTransform FromJson(Dictionary<string, object> dict)
        {
            var transform = Identity;
            if (dict == null)
            {
                return transform;
            }

            transform.X = rimrushJson.Float(dict, "x", 0f);
            transform.Y = rimrushJson.Float(dict, "y", 0f);
            transform.Rotation = rimrushJson.Float(dict, "skX", rimrushJson.Float(dict, "rotate", 0f));
            transform.ScaleX = rimrushJson.Float(dict, "scX", 1f);
            transform.ScaleY = rimrushJson.Float(dict, "scY", 1f);
            return transform;
        }

        /// <summary>
        /// Executes Combine for the DBLiteDisplayData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="animation">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public DBLiteTransform Combine(DBLiteTransform animation)
        {
            return new DBLiteTransform
            {
                X = X + animation.X,
                Y = Y + animation.Y,
                Rotation = Rotation + animation.Rotation,
                ScaleX = ScaleX * animation.ScaleX,
                ScaleY = ScaleY * animation.ScaleY
            };
        }

        /// <summary>
        /// Executes Lerp for the DBLiteDisplayData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="a">Input value used by this step of the workflow.</param>
        /// <param name="b">Input value used by this step of the workflow.</param>
        /// <param name="t">Input value used by this step of the workflow.</param>
        /// <param name="kind">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static DBLiteTransform Lerp(DBLiteTransform a, DBLiteTransform b, float t, FrameKind kind)
        {
            var result = Identity;
            if (kind == FrameKind.Translate)
            {
                result.X = Mathf.Lerp(a.X, b.X, t);
                result.Y = Mathf.Lerp(a.Y, b.Y, t);
            }
            else if (kind == FrameKind.Rotate)
            {
                result.Rotation = Mathf.LerpAngle(a.Rotation, b.Rotation, t);
            }
            else
            {
                result.ScaleX = Mathf.Lerp(a.ScaleX, b.ScaleX, t);
                result.ScaleY = Mathf.Lerp(a.ScaleY, b.ScaleY, t);
            }

            return result;
        }
    }

    public struct DBLiteTimedTransform
    {
        public float Start;
        public float Duration;
        public bool Tween;
        public DBLiteTransform Transform;
    }

    public struct DBLiteDisplayFrame
    {
        public float Start;
        public float Duration;
        public int Value;
    }

    public struct DBLiteColorFrame
    {
        public float Start;
        public float Duration;
        public float Alpha;
    }

    public struct DBLiteSubTexture
    {
        public string Name;
        public float X;
        public float Y;
        public float Width;
        public float Height;
    }

    public enum FrameKind
    {
        Translate,
        Rotate,
        Scale
    }
}
