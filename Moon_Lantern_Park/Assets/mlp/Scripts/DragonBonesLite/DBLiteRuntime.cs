// DragonBones 骨骼动画运行时 / 加载和播放 DragonBones 格式的骨骼动画，让角色能做出跑步、跳跃、投篮等各种动作。负责解析动画数据、管理骨骼层级、插值关键帧、显示插槽上的图片。游戏中所有角色动画都靠这个系统驱动。

using System;
using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    public sealed class DBLiteFactory
    {
        private static DBLiteFactory instance;

        private readonly Dictionary<string, DBLiteSkeleton> skeletons = new Dictionary<string, DBLiteSkeleton>();
        private readonly Dictionary<string, DBLiteTextureAtlas> textureAtlases = new Dictionary<string, DBLiteTextureAtlas>();
        private readonly Dictionary<string, DBLiteArmatureData> armatures = new Dictionary<string, DBLiteArmatureData>();

        public static DBLiteFactory Instance => instance ?? (instance = new DBLiteFactory());

        /// <summary>
        /// 如果尚未加载，则加载默认的 DragonBones 骨架和纹理。
        /// </summary>
        public void EnsureLoaded()
        {
            Load("sk2", "texture2", "texture2");
        }

        /// <summary>
        /// 从 Resources 加载 DragonBones 骨架、纹理图集 JSON 和纹理图片。
        /// </summary>
        public void Load(string skeletonKey, string textureJsonKey, string textureImageKey)
        {
            if (skeletons.ContainsKey(skeletonKey))
            {
                return;
            }

            var skeletonAsset = Resources.Load<TextAsset>($"mlp/DragonBones/{skeletonKey}");
            var textureJsonAsset = Resources.Load<TextAsset>($"mlp/DragonBones/{textureJsonKey}");
            var texture = Resources.Load<Texture2D>($"mlp/DragonBones/{textureImageKey}");
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
        /// 为指定名称的骨架创建一个带有 DBLiteArmature 组件的新 GameObject。
        /// </summary>
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
        /// 尝试根据名称查找已加载的骨架定义。
        /// </summary>
        public bool TryGetArmature(string armatureName, out DBLiteArmatureData data)
        {
            EnsureLoaded();
            return armatures.TryGetValue(armatureName, out data);
        }

        /// <summary>
        /// 根据名称从纹理图集返回精灵，未找到则返回 null。
        /// </summary>
        public Sprite GetTextureSprite(string spriteName, string textureAtlasKey = "texture2")
        {
            EnsureLoaded();
            return textureAtlases.TryGetValue(textureAtlasKey, out var atlas)
                ? atlas.Sprite(spriteName)
                : null;
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
        /// 使用数据初始化骨架，构建骨骼层级，并播放第一个动画。
        /// </summary>
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
        /// 返回指定插槽中显示的子骨架（如果有）。
        /// </summary>
        public DBLiteArmature GetChildArmature(string slotName)
        {
            return slots.TryGetValue(slotName, out var slot) ? slot.ChildArmature : null;
        }

        /// <summary>
        /// 根据名称显示或隐藏指定插槽。
        /// </summary>
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
        /// 从头开始播放指定名称的动画。
        /// </summary>
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
        /// 加载动画但冻结在第 0 帧，不播放。
        /// </summary>
        public void StopAtStart(string animationName)
        {
            Play(animationName);
            playing = false;
            elapsedFrames = 0f;
            animationCompleteSent = false;
            ApplyPose(0f);
        }

        /// <summary>
        /// 重新应用当前动画姿态（更改插槽或可见性后调用）。
        /// </summary>
        public void RefreshPose()
        {
            ApplyPose(elapsedFrames);
        }

        /// <summary>
        /// 每帧推进动画计时器并应用当前姿态。
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
        /// 触发前一帧和当前帧之间的所有帧事件。
        /// </summary>
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
        /// 当非循环动画播放到末尾时触发 AnimationComplete 事件。
        /// </summary>
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
        /// 触发时间戳在指定范围内的帧事件。
        /// </summary>
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
        /// 返回值对除数的正取模结果。
        /// </summary>
        private static float Mod(float value, float divisor)
        {
            return (value % divisor + divisor) % divisor;
        }

        /// <summary>
        /// 根据骨架数据创建骨骼变换层级和插槽显示对象。
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
        /// 在指定帧采样当前动画，并将变换应用到所有骨骼和插槽。
        /// </summary>
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

                transform.localPosition = mlpConstants.SnapLocalPositionToScreenPixels(
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
        /// 使用给定的纹理和每单位像素比例创建纹理图集。
        /// </summary>
        private DBLiteTextureAtlas(Texture2D texture, float pixelsPerUnit)
        {
            this.texture = texture;
            this.pixelsPerUnit = Mathf.Max(0.0001f, pixelsPerUnit);
        }

        /// <summary>
        /// 从 JSON 定义和纹理解析 DragonBones 纹理图集。
        /// </summary>
        public static DBLiteTextureAtlas Parse(string name, Texture2D texture, string json)
        {
            var root = mlpJson.AsDict(mlpJson.Parse(json));
            var atlas = new DBLiteTextureAtlas(texture, mlpJson.Float(root, "pixelsPerUnit", 1f));
            var list = mlpJson.List(root, "SubTexture");
            if (list == null)
            {
                return atlas;
            }

            foreach (var item in list)
            {
                var dict = mlpJson.AsDict(item);
                if (dict == null)
                {
                    continue;
                }

                var sub = new DBLiteSubTexture
                {
                    Name = mlpJson.String(dict, "name"),
                    X = mlpJson.Float(dict, "x"),
                    Y = mlpJson.Float(dict, "y"),
                    Width = mlpJson.Float(dict, "width"),
                    Height = mlpJson.Float(dict, "height")
                };
                atlas.subTextures[sub.Name] = sub;
            }

            return atlas;
        }

        /// <summary>
        /// 返回指定子纹理的缓存精灵，未找到则返回 null。
        /// </summary>
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
        /// 解析 DragonBones 骨架文件，提取所有骨架定义。
        /// </summary>
        public static DBLiteSkeleton Parse(string json, DBLiteTextureAtlas textureAtlas)
        {
            var skeleton = new DBLiteSkeleton();
            var root = mlpJson.AsDict(mlpJson.Parse(json));
            var frameRate = mlpJson.Int(root, "frameRate", 30);
            var armatureList = mlpJson.List(root, "armature");
            if (armatureList == null)
            {
                return skeleton;
            }

            foreach (var item in armatureList)
            {
                var dict = mlpJson.AsDict(item);
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
        /// 返回指定插槽的显示条目列表，若不存在则返回 null。
        /// </summary>
        public List<DBLiteDisplayData> GetDisplays(string slotName)
        {
            return DisplaysBySlot.TryGetValue(slotName, out var displays) ? displays : null;
        }

        /// <summary>
        /// 从 JSON 解析骨架定义，包括骨骼、插槽、皮肤和动画。
        /// </summary>
        public static DBLiteArmatureData Parse(Dictionary<string, object> dict, DBLiteTextureAtlas atlas, int frameRate)
        {
            var data = new DBLiteArmatureData
            {
                Name = mlpJson.String(dict, "name"),
                FrameRate = frameRate,
                TextureAtlas = atlas
            };

            var boneList = mlpJson.List(dict, "bone");
            if (boneList != null)
            {
                foreach (var item in boneList)
                {
                    var bone = mlpJson.AsDict(item);
                    if (bone == null)
                    {
                        continue;
                    }

                    data.Bones.Add(new DBLiteBoneData
                    {
                        Name = mlpJson.String(bone, "name"),
                        Parent = mlpJson.String(bone, "parent"),
                        Transform = DBLiteTransform.FromJson(mlpJson.Dict(bone, "transform"))
                    });
                }
            }

            var slotList = mlpJson.List(dict, "slot");
            if (slotList != null)
            {
                for (var i = 0; i < slotList.Count; i++)
                {
                    var item = slotList[i];
                    var slot = mlpJson.AsDict(item);
                    if (slot == null)
                    {
                        continue;
                    }

                    data.Slots.Add(new DBLiteSlotData
                    {
                        Name = mlpJson.String(slot, "name"),
                        Parent = mlpJson.String(slot, "parent"),
                        DisplayIndex = mlpJson.Int(slot, "displayIndex", 0),
                        Order = i
                    });
                }
            }

            ParseSkin(dict, data);
            ParseAnimations(dict, data);
            return data;
        }

        /// <summary>
        /// 解析皮肤定义以填充每个插槽的显示数据。
        /// </summary>
        private static void ParseSkin(Dictionary<string, object> dict, DBLiteArmatureData data)
        {
            var skins = mlpJson.List(dict, "skin");
            if (skins == null || skins.Count == 0)
            {
                return;
            }

            var firstSkin = mlpJson.AsDict(skins[0]);
            var skinSlots = mlpJson.List(firstSkin, "slot");
            if (skinSlots == null)
            {
                return;
            }

            foreach (var item in skinSlots)
            {
                var slot = mlpJson.AsDict(item);
                if (slot == null)
                {
                    continue;
                }

                var slotName = mlpJson.String(slot, "name");
                var displays = new List<DBLiteDisplayData>();
                var displayList = mlpJson.List(slot, "display");
                if (displayList != null)
                {
                    foreach (var displayItem in displayList)
                    {
                        var displayDict = mlpJson.AsDict(displayItem);
                        if (displayDict == null)
                        {
                            continue;
                        }

                        displays.Add(new DBLiteDisplayData
                        {
                            Name = mlpJson.String(displayDict, "name"),
                            Type = mlpJson.String(displayDict, "type", "image"),
                            Transform = DBLiteTransform.FromJson(mlpJson.Dict(displayDict, "transform"))
                        });
                    }
                }

                data.DisplaysBySlot[slotName] = displays;
            }
        }

        /// <summary>
        /// 解析所有动画定义并保存第一个动画的名称。
        /// </summary>
        private static void ParseAnimations(Dictionary<string, object> dict, DBLiteArmatureData data)
        {
            var animationList = mlpJson.List(dict, "animation");
            if (animationList == null)
            {
                return;
            }

            foreach (var item in animationList)
            {
                var animDict = mlpJson.AsDict(item);
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
        /// 解析包含骨骼轨道、插槽轨道和帧事件的动画定义。
        /// </summary>
        public static DBLiteAnimationData Parse(Dictionary<string, object> dict)
        {
            var animation = new DBLiteAnimationData
            {
                Name = mlpJson.String(dict, "name"),
                Duration = Mathf.Max(1, mlpJson.Int(dict, "duration", 1)),
                Loops = mlpJson.Int(dict, "playTimes", 1) == 0
            };

            var bones = mlpJson.List(dict, "bone");
            if (bones != null)
            {
                foreach (var item in bones)
                {
                    var bone = mlpJson.AsDict(item);
                    if (bone == null)
                    {
                        continue;
                    }

                    animation.BoneTracks[mlpJson.String(bone, "name")] = DBLiteBoneTrack.Parse(bone);
                }
            }

            var slots = mlpJson.List(dict, "slot");
            if (slots != null)
            {
                foreach (var item in slots)
                {
                    var slot = mlpJson.AsDict(item);
                    if (slot == null)
                    {
                        continue;
                    }

                    animation.SlotTracks[mlpJson.String(slot, "name")] = DBLiteSlotTrack.Parse(slot);
                }
            }

            var frames = mlpJson.List(dict, "frame");
            if (frames != null)
            {
                var start = 0f;
                foreach (var item in frames)
                {
                    var frame = mlpJson.AsDict(item);
                    if (frame == null)
                    {
                        continue;
                    }

                    var eventName = mlpJson.String(frame, "event");
                    if (!string.IsNullOrEmpty(eventName))
                    {
                        animation.FrameEvents.Add(new DBLiteAnimationFrameEvent
                        {
                            Frame = start,
                            EventName = eventName
                        });
                    }

                    start += Mathf.Max(1, mlpJson.Int(frame, "duration", 1));
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
        /// 从 JSON 解析骨骼轨道的平移、旋转和缩放关键帧。
        /// </summary>
        public static DBLiteBoneTrack Parse(Dictionary<string, object> dict)
        {
            var track = new DBLiteBoneTrack();
            ParseTransformFrames(mlpJson.List(dict, "translateFrame"), track.translate, FrameKind.Translate);
            ParseTransformFrames(mlpJson.List(dict, "rotateFrame"), track.rotate, FrameKind.Rotate);
            ParseTransformFrames(mlpJson.List(dict, "scaleFrame"), track.scale, FrameKind.Scale);
            return track;
        }

        /// <summary>
        /// 在指定帧采样骨骼轨道，在关键帧之间进行插值。
        /// </summary>
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
        /// 从 JSON 数组解析关键帧列表（平移、旋转或缩放）。
        /// </summary>
        private static void ParseTransformFrames(List<object> list, List<DBLiteTimedTransform> output, FrameKind kind)
        {
            if (list == null)
            {
                return;
            }

            var start = 0f;
            foreach (var item in list)
            {
                var dict = mlpJson.AsDict(item);
                if (dict == null)
                {
                    continue;
                }

                var transform = DBLiteTransform.Identity;
                transform.X = mlpJson.Float(dict, "x", 0f);
                transform.Y = mlpJson.Float(dict, "y", 0f);
                transform.Rotation = mlpJson.Float(dict, "rotate", 0f) + mlpJson.Float(dict, "skew", 0f);
                transform.ScaleX = mlpJson.Float(dict, "x", 1f);
                transform.ScaleY = mlpJson.Float(dict, "y", 1f);

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

                var duration = Mathf.Max(1, mlpJson.Int(dict, "duration", 1));
                output.Add(new DBLiteTimedTransform
                {
                    Start = start,
                    Duration = duration,
                    Transform = transform,
                    // DragonBones 将缺失的 tweenEasing 和 tweenEasing: 0 视为线性补间。
                    // 只有显式的 null 值才会禁用变换关键帧之间的插值。
                    Tween = !dict.ContainsKey("tweenEasing") || dict["tweenEasing"] != null
                });
                start += duration;
            }
        }

        /// <summary>
        /// 查找包围指定帧的关键帧并进行变换插值。
        /// </summary>
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
        /// 从 JSON 解析插槽轨道的显示和颜色关键帧。
        /// </summary>
        public static DBLiteSlotTrack Parse(Dictionary<string, object> dict)
        {
            var track = new DBLiteSlotTrack();
            var list = mlpJson.List(dict, "displayFrame");
            if (list != null)
            {
                var start = 0f;
                foreach (var item in list)
                {
                    var frame = mlpJson.AsDict(item);
                    if (frame == null)
                    {
                        continue;
                    }

                    var duration = Mathf.Max(1, mlpJson.Int(frame, "duration", 1));
                    track.displayFrames.Add(new DBLiteDisplayFrame
                    {
                        Start = start,
                        Duration = duration,
                        Value = mlpJson.Int(frame, "value", 0)
                    });
                    start += duration;
                }
            }

            var colorList = mlpJson.List(dict, "colorFrame");
            if (colorList != null)
            {
                var start = 0f;
                foreach (var item in colorList)
                {
                    var frame = mlpJson.AsDict(item);
                    if (frame == null)
                    {
                        continue;
                    }

                    var duration = Mathf.Max(1, mlpJson.Int(frame, "duration", 1));
                    var value = mlpJson.Dict(frame, "value");
                    var alphaMultiplier = Mathf.Clamp01(mlpJson.Int(value, "aM", 100) / 100f);
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
        /// 返回指定帧激活的显示索引。
        /// </summary>
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
        /// 返回指定帧激活的透明度乘数。
        /// </summary>
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
        /// 创建管理当前显示对象和子骨架的插槽实例。
        /// </summary>
        public DBLiteSlotInstance(DBLiteSlotData slotData, List<DBLiteDisplayData> displays, Transform slotTransform, DBLiteFactory factory)
        {
            this.slotData = slotData;
            this.displays = displays ?? new List<DBLiteDisplayData>();
            this.slotTransform = slotTransform;
            this.factory = factory;
            SetDisplay(slotData.DisplayIndex);
        }

        /// <summary>
        /// 切换插槽以通过索引显示不同的内容（图片或子骨架）。
        /// </summary>
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
        /// 设置当前显示对象的不透明度（0 = 不可见，1 = 完全可见）。
        /// </summary>
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
        /// 将插槽重置为初始显示索引和完全不透明。
        /// </summary>
        public void ResetToSetupPose()
        {
            SetDisplay(slotData.DisplayIndex);
            SetAlpha(1f);
        }

        /// <summary>
        /// 查找显示内容的精灵，球体剪辑插槽使用主题球精灵。
        /// </summary>
        private Sprite FindTextureAtlasSprite(DBLiteDisplayData display)
        {
            if (display != null && display.Name == ".Game/ball/BallClip")
            {
                var themedBall = mlpGameplaySpriteLoader.LoadBallThemeSprite(
                    mlpInventory.Instance.MatchData.BallTheme,
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
        /// 将显示变换的位置、旋转和缩放应用到 GameObject。
        /// </summary>
        private static void ApplyDisplayTransform(Transform transform, DBLiteTransform displayTransform)
        {
            transform.localPosition = mlpConstants.SnapLocalPositionToScreenPixels(
                transform.parent,
                new Vector3(displayTransform.X, -displayTransform.Y, 0f));
            transform.localRotation = Quaternion.Euler(0f, 0f, -displayTransform.Rotation);
            transform.localScale = new Vector3(displayTransform.ScaleX, displayTransform.ScaleY, 1f);
        }

        /// <summary>
        /// 将透明度应用到当前显示对象的所有 SpriteRenderer，或切换子骨架的可见性。
        /// </summary>
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
        /// 从 JSON 字典解析变换数据（x、y、旋转、缩放）。
        /// </summary>
        public static DBLiteTransform FromJson(Dictionary<string, object> dict)
        {
            var transform = Identity;
            if (dict == null)
            {
                return transform;
            }

            transform.X = mlpJson.Float(dict, "x", 0f);
            transform.Y = mlpJson.Float(dict, "y", 0f);
            transform.Rotation = mlpJson.Float(dict, "skX", mlpJson.Float(dict, "rotate", 0f));
            transform.ScaleX = mlpJson.Float(dict, "scX", 1f);
            transform.ScaleY = mlpJson.Float(dict, "scY", 1f);
            return transform;
        }

        /// <summary>
        /// 将此变换与动画变换合并：平移/旋转相加，缩放相乘。
        /// </summary>
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
        /// 根据帧类型（平移、旋转或缩放）在两个变换之间插值。
        /// </summary>
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
