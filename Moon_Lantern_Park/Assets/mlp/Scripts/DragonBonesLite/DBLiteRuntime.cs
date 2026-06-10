// DragonBones 骨骼动画运行时 / 加载和播放 DragonBones 格式的骨骼动画，让角色能做出跑步、跳跃、投篮等各种动作。负责解析动画数据、管理骨骼层级、插值关键帧、显示插槽上的图片。游戏中所有角色动画都靠这个系统驱动。

using System;
using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 骨骼动画工厂（单例）：负责加载和缓存骨骼数据、纹理图集，管理所有骨骼动画的创建和资源复用。
    /// </summary>
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
            // 1. 如果该骨架数据已加载过，直接跳过（避免重复加载）
            if (skeletons.ContainsKey(skeletonKey))
            {
                return;
            }

            // 2. 从 Resources 文件夹加载骨架 JSON、纹理图集 JSON 和纹理图片三个资源
            var skeletonAsset = Resources.Load<TextAsset>($"mlp/DragonBones/{skeletonKey}");
            var textureJsonAsset = Resources.Load<TextAsset>($"mlp/DragonBones/{textureJsonKey}");
            var texture = Resources.Load<Texture2D>($"mlp/DragonBones/{textureImageKey}");
            if (skeletonAsset == null || textureJsonAsset == null || texture == null)
            {
                Debug.LogError($"Missing DragonBones set {skeletonKey}/{textureJsonKey}/{textureImageKey}");
                return;
            }

            // 3. 解析纹理图集 JSON，记录每张小图在大图中的位置和尺寸
            var textureAtlas = DBLiteTextureAtlas.Parse(textureJsonKey, texture, textureJsonAsset.text);
            textureAtlases[textureJsonKey] = textureAtlas;

            // 4. 解析骨架 JSON，提取所有骨架定义（骨骼、插槽、动画数据）
            var skeleton = DBLiteSkeleton.Parse(skeletonAsset.text, textureAtlas);
            skeletons[skeletonKey] = skeleton;
            // 5. 把所有骨架的名称和数据存入字典，方便按名称查找
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
            // 1. 确保骨架数据已加载
            EnsureLoaded();
            // 2. 按名称查找骨架数据，找不到则返回 null
            if (!armatures.TryGetValue(armatureName, out var data))
            {
                Debug.LogWarning($"Missing DragonBones armature {armatureName}");
                return null;
            }

            // 3. 创建新的游戏对象，添加骨骼动画组件并初始化
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

    /// <summary>
    /// 骨骼动画播放器（挂载在游戏对象上）：控制一个角色的骨骼动画播放，包括切换动画、暂停、设置速度、更新骨骼和插槽的变换。
    /// </summary>
    public sealed class DBLiteArmature : MonoBehaviour
    {
        private const float SlotDepthStep = 0.001f;
        private const string BallSlotName = "ball";
        private const string BallFrontSlotName = "ball_front";
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
        public float PlaybackSpeed { get; set; } = 1f;
        public event Action<string> AnimationComplete;
        public event Action<string, string> FrameEvent;

        /// <summary>
        /// 使用数据初始化骨架，构建骨骼层级，并播放第一个动画。
        /// </summary>
        public void Init(DBLiteFactory factory, DBLiteArmatureData data)
        {
            // 1. 保存工厂引用和骨架数据
            this.factory = factory;
            this.data = data;
            // 2. 根据数据创建骨骼层级和插槽显示对象
            BuildBonesAndSlots();
            // 3. 自动播放第一个动画，如果没有动画则显示第 0 帧静止姿态
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
            // 1. 没有骨架数据则无法播放
            if (data == null)
            {
                return;
            }

            // 2. 按名称查找动画数据，找不到则忽略
            if (!data.Animations.TryGetValue(animationName, out var animation))
            {
                return;
            }

            // 3. 如果是新动画或需要重新开始，重置计时器和完成标记
            if (currentAnimation != animation || restart)
            {
                currentAnimation = animation;
                elapsedFrames = 0f;
                animationCompleteSent = false;
            }

            // 4. 标记为播放状态并立即应用当前帧的姿态
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
            // 1. 没有正在播放的动画则跳过
            if (currentAnimation == null)
            {
                return;
            }

            // 2. 如果处于播放状态，根据时间流逝推进帧计数器
            if (playing)
            {
                var previousFrame = elapsedFrames;
                // 3. 帧增量 = 一帧的时间 x 帧率 x 播放速度
                elapsedFrames += Time.deltaTime * data.FrameRate * Mathf.Max(0f, PlaybackSpeed);
                // 4. 检查并触发帧事件（如音效、特效）
                DispatchFrameEvents(previousFrame, elapsedFrames);
                // 5. 检查非循环动画是否播放完毕
                TryDispatchAnimationComplete(previousFrame, elapsedFrames);
            }

            // 6. 将当前帧的姿态应用到所有骨骼和插槽上
            ApplyPose(elapsedFrames);
        }

        /// <summary>
        /// 触发前一帧和当前帧之间的所有帧事件。
        /// </summary>
        private void DispatchFrameEvents(float previousFrame, float currentFrame)
        {
            // 1. 没有动画或没有帧事件则跳过
            if (currentAnimation == null || currentAnimation.FrameEvents.Count == 0)
            {
                return;
            }

            var duration = Mathf.Max(1f, currentAnimation.Duration);
            if (currentAnimation.Loops)
            {
                // 2. 循环动画：判断上一帧和当前帧是否在同一圈内
                var previousLoop = Mathf.FloorToInt(previousFrame / duration);
                var currentLoop = Mathf.FloorToInt(currentFrame / duration);
                if (currentLoop == previousLoop)
                {
                    // 3. 同一圈内，直接检查这段区间内的事件
                    EmitFrameEventsInRange(Mod(previousFrame, duration), Mod(currentFrame, duration), duration);
                    return;
                }

                // 4. 跨圈了：先触发上一圈尾部的事件，再触发中间完整圈的所有事件，最后触发当前圈头部的事件
                EmitFrameEventsInRange(Mod(previousFrame, duration), duration, duration);
                for (var loop = previousLoop + 1; loop < currentLoop; loop++)
                {
                    EmitFrameEventsInRange(0f, duration, duration);
                }

                EmitFrameEventsInRange(0f, Mod(currentFrame, duration), duration);
                return;
            }

            // 5. 非循环动画：在有效范围内检查事件
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
            // 1. 第一遍遍历：为每根骨骼创建一个游戏对象，先全部挂在根节点下
            foreach (var boneData in data.Bones)
            {
                var boneGo = new GameObject(boneData.Name);
                boneGo.transform.SetParent(transform, false);
                bones[boneData.Name] = boneGo.transform;
            }

            // 2. 第二遍遍历：根据父子关系，把骨骼挂到正确的父骨骼下
            foreach (var boneData in data.Bones)
            {
                if (!string.IsNullOrEmpty(boneData.Parent) && bones.TryGetValue(boneData.Parent, out var parent))
                {
                    bones[boneData.Name].SetParent(parent, false);
                }
            }

            // 3. 第三遍遍历：为每个插槽创建显示对象，挂在对应的父骨骼下
            foreach (var slotData in data.Slots)
            {
                if (!bones.TryGetValue(slotData.Parent, out var parent))
                {
                    continue;
                }

                // 4. 创建插槽游戏对象，设置层级深度（Order 越大越靠前显示）
                var slotGo = new GameObject(slotData.Name);
                slotGo.transform.SetParent(parent, false);
                slotGo.transform.localPosition = new Vector3(0f, 0f, -slotData.Order * SlotDepthStep);
                // 5. 创建插槽实例，负责管理图片精灵或子骨架的显示
                var slot = new DBLiteSlotInstance(slotData, data.GetDisplays(slotData.Name), slotGo.transform, factory);
                slots[slotData.Name] = slot;
            }
        }

        /// <summary>
        /// 在指定帧采样当前动画，并将变换应用到所有骨骼和插槽。
        /// </summary>
        private void ApplyPose(float frame)
        {
            // 1. 没有骨架数据则跳过
            if (data == null)
            {
                return;
            }

            // 2. 根据动画是否循环，计算当前实际帧号
            var animFrame = 0f;
            var animationDuration = 1f;
            var animationLoops = false;
            if (currentAnimation != null)
            {
                animationDuration = Mathf.Max(1, currentAnimation.Duration);
                animationLoops = currentAnimation.Loops;
                // 循环动画用取模，非循环动画停在最后一帧之前
                animFrame = animationLoops ? frame % animationDuration : Mathf.Min(frame, animationDuration - 0.001f);
            }

            // 3. 遍历所有骨骼，计算并应用变换（位置、旋转、缩放）
            foreach (var boneData in data.Bones)
            {
                if (!bones.TryGetValue(boneData.Name, out var transform))
                {
                    continue;
                }

                // 4. 从初始姿态开始，叠加动画轨道的关键帧插值结果
                var pose = boneData.Transform;
                if (currentAnimation != null && currentAnimation.BoneTracks.TryGetValue(boneData.Name, out var track))
                {
                    pose = pose.Combine(track.Sample(animFrame, animationDuration, animationLoops));
                }

                // 5. 球体骨骼在特定动画中保持等比缩放（避免被压扁变形）
                if (IsBallBone(boneData.Name) && ShouldKeepBallRound(currentAnimation))
                {
                    pose = KeepUniformScale(pose);
                }

                // 6. 应用最终的位置（Y 轴翻转，对齐到像素网格）、旋转和缩放
                transform.localPosition = mlpConstants.SnapLocalPositionToScreenPixels(
                    transform.parent,
                    new Vector3(pose.X, -pose.Y, 0f));
                transform.localRotation = Quaternion.Euler(0f, 0f, -pose.Rotation);
                transform.localScale = new Vector3(pose.ScaleX, pose.ScaleY, 1f);
            }

            // 7. 遍历所有插槽，重置为初始显示，再根据动画轨道切换图片和透明度
            foreach (var pair in slots)
            {
                pair.Value.ResetToSetupPose();
                if (currentAnimation != null && currentAnimation.SlotTracks.TryGetValue(pair.Key, out var slotTrack))
                {
                    // 8. 根据动画帧切换插槽显示的内容（如不同身体部件）
                    var displayIndex = slotTrack.SampleDisplay(animFrame);
                    if (displayIndex != int.MinValue)
                    {
                        pair.Value.SetDisplay(displayIndex);
                    }

                    // 9. 根据动画帧设置插槽的透明度（实现淡入淡出效果）
                    pair.Value.SetAlpha(slotTrack.SampleAlpha(animFrame));
                }

                // 10. 如果插槽被手动隐藏，强制设为不可见
                if (hiddenSlots.Contains(pair.Key))
                {
                    pair.Value.SetAlpha(0f);
                }
            }
        }

        private static bool IsBallBone(string boneName)
        {
            return boneName == BallSlotName || boneName == BallFrontSlotName;
        }

        private static bool ShouldKeepBallRound(DBLiteAnimationData animation)
        {
            if (animation == null || string.IsNullOrEmpty(animation.Name))
            {
                return false;
            }

            var name = animation.Name;
            return name == "jump_wb" ||
                   name == "landing_wb" ||
                   (name.StartsWith("fly", StringComparison.Ordinal) && name.EndsWith("_wb", StringComparison.Ordinal)) ||
                   name.StartsWith("dunk", StringComparison.Ordinal) ||
                   name.StartsWith("megadunk", StringComparison.Ordinal) ||
                   (name.StartsWith("md_", StringComparison.Ordinal) && name.EndsWith("_wb", StringComparison.Ordinal));
        }

        private static DBLiteTransform KeepUniformScale(DBLiteTransform pose)
        {
            var uniformScale = Mathf.Max(0.0001f, (Mathf.Abs(pose.ScaleX) + Mathf.Abs(pose.ScaleY)) * 0.5f);
            pose.ScaleX = Mathf.Sign(pose.ScaleX == 0f ? 1f : pose.ScaleX) * uniformScale;
            pose.ScaleY = Mathf.Sign(pose.ScaleY == 0f ? 1f : pose.ScaleY) * uniformScale;
            return pose;
        }
    }

    /// <summary>
    /// 纹理图集：存储一张大图中所有小图的位置和尺寸信息，用于从图集中裁剪出各个身体部件的图片。
    /// </summary>
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
            // 1. 解析 JSON 字符串为字典，读取像素单位比例
            var root = mlpJson.AsDict(mlpJson.Parse(json));
            var atlas = new DBLiteTextureAtlas(texture, mlpJson.Float(root, "pixelsPerUnit", 1f));
            // 2. 读取 SubTexture 列表，每项记录了大图中某张小图的位置和尺寸
            var list = mlpJson.List(root, "SubTexture");
            if (list == null)
            {
                return atlas;
            }

            // 3. 遍历每张子纹理，记录名称、坐标和大小
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
            // 1. 名称为空则返回 null
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            // 2. 如果之前已经创建过这个精灵，直接从缓存返回（避免重复创建）
            if (sprites.TryGetValue(name, out var cached))
            {
                return cached;
            }

            // 3. 在子纹理字典中查找，找不到则返回 null
            if (!subTextures.TryGetValue(name, out var sub))
            {
                return null;
            }

            // 4. 从大图中裁剪出该子纹理对应的矩形区域（Y 轴需要翻转，因为图片坐标和 Unity 坐标方向相反）
            var rect = new Rect(sub.X, texture.height - sub.Y - sub.Height, sub.Width, sub.Height);
            // 5. 创建 Unity Sprite 对象，锚点在中心
            var sprite = UnityEngine.Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            // 6. 存入缓存后返回
            sprites[name] = sprite;
            return sprite;
        }
    }

    /// <summary>
    /// 骨骼骨架数据：存储一个角色的完整骨骼结构，包括所有骨骼、插槽、图集引用和动画数据。
    /// </summary>
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

    /// <summary>
    /// 骨架数据：存储一个骨架的所有骨骼层级、插槽列表和动画集合，是动画播放的核心数据结构。
    /// </summary>
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
            // 1. 创建骨架数据对象，记录名称、帧率和纹理图集引用
            var data = new DBLiteArmatureData
            {
                Name = mlpJson.String(dict, "name"),
                FrameRate = frameRate,
                TextureAtlas = atlas
            };

            // 2. 解析骨骼列表：每根骨骼有名称、父骨骼和初始变换
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

            // 3. 解析插槽列表：每个插槽关联一根骨骼，决定图片的显示顺序
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

            // 4. 解析皮肤（每个插槽可显示哪些图片或子骨架）
            ParseSkin(dict, data);
            // 5. 解析动画定义（骨骼轨道、插槽轨道、帧事件）
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

    /// <summary>
    /// 动画数据：存储一段动画的所有关键帧轨道（骨骼轨道和插槽轨道）、持续时间和帧事件。
    /// </summary>
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
            // 1. 读取动画名称、持续帧数和是否循环（playTimes 为 0 表示无限循环）
            var animation = new DBLiteAnimationData
            {
                Name = mlpJson.String(dict, "name"),
                Duration = Mathf.Max(1, mlpJson.Int(dict, "duration", 1)),
                Loops = mlpJson.Int(dict, "playTimes", 1) == 0
            };

            // 2. 解析每根骨骼的动画轨道（包含位置、旋转、缩放的关键帧序列）
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

            // 3. 解析每个插槽的动画轨道（包含显示切换和透明度的关键帧序列）
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

            // 4. 解析帧事件列表（在动画特定时间点触发的事件，如音效或特效）
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

                    // 累加每帧的持续时间，得到事件触发的绝对帧号
                    start += Mathf.Max(1, mlpJson.Int(frame, "duration", 1));
                }
            }

            return animation;
        }
    }

    /// <summary>
    /// 动画帧事件：在动画的特定时间点触发的事件，包含事件名称（如播放音效、触发特效）。
    /// </summary>
    public struct DBLiteAnimationFrameEvent
    {
        public float Frame;
        public string EventName;
    }

    /// <summary>
    /// 骨骼动画轨道：存储一根骨骼在一段动画中的所有位置/旋转关键帧，用于播放时插值计算骨骼姿态。
    /// </summary>
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
        public DBLiteTransform Sample(float frame, float animationDuration, bool loop)
        {
            // 1. 以单位变换为基础，分别对平移、旋转、缩放三组关键帧进行插值采样
            var result = DBLiteTransform.Identity;
            var translation = SampleList(translate, frame, FrameKind.Translate, animationDuration, loop);
            var rotation = SampleList(rotate, frame, FrameKind.Rotate, animationDuration, loop);
            var scaling = SampleList(scale, frame, FrameKind.Scale, animationDuration, loop);

            // 2. 将三组插值结果合并为一个完整的变换
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
            // 1. 没有关键帧列表则跳过
            if (list == null)
            {
                return;
            }

            // 2. 遍历每个关键帧，记录起始时间、持续时间和变换值
            var start = 0f;
            foreach (var item in list)
            {
                var dict = mlpJson.AsDict(item);
                if (dict == null)
                {
                    continue;
                }

                // 3. 读取变换值，然后根据类型（平移/旋转/缩放）只保留对应字段，其余重置
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

                // 4. 记录关键帧的起始时间、持续时间和是否使用补间（平滑过渡）
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
                // 5. 累加时间，下一个关键帧的起始时间 = 当前起始 + 当前持续
                start += duration;
            }
        }

        /// <summary>
        /// 查找包围指定帧的关键帧并进行变换插值。
        /// </summary>
        private static DBLiteTransform SampleList(List<DBLiteTimedTransform> list, float frame, FrameKind kind, float animationDuration, bool loop)
        {
            // 1. 没有关键帧则返回默认姿态
            if (list.Count == 0)
            {
                return DBLiteTransform.Identity;
            }

            // 2. 找到当前帧所在的区间：current 是当前关键帧，next 是下一个关键帧
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

            // 3. 循环动画：如果当前帧已超过最后一个关键帧，下一个关键帧就是第一个（首尾衔接）
            if (!next.HasValue && loop && list.Count > 1)
            {
                next = new DBLiteTimedTransform
                {
                    Start = Mathf.Max(animationDuration, current.Start + current.Duration),
                    Duration = list[0].Duration,
                    Transform = list[0].Transform,
                    Tween = list[0].Tween
                };
            }

            // 4. 如果当前关键帧不使用补间、没有下一个关键帧、或持续时间为 0，直接返回当前值（不插值）
            if (!current.Tween || !next.HasValue || current.Duration <= 0f)
            {
                return current.Transform;
            }

            // 5. 计算当前帧在区间内的归一化进度（0 到 1 之间），然后在两个关键帧之间插值
            var segmentDuration = current.Duration;
            if (loop)
            {
                segmentDuration = Mathf.Max(0.0001f, Mathf.Min(segmentDuration, animationDuration - current.Start));
            }
            var t = Mathf.Clamp01((frame - current.Start) / segmentDuration);
            return DBLiteTransform.Lerp(current.Transform, next.Value.Transform, t, kind);
        }
    }

    /// <summary>
    /// 插槽动画轨道：存储一个插槽在一段动画中的显示切换和颜色变化关键帧，控制角色部件的显示顺序和透明度。
    /// </summary>
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

    /// <summary>
    /// 插槽实例：代表角色身上的一个可显示部件（如手臂、武器），管理其图片精灵、颜色和显示顺序。
    /// </summary>
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
            // 1. 保存插槽配置数据、可显示元素列表、挂载点变换和工厂引用
            this.slotData = slotData;
            this.displays = displays ?? new List<DBLiteDisplayData>();
            this.slotTransform = slotTransform;
            this.factory = factory;
            // 2. 根据初始显示索引，立即显示默认的内容（图片或子骨架）
            SetDisplay(slotData.DisplayIndex);
        }

        /// <summary>
        /// 切换插槽以通过索引显示不同的内容（图片或子骨架）。
        /// </summary>
        public void SetDisplay(int index)
        {
            // 1. 如果新索引和当前一样，不需要切换
            if (index == currentDisplay)
            {
                return;
            }

            // 2. 记录新索引，销毁旧的显示对象（编辑器下用 DestroyImmediate，运行时用 Destroy）
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

            // 3. 索引越界则不显示任何内容
            if (index < 0 || index >= displays.Count)
            {
                return;
            }

            // 4. 根据显示类型创建子骨架或图片精灵
            var display = displays[index];
            if (display.Type == "armature")
            {
                // 5. 类型为子骨架：递归构建一个完整的子骨骼动画
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
                // 6. 类型为图片：创建精灵渲染器，从纹理图集中查找对应的图片
                var go = new GameObject($"{slotData.Name}:{display.Name}");
                go.transform.SetParent(slotTransform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = FindTextureAtlasSprite(display);
                renderer.sortingOrder = 20 + slotData.Order;
                currentDisplayObject = go;
                ApplyDisplayTransform(go.transform, display.Transform);
            }

            // 7. 保持当前的透明度设置
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
            // 1. 特殊处理球体剪辑插槽：使用当前比赛的球体皮肤精灵
            if (display != null && display.Name == ".Game/ball/BallClip")
            {
                return mlpGameplaySpriteLoader.LoadMatchBallSprite(
                    mlpInventory.Instance.MatchData.BallTheme,
                    0.5f,
                    0.5f);
            }

            // 2. 普通图片：通过反射获取骨架数据，再从纹理图集中查找对应精灵
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
            // 1. 没有显示对象则跳过
            if (currentDisplayObject == null)
            {
                return;
            }

            // 2. 如果是子骨架，通过激活/禁用整个对象来控制可见性
            if (ChildArmature != null)
            {
                currentDisplayObject.SetActive(alpha > 0.001f);
                return;
            }

            // 3. 如果是图片精灵，修改每个 SpriteRenderer 的颜色 alpha 值和启用状态
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
                // 4. alpha 极小时直接禁用渲染器（避免绘制不可见的物体浪费性能）
                renderer.enabled = alpha > 0.001f;
            }
        }
    }

    /// <summary>骨骼数据：存储一根骨骼的名称、父骨骼名称和初始变换（位置、旋转）。</summary>
    public sealed class DBLiteBoneData
    {
        public string Name;
        public string Parent;
        public DBLiteTransform Transform;
    }

    /// <summary>插槽数据：存储一个插槽的名称、父骨骼名称、当前显示索引和渲染顺序。</summary>
    public sealed class DBLiteSlotData
    {
        public string Name;
        public string Parent;
        public int DisplayIndex;
        public int Order;
    }

    /// <summary>显示数据：存储插槽中一个可显示元素的名称、类型和初始变换。</summary>
    public sealed class DBLiteDisplayData
    {
        public string Name;
        public string Type;
        public DBLiteTransform Transform;
    }

    /// <summary>变换数据：存储位置（X/Y）和旋转角度，用于骨骼和插槽的空间变换计算。</summary>
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

    /// <summary>带时间的变换关键帧：记录某个时间点的变换值和是否使用补间动画。</summary>
    public struct DBLiteTimedTransform
    {
        public float Start;
        public float Duration;
        public bool Tween;
        public DBLiteTransform Transform;
    }

    /// <summary>显示切换关键帧：记录在什么时间点切换到哪个显示元素。</summary>
    public struct DBLiteDisplayFrame
    {
        public float Start;
        public float Duration;
        public int Value;
    }

    /// <summary>颜色关键帧：记录在什么时间点的透明度变化，用于淡入淡出效果。</summary>
    public struct DBLiteColorFrame
    {
        public float Start;
        public float Duration;
        public float Alpha;
    }

    /// <summary>子纹理数据：记录图集中一张小图的名称、位置（X/Y）和尺寸（宽/高）。</summary>
    public struct DBLiteSubTexture
    {
        public string Name;
        public float X;
        public float Y;
        public float Width;
        public float Height;
    }

    /// <summary>关键帧类型：标识动画帧是骨骼变换帧、插槽显示帧还是颜色帧。</summary>
    public enum FrameKind
    {
        Translate,
        Rotate,
        Scale
    }
}
