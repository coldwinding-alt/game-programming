// DragonBones skeletal animation runtime/loads and plays skeletal animation in DragonBones format, allowing the character to perform various actions such as running, jumping, shooting, etc. Responsible for parsing animation data, managing bone levels, interpolating key frames, and displaying images on slots. All character animations in the game are driven by this system.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Skeleton animation factory (single case): Responsible for loading and caching skeletal data and texture atlases, managing the creation and resource reuse of all skeletal animations.
    /// </summary>
    public sealed class DBLiteFactory
    {
        private static DBLiteFactory instance; // Singleton instance


        private readonly Dictionary<string, DBLiteSkeleton> skeletons = new Dictionary<string, DBLiteSkeleton>(); // Loaded skeleton data cache, key is skeleton name

        private readonly Dictionary<string, DBLiteTextureAtlas> textureAtlases = new Dictionary<string, DBLiteTextureAtlas>(); // Loaded texture atlas cache, key is the atlas name

        private readonly Dictionary<string, DBLiteArmatureData> armatures = new Dictionary<string, DBLiteArmatureData>(); // Loaded skeleton definition cache, key is skeleton name


        public static DBLiteFactory Instance => instance ?? (instance = new DBLiteFactory());

        /// <summary>
        /// Loads the default DragonBones skeleton and textures if not already loaded.

        /// </summary>
        public void EnsureLoaded()
        {
            Load("sk2", "texture2", "texture2");
        }

        /// <summary>
        /// Load the DragonBones skeleton, texture atlas JSON, and texture image from Resources.
        /// </summary>
        public void Load(string skeletonKey, string textureJsonKey, string textureImageKey)
        {
            // 1. If the skeleton data has already been loaded, skip it directly (to avoid repeated loading)

            if (skeletons.ContainsKey(skeletonKey))
            {
                return;
            }

            // 2. Load three resources: skeleton JSON, texture atlas JSON and texture pictures from the Resources folder

            var skeletonAsset = Resources.Load<TextAsset>($"mlp/DragonBones/{skeletonKey}");
            var textureJsonAsset = Resources.Load<TextAsset>($"mlp/DragonBones/{textureJsonKey}");
            var texture = Resources.Load<Texture2D>($"mlp/DragonBones/{textureImageKey}");
            if (skeletonAsset == null || textureJsonAsset == null || texture == null)
            {
                Debug.LogError($"Missing DragonBones set {skeletonKey}/{textureJsonKey}/{textureImageKey}");
                return;
            }

            // 3. Parse the texture atlas JSON and record the position and size of each small image in the large image.

            var textureAtlas = DBLiteTextureAtlas.Parse(textureJsonKey, texture, textureJsonAsset.text);
            textureAtlases[textureJsonKey] = textureAtlas;

            // 4. Parse the skeleton JSON and extract all skeleton definitions (bones, slots, animation data)

            var skeleton = DBLiteSkeleton.Parse(skeletonAsset.text, textureAtlas);
            skeletons[skeletonKey] = skeleton;
            // 5. Store the names and data of all skeletons in the dictionary to facilitate searching by name

            foreach (var pair in skeleton.Armatures)
            {
                armatures[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// Creates a new GameObject with a DBLiteArmature component for the specified named skeleton.

        /// </summary>
        public DBLiteArmature BuildArmature(string armatureName, string objectName = null)
        {
            // 1. Make sure the skeleton data is loaded

            EnsureLoaded();
            // 2. Search skeleton data by name, if not found, return null

            if (!armatures.TryGetValue(armatureName, out var data))
            {
                Debug.LogWarning($"Missing DragonBones armature {armatureName}");
                return null;
            }

            // 3. Create a new game object, add skeletal animation components and initialize

            var go = new GameObject(objectName ?? armatureName);
            var armature = go.AddComponent<DBLiteArmature>();
            armature.Init(this, data);
            return armature;
        }

        /// <summary>
        /// Try to find a loaded skeleton definition by name.

        /// </summary>
        public bool TryGetArmature(string armatureName, out DBLiteArmatureData data)
        {
            EnsureLoaded();
            return armatures.TryGetValue(armatureName, out data);
        }

        /// <summary>
        /// Returns the sprite from the texture atlas by name, or null if not found.

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
    /// Skeletal animation player (mounted on the game object): Controls the skeletal animation playback of a character, including switching animations, pausing, setting speed, updating bones and slot transformations.

    /// </summary>
    public sealed class DBLiteArmature : MonoBehaviour
    {
        private const float SlotDepthStep = 0.001f; // The Z-axis depth step between slots. The smaller the value, the closer the slots are.

        private const string BallSlotName = "ball"; // Sphere bone name

        private const string BallFrontSlotName = "ball_front"; // Sphere front layer bone name

        private DBLiteFactory factory; // Skeleton animation factory reference, used to create sub-skeletons and obtain textures

        private DBLiteArmatureData data; // Definition data of the current skeleton, including bone, slot and animation information

        private readonly Dictionary<string, DBLiteSlotInstance> slots = new Dictionary<string, DBLiteSlotInstance>(); // A dictionary of slot instances, with keys being slot names, managing the display of each body part

        private readonly HashSet<string> hiddenSlots = new HashSet<string>(); // A collection of manually hidden slot names

        private readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>(); // Bone transformation dictionary, the key is the bone name, used to control the rotation and scaling of bone position

        private DBLiteAnimationData currentAnimation; // Animation data currently being played

        private float elapsedFrames; // The cumulative number of frames that the current animation has been played

        private bool animationCompleteSent; // Whether the animation completion event has been sent to prevent repeated sending

        private bool playing = true; // Whether animation is playing

        public string ArmatureName => data != null ? data.Name : string.Empty; // The name of the current skeleton

        public float PlaybackSpeed { get; set; } = 1f; // Animation playback speed multiplier, 1 is normal speed

        public event Action<string> AnimationComplete; // Animation playback completion event, the parameter is the animation name

        public event Action<string, string> FrameEvent; // Frame event callback, the parameters are animation name and event name


        /// <summary>
        /// Initialize the skeleton with data, build the bone hierarchy, and play the first animation.

        /// </summary>
        public void Init(DBLiteFactory factory, DBLiteArmatureData data)
        {
            // 1. Save factory reference and skeleton data

            this.factory = factory;
            this.data = data;
            // 2. Create bone hierarchy and slot display objects based on data

            BuildBonesAndSlots();
            // 3. Automatically play the first animation. If there is no animation, the static posture of frame 0 will be displayed.

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
        /// Returns the child skeleton shown in the specified slot, if any.

        /// </summary>
        public DBLiteArmature GetChildArmature(string slotName)
        {
            return slots.TryGetValue(slotName, out var slot) ? slot.ChildArmature : null;
        }

        /// <summary>
        /// Show or hide specified slots based on their names.

        /// </summary>
        public void SetSlotHidden(string slotName, bool hidden)
        {
            // 1. Skip when the slot name is empty

            if (string.IsNullOrEmpty(slotName))
            {
                return;
            }

            // 2. Add or remove slot names to or from the hidden list

            if (hidden)
            {
                hiddenSlots.Add(slotName);
            }
            else
            {
                hiddenSlots.Remove(slotName);
            }

            // 3. If the slot has not been created yet, just record the status (it will be checked when creating)

            if (!slots.TryGetValue(slotName, out var slot))
            {
                return;
            }

            // 4. Set the transparency to 0 directly when hiding, and refresh the entire skeleton posture when unhiding.

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
        /// Plays the animation with the specified name from the beginning.

        /// </summary>
        public void Play(string animationName, bool restart = true)
        {
            // 1. Unable to play without skeleton data

            if (data == null)
            {
                return;
            }

            // 2. Search animation data by name, ignore if not found

            if (!data.Animations.TryGetValue(animationName, out var animation))
            {
                return;
            }

            // 3. If it is a new animation or needs to be restarted, reset the timer and completion mark

            if (currentAnimation != animation || restart)
            {
                currentAnimation = animation;
                elapsedFrames = 0f;
                animationCompleteSent = false;
            }

            // 4. Mark as playing state and immediately apply the pose of the current frame

            playing = true;
            ApplyPose(elapsedFrames);
        }

        /// <summary>
        /// The animation loads but freezes at frame 0 and does not play.

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
        /// Reapply the current animation pose (called after changing slots or visibility).

        /// </summary>
        public void RefreshPose()
        {
            ApplyPose(elapsedFrames);
        }

        /// <summary>
        /// Each frame advances the animation timer and applies the current pose.

        /// </summary>
        private void Update()
        {
            // 1. Skip if there is no animation currently playing.

            if (currentAnimation == null)
            {
                return;
            }

            // 2. If playing, advance the frame counter based on the passage of time
            if (playing)
            {
                var previousFrame = elapsedFrames;
                // 3. Frame increment = one frame time x frame rate x playback speed

                elapsedFrames += Time.deltaTime * data.FrameRate * Mathf.Max(0f, PlaybackSpeed);
                // 4. Check and trigger frame events (such as sound effects, special effects)

                DispatchFrameEvents(previousFrame, elapsedFrames);
                // 5. Check whether the non-loop animation has finished playing

                TryDispatchAnimationComplete(previousFrame, elapsedFrames);
            }

            // 6. Apply the pose of the current frame to all bones and slots

            ApplyPose(elapsedFrames);
        }

        /// <summary>
        /// Triggers all frame events between the previous frame and the current frame.

        /// </summary>
        private void DispatchFrameEvents(float previousFrame, float currentFrame)
        {
            // 1. Skip if there is no animation or frame event

            if (currentAnimation == null || currentAnimation.FrameEvents.Count == 0)
            {
                return;
            }

            var duration = Mathf.Max(1f, currentAnimation.Duration);
            if (currentAnimation.Loops)
            {
                // 2. Loop animation: determine whether the previous frame and the current frame are in the same circle

                var previousLoop = Mathf.FloorToInt(previousFrame / duration);
                var currentLoop = Mathf.FloorToInt(currentFrame / duration);
                if (currentLoop == previousLoop)
                {
                    // 3. In the same circle, directly check the events in this interval

                    EmitFrameEventsInRange(Mod(previousFrame, duration), Mod(currentFrame, duration), duration);
                    return;
                }

                // 4. Crossed the circle: first trigger the event at the end of the previous circle, then trigger all events of the complete circle in the middle, and finally trigger the event at the head of the current circle.

                EmitFrameEventsInRange(Mod(previousFrame, duration), duration, duration);
                for (var loop = previousLoop + 1; loop < currentLoop; loop++)
                {
                    EmitFrameEventsInRange(0f, duration, duration);
                }

                EmitFrameEventsInRange(0f, Mod(currentFrame, duration), duration);
                return;
            }

            // 5. Acyclic animation: check events within the valid range

            var start = Mathf.Clamp(previousFrame, 0f, duration);
            var end = Mathf.Clamp(currentFrame, 0f, duration);
            EmitFrameEventsInRange(start, end, duration);
        }

        /// <summary>
        /// The AnimationComplete event is triggered when the non-looping animation reaches the end.

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
        /// Triggers frame events with timestamps within the specified range.

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
        /// Returns the positive modulo result of the divisor.

        /// </summary>
        private static float Mod(float value, float divisor)
        {
            return (value % divisor + divisor) % divisor;
        }

        /// <summary>
        /// Create bone transform hierarchy and slot display objects based on skeleton data.

        /// </summary>
        private void BuildBonesAndSlots()
        {
            // 1. First pass of traversal: create a game object for each bone, first hang them all under the root node

            foreach (var boneData in data.Bones)
            {
                var boneGo = new GameObject(boneData.Name);
                boneGo.transform.SetParent(transform, false);
                bones[boneData.Name] = boneGo.transform;
            }

            // 2. Second traversal: According to the parent-child relationship, hang the bones under the correct parent bones

            foreach (var boneData in data.Bones)
            {
                if (!string.IsNullOrEmpty(boneData.Parent) && bones.TryGetValue(boneData.Parent, out var parent))
                {
                    bones[boneData.Name].SetParent(parent, false);
                }
            }

            // 3. The third traversal: Create a display object for each slot and hang it under the corresponding parent bone.

            foreach (var slotData in data.Slots)
            {
                if (!bones.TryGetValue(slotData.Parent, out var parent))
                {
                    continue;
                }

                // 4. Create a slot game object and set the hierarchy depth (the larger the Order, the higher it will be displayed)

                var slotGo = new GameObject(slotData.Name);
                slotGo.transform.SetParent(parent, false);
                slotGo.transform.localPosition = new Vector3(0f, 0f, -slotData.Order * SlotDepthStep);
                // 5. Create a slot instance to manage the display of picture sprites or sub-skeletons

                var slot = new DBLiteSlotInstance(slotData, data.GetDisplays(slotData.Name), slotGo.transform, factory);
                slots[slotData.Name] = slot;
            }
        }

        /// <summary>
        /// Samples the current animation at the specified frame and applies the transform to all bones and slots.

        /// </summary>
        private void ApplyPose(float frame)
        {
            // 1. Skip if there is no skeleton data.

            if (data == null)
            {
                return;
            }

            // 2. Calculate the current actual frame number based on whether the animation loops

            var animFrame = 0f;
            var animationDuration = 1f;
            var animationLoops = false;
            if (currentAnimation != null)
            {
                animationDuration = Mathf.Max(1, currentAnimation.Duration);
                animationLoops = currentAnimation.Loops;
                // Looping animations use modulo, and non-looping animations stop before the last frame.

                animFrame = animationLoops ? frame % animationDuration : Mathf.Min(frame, animationDuration - 0.001f);
            }

            // 3. Traverse all bones, calculate and apply transformations (position, rotation, scale)
            foreach (var boneData in data.Bones)
            {
                if (!bones.TryGetValue(boneData.Name, out var transform))
                {
                    continue;
                }

                // 4. Starting from the initial pose, superimpose the keyframe interpolation results of the animation track

                var pose = boneData.Transform;
                if (currentAnimation != null && currentAnimation.BoneTracks.TryGetValue(boneData.Name, out var track))
                {
                    pose = pose.Combine(track.Sample(animFrame, animationDuration, animationLoops));
                }

                // 5. Sphere bones maintain proportional scaling in specific animations (to avoid being squashed and deformed)

                if (IsBallBone(boneData.Name) && ShouldKeepBallRound(currentAnimation))
                {
                    pose = KeepUniformScale(pose);
                }

                // 6. Apply final position (Y-axis flip, snap to pixel grid), rotation and scale

                transform.localPosition = mlpConstants.SnapLocalPositionToScreenPixels(
                    transform.parent,
                    new Vector3(pose.X, -pose.Y, 0f));
                transform.localRotation = Quaternion.Euler(0f, 0f, -pose.Rotation);
                transform.localScale = new Vector3(pose.ScaleX, pose.ScaleY, 1f);
            }

            // 7. Traverse all slots, reset to initial display, and then switch pictures and transparency according to the animation track

            foreach (var pair in slots)
            {
                pair.Value.ResetToSetupPose();
                if (currentAnimation != null && currentAnimation.SlotTracks.TryGetValue(pair.Key, out var slotTrack))
                {
                    // 8. Switch the content displayed by the slot according to the animation frame (such as different body parts)

                    var displayIndex = slotTrack.SampleDisplay(animFrame);
                    if (displayIndex != int.MinValue)
                    {
                        pair.Value.SetDisplay(displayIndex);
                    }

                    // 9. Set the transparency of the slot according to the animation frame (to achieve the fade-in and fade-out effect)

                    pair.Value.SetAlpha(slotTrack.SampleAlpha(animFrame));
                }

                // 10. If the slot is manually hidden, force it to be invisible

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
            // 1. There is no need to keep the sphere round when there is no animation data.

            if (animation == null || string.IsNullOrEmpty(animation.Name))
            {
                return false;
            }

            // 2. During jumping, landing, flying, dunking and super dunk animations, the sphere needs to maintain proportional scaling (not be squashed)

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
    /// Texture atlas: stores the position and size information of all small images in a large image, which is used to cut out images of various body parts from the atlas.

    /// </summary>
    public sealed class DBLiteTextureAtlas
    {
        private readonly Texture2D texture; // Large image textures from texture atlas

        private readonly float pixelsPerUnit; // Number of pixels per unit, controls the display size of sprites in world space

        private readonly Dictionary<string, DBLiteSubTexture> subTextures = new Dictionary<string, DBLiteSubTexture>(); // Sub-texture dictionary, the key is the name, recording the position and size of each small image in the large image

        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>(); // The created sprite cache, the key is the name, to avoid repeated cropping


        /// <summary>
        /// Creates a texture atlas using the given texture and pixel-per-unit scale.

        /// </summary>
        private DBLiteTextureAtlas(Texture2D texture, float pixelsPerUnit)
        {
            this.texture = texture;
            this.pixelsPerUnit = Mathf.Max(0.0001f, pixelsPerUnit);
        }

        /// <summary>
        /// Parse DragonBones texture atlas from JSON definitions and textures.

        /// </summary>
        public static DBLiteTextureAtlas Parse(string name, Texture2D texture, string json)
        {
            // 1. Parse the JSON string into a dictionary and read the pixel unit ratio

            var root = mlpJson.AsDict(mlpJson.Parse(json));
            var atlas = new DBLiteTextureAtlas(texture, mlpJson.Float(root, "pixelsPerUnit", 1f));
            // 2. Read the SubTexture list, each item records the position and size of a small picture in the big picture

            var list = mlpJson.List(root, "SubTexture");
            if (list == null)
            {
                return atlas;
            }

            // 3. Traverse each sub-texture and record the name, coordinates and size

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
        /// Returns the cached sprite for the specified subtexture, or null if not found.

        /// </summary>
        public Sprite Sprite(string name)
        {
            // 1. If the name is empty, null is returned.

            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            // 2. If this elf has been created before, return it directly from the cache (to avoid repeated creation)

            if (sprites.TryGetValue(name, out var cached))
            {
                return cached;
            }

            // 3. Search in the sub-texture dictionary, return null if not found

            if (!subTextures.TryGetValue(name, out var sub))
            {
                return null;
            }

            // 4. Cut out the rectangular area corresponding to the sub-texture from the large image (the Y axis needs to be flipped because the image coordinates and Unity coordinates are in opposite directions)
            var rect = new Rect(sub.X, texture.height - sub.Y - sub.Height, sub.Width, sub.Height);
            // 5. Create a Unity Sprite object with the anchor point in the center

            var sprite = UnityEngine.Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            // 6. Return after storing in cache

            sprites[name] = sprite;
            return sprite;
        }
    }

    /// <summary>
    /// Skeletal skeleton data: Stores the complete skeletal structure of a character, including all bones, slots, atlas references, and animation data.

    /// </summary>
    public sealed class DBLiteSkeleton
    {
        public readonly Dictionary<string, DBLiteArmatureData> Armatures = new Dictionary<string, DBLiteArmatureData>(); // Skeleton definition dictionary, the key is the skeleton name, one skeleton file can contain multiple skeletons


        /// <summary>
        /// Parse the DragonBones skeleton file and extract all skeleton definitions.

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
    /// Skeleton data: stores all bone levels, slot lists and animation collections of a skeleton. It is the core data structure for animation playback.

    /// </summary>
    public sealed class DBLiteArmatureData
    {
        public string Name; // Skeleton name

        public int FrameRate; // Animation frame rate (frames per second)

        public readonly List<DBLiteBoneData> Bones = new List<DBLiteBoneData>(); // Bone list, defining the hierarchical structure of the skeleton

        public readonly List<DBLiteSlotData> Slots = new List<DBLiteSlotData>(); // Slot list, each slot is hung on a bone and used to display images

        public readonly Dictionary<string, List<DBLiteDisplayData>> DisplaysBySlot = new Dictionary<string, List<DBLiteDisplayData>>(); // List of images/sub-skeletons that can be displayed for each slot, the key is the slot name

        public readonly Dictionary<string, DBLiteAnimationData> Animations = new Dictionary<string, DBLiteAnimationData>(); // Animation dictionary, the key is the animation name

        public DBLiteTextureAtlas TextureAtlas; // Texture atlas reference used by this skeleton

        public string FirstAnimationName; // The first animation name of the skeleton, used to play automatically during initialization


        /// <summary>
        /// Returns a list of displayed entries for the specified slot, or null if none exists.

        /// </summary>
        public List<DBLiteDisplayData> GetDisplays(string slotName)
        {
            return DisplaysBySlot.TryGetValue(slotName, out var displays) ? displays : null;
        }

        /// <summary>
        /// Parse skeleton definitions from JSON, including bones, slots, skins, and animations.

        /// </summary>
        public static DBLiteArmatureData Parse(Dictionary<string, object> dict, DBLiteTextureAtlas atlas, int frameRate)
        {
            // 1. Create a skeleton data object and record the name, frame rate and texture atlas reference

            var data = new DBLiteArmatureData
            {
                Name = mlpJson.String(dict, "name"),
                FrameRate = frameRate,
                TextureAtlas = atlas
            };

            // 2. Parse the bone list: each bone has a name, parent bone and initial transformation

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

            // 3. Parse the slot list: Each slot is associated with a bone, which determines the display order of the images.

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

            // 4. Analyze skins (which pictures or sub-skeletons can be displayed in each slot)

            ParseSkin(dict, data);
            // 5. Analyze animation definition (bone track, slot track, frame event)

            ParseAnimations(dict, data);
            return data;
        }

        /// <summary>
        /// Parses the skin definition to populate the display data for each slot.

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
        /// Parse all animation definitions and save the name of the first animation.

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
    /// Animation data: Stores all keyframe tracks (bone tracks and slot tracks), durations, and frame events for an animation.
    /// </summary>
    public sealed class DBLiteAnimationData
    {
        public string Name; // Animation name

        public int Duration = 1; // The total number of animation frames (in frames, not seconds)

        public bool Loops; // Whether to loop playback (true means loop, false means stop after playing once)

        public readonly Dictionary<string, DBLiteBoneTrack> BoneTracks = new Dictionary<string, DBLiteBoneTrack>(); // Bone orbit dictionary, the key is the bone name, controls the displacement animation of each bone

        public readonly Dictionary<string, DBLiteSlotTrack> SlotTracks = new Dictionary<string, DBLiteSlotTrack>(); // Slot track dictionary, the key is the slot name, controls the display switching and transparency of the slot

        public readonly List<DBLiteAnimationFrameEvent> FrameEvents = new List<DBLiteAnimationFrameEvent>(); // Frame event list, triggering sound effects or special effects at specific frames


        /// <summary>
        /// Parse animation definitions containing bone tracks, slot tracks, and frame events.

        /// </summary>
        public static DBLiteAnimationData Parse(Dictionary<string, object> dict)
        {
            // 1. Read the animation name, duration of frames and whether to loop (playTimes of 0 means infinite loop)

            var animation = new DBLiteAnimationData
            {
                Name = mlpJson.String(dict, "name"),
                Duration = Mathf.Max(1, mlpJson.Int(dict, "duration", 1)),
                Loops = mlpJson.Int(dict, "playTimes", 1) == 0
            };

            // 2. Analyze the animation track of each bone (keyframe sequence including position, rotation, and scaling)

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

            // 3. Parse the animation track for each slot (contains keyframe sequences showing transitions and transparency)

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

            // 4. Parse the frame event list (events triggered at specific time points in the animation, such as sound effects or special effects)

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

                    // Accumulate the duration of each frame to get the absolute frame number when the event is triggered.

                    start += Mathf.Max(1, mlpJson.Int(frame, "duration", 1));
                }
            }

            return animation;
        }
    }

    /// <summary>
    /// Animation frame event: an event triggered at a specific time point of the animation, including the event name (such as playing sound effects, triggering special effects).

    /// </summary>
    public struct DBLiteAnimationFrameEvent
    {
        public float Frame; // The frame number when the event is triggered

        public string EventName; // Event name (such as sound effect name, special effect name)

    }

    /// <summary>
    /// Bone animation track: stores all the position/rotation keyframes of a bone in an animation, which is used to interpolate and calculate the bone posture during playback.

    /// </summary>
    public sealed class DBLiteBoneTrack
    {
        private readonly List<DBLiteTimedTransform> translate = new List<DBLiteTimedTransform>(); // Pan animation keyframe list

        private readonly List<DBLiteTimedTransform> rotate = new List<DBLiteTimedTransform>(); // Rotation animation keyframe list

        private readonly List<DBLiteTimedTransform> scale = new List<DBLiteTimedTransform>(); // Zoom animation keyframe list


        /// <summary>
        /// Parse translation, rotation, and scale keyframes for bone tracks from JSON.

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
        /// Samples bone tracks at specified frames, interpolating between keyframes.

        /// </summary>
        public DBLiteTransform Sample(float frame, float animationDuration, bool loop)
        {
            // 1. Based on unit transformation, perform interpolation sampling on three groups of key frames: translation, rotation, and scaling.

            var result = DBLiteTransform.Identity;
            var translation = SampleList(translate, frame, FrameKind.Translate, animationDuration, loop);
            var rotation = SampleList(rotate, frame, FrameKind.Rotate, animationDuration, loop);
            var scaling = SampleList(scale, frame, FrameKind.Scale, animationDuration, loop);

            // 2. Combine three sets of interpolation results into a complete transformation

            result.X = translation.X;
            result.Y = translation.Y;
            result.Rotation = rotation.Rotation;
            result.ScaleX = scaling.ScaleX;
            result.ScaleY = scaling.ScaleY;
            return result;
        }

        /// <summary>
        /// Parse a list of keyframes (translation, rotation, or scale) from a JSON array.
        /// </summary>
        private static void ParseTransformFrames(List<object> list, List<DBLiteTimedTransform> output, FrameKind kind)
        {
            // 1. Skip if there is no keyframe list

            if (list == null)
            {
                return;
            }

            // 2. Traverse each keyframe and record the start time, duration and transformation value

            var start = 0f;
            foreach (var item in list)
            {
                var dict = mlpJson.AsDict(item);
                if (dict == null)
                {
                    continue;
                }

                // 3. Read the transformation value, then only keep the corresponding fields according to the type (translation/rotation/zoom), and reset the rest

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

                // 4. Record the start time, duration of the keyframe and whether to use tweening (smooth transition)

                var duration = Mathf.Max(1, mlpJson.Int(dict, "duration", 1));
                output.Add(new DBLiteTimedTransform
                {
                    Start = start,
                    Duration = duration,
                    Transform = transform,
                    // DragonBones treats missing tweenEasing and tweenEasing: 0 as linear tweens.

                    // Only an explicit null value disables interpolation between transform keyframes.

                    Tween = !dict.ContainsKey("tweenEasing") || dict["tweenEasing"] != null
                });
                // 5. Accumulated time, starting time of next keyframe = current start + current duration

                start += duration;
            }
        }

        /// <summary>
        /// Finds keyframes surrounding the specified frame and performs transform interpolation.

        /// </summary>
        private static DBLiteTransform SampleList(List<DBLiteTimedTransform> list, float frame, FrameKind kind, float animationDuration, bool loop)
        {
            // 1. If there is no keyframe, return to the default posture.

            if (list.Count == 0)
            {
                return DBLiteTransform.Identity;
            }

            // 2. Find the interval where the current frame is located: current is the current key frame, next is the next key frame

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

            // 3. Loop animation: If the current frame has exceeded the last key frame, the next key frame will be the first one (connected from beginning to end)

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

            // 4. If the current keyframe does not use tweening, there is no next keyframe, or the duration is 0, return the current value directly (no interpolation)

            if (!current.Tween || !next.HasValue || current.Duration <= 0f)
            {
                return current.Transform;
            }

            // 5. Calculate the normalized progress of the current frame within the interval (between 0 and 1), and then interpolate between the two key frames

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
    /// Slot animation track: stores the display switching and color change key frames of a slot in an animation, and controls the display order and transparency of character parts.

    /// </summary>
    public sealed class DBLiteSlotTrack
    {
        private readonly List<DBLiteDisplayFrame> displayFrames = new List<DBLiteDisplayFrame>(); // Display a switching keyframe list to control which picture the slot displays at different times

        private readonly List<DBLiteColorFrame> colorFrames = new List<DBLiteColorFrame>(); // List of color/transparency keyframes to control the slot's fade


        /// <summary>
        /// Parse slot track display and color keyframes from JSON.

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
        /// Returns the active display index for the specified frame.

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
        /// Returns the transparency multiplier activated for the specified frame.

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
    /// Slot instance: represents a displayable part (such as arm, weapon) on the character, and manages its picture sprite, color and display order.

    /// </summary>
    public sealed class DBLiteSlotInstance
    {
        private readonly DBLiteSlotData slotData; // Slot configuration data, recording name, parent bone and display order

        private readonly List<DBLiteDisplayData> displays; // List of all elements (pictures or subskeletons) that this slot can display

        private readonly Transform slotTransform; // The Transform of the slot game object, which serves as the mount point for the display element.

        private readonly DBLiteFactory factory; // Skeleton animation factory reference, used to create sub-skeletons and obtain texture sprites
        private GameObject currentDisplayObject; // The currently displayed GameObject (picture sprite object or child skeleton object)

        private int currentDisplay = int.MinValue; // The currently displayed index, int.MinValue means not set

        private float currentAlpha = 1f; // Current transparency (0 is fully transparent, 1 is fully opaque)


        public DBLiteArmature ChildArmature { get; private set; } // When the display type is a sub-skeleton, reference its skeletal animation component


        /// <summary>
        /// Creates a slot instance that manages the current display object and child skeletons.

        /// </summary>
        public DBLiteSlotInstance(DBLiteSlotData slotData, List<DBLiteDisplayData> displays, Transform slotTransform, DBLiteFactory factory)
        {
            // 1. Save slot configuration data, displayable element list, mount point transformation and factory reference

            this.slotData = slotData;
            this.displays = displays ?? new List<DBLiteDisplayData>();
            this.slotTransform = slotTransform;
            this.factory = factory;
            // 2. Immediately display the default content (picture or sub-skeleton) according to the initial display index

            SetDisplay(slotData.DisplayIndex);
        }

        /// <summary>
        /// Switch slots to display different content (pictures or sub-skeletons) via index.

        /// </summary>
        public void SetDisplay(int index)
        {
            // 1. If the new index is the same as the current one, there is no need to switch

            if (index == currentDisplay)
            {
                return;
            }

            // 2. Record the new index and destroy the old display object (use DestroyImmediate in the editor and Destroy at runtime)

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

            // 3. If the index is out of bounds, no content will be displayed.

            if (index < 0 || index >= displays.Count)
            {
                return;
            }

            // 4. Create sub-skeletons or picture sprites based on display type

            var display = displays[index];
            if (display.Type == "armature")
            {
                // 5. Type is sub-skeleton: recursively build a complete sub-skeleton animation

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
                // 6. Type is picture: Create a sprite renderer and find the corresponding picture from the texture atlas

                var go = new GameObject($"{slotData.Name}:{display.Name}");
                go.transform.SetParent(slotTransform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = FindTextureAtlasSprite(display);
                renderer.sortingOrder = 20 + slotData.Order;
                currentDisplayObject = go;
                ApplyDisplayTransform(go.transform, display.Transform);
            }

            // 7. Keep current transparency settings

            ApplyAlphaToCurrentDisplay(currentAlpha);
        }

        /// <summary>
        /// Sets the opacity of the currently displayed object (0 = invisible, 1 = fully visible).

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
        /// Resets the slot to its initial display index and full opacity.

        /// </summary>
        public void ResetToSetupPose()
        {
            SetDisplay(slotData.DisplayIndex);
            SetAlpha(1f);
        }

        /// <summary>
        /// Find the sprite that displays the content, the sphere clip slot uses themed ball sprites.

        /// </summary>
        private Sprite FindTextureAtlasSprite(DBLiteDisplayData display)
        {
            // 1. Special handling of the sphere clip slot: using the sphere skin sprite of the current game

            if (display != null && display.Name == ".Game/ball/BallClip")
            {
                return mlpGameplaySpriteLoader.LoadMatchBallSprite(
                    mlpInventory.Instance.MatchData.BallTheme,
                    0.5f,
                    0.5f);
            }

            // 2. Ordinary pictures: Obtain the private data fields of the parent skeleton component through reflection

            var armature = slotTransform.GetComponentInParent<DBLiteArmature>();
            var dataField = typeof(DBLiteArmature).GetField("data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var armatureData = dataField != null ? dataField.GetValue(armature) as DBLiteArmatureData : null;
            // 3. Find and return the corresponding sprite from the skeleton's texture atlas

            return armatureData != null ? armatureData.TextureAtlas.Sprite(display.Name) : null;
        }

        /// <summary>
        /// Applies the display transform's position, rotation, and scale to the GameObject.

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
        /// Apply transparency to all SpriteRenderers of the currently displayed object, or toggle the visibility of child skeletons.

        /// </summary>
        private void ApplyAlphaToCurrentDisplay(float alpha)
        {
            // 1. Skip if no object is displayed.
            if (currentDisplayObject == null)
            {
                return;
            }

            // 2. If it is a sub-skeleton, control visibility by activating/deactivating the entire object

            if (ChildArmature != null)
            {
                currentDisplayObject.SetActive(alpha > 0.001f);
                return;
            }

            // 3. If it is a picture sprite, modify the color alpha value and enabled state of each SpriteRenderer

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
                // 4. Directly disable the renderer when alpha is extremely small (to avoid wasting performance by drawing invisible objects)
                renderer.enabled = alpha > 0.001f;
            }
        }
    }

    /// <summary>Bone data: stores the name of a bone, the name of the parent bone and the initial transformation (position, rotation). </summary>
    public sealed class DBLiteBoneData
    {
        public string Name; // Bone name

        public string Parent; // The name of the parent bone. If empty, it indicates the root bone.
        public DBLiteTransform Transform; // Initial transformation of the bone (position, rotation, scale)
    }

    /// <summary>Slot data: stores the name of a slot, parent bone name, current display index and rendering order. </summary>
    public sealed class DBLiteSlotData
    {
        public string Name; // Slot name
        public string Parent; // Mounted parent bone name

        public int DisplayIndex; // Default displayed image/sub-skeleton index

        public int Order; // Render sorting value, the larger the value, the higher it is displayed.
    }

    /// <summary>Display data: The name, type and initial transformation of a displayable element in the storage slot. </summary>
    public sealed class DBLiteDisplayData
    {
        public string Name; // Display the name of the element (picture name or child skeleton name)

        public string Type; // Display type: "image" is the image sprite, "armature" is the child skeleton
        public DBLiteTransform Transform; // Displays the element's offset transformation relative to the slot
    }

    /// <summary>Transformation data: stores position (X/Y) and rotation angle, used for spatial transformation calculation of bones and slots. </summary>
    public struct DBLiteTransform
    {
        public float X; // horizontal position
        public float Y; // vertical position

        public float Rotation; // Rotation angle (degrees)

        public float ScaleX; // Horizontal scaling (1 is original size)

        public float ScaleY; // Vertical scaling (1 is original size)


        public static DBLiteTransform Identity => new DBLiteTransform
        {
            X = 0f,
            Y = 0f,
            Rotation = 0f,
            ScaleX = 1f,
            ScaleY = 1f
        };

        /// <summary>
        /// Parse transformation data (x, y, rotation, scale) from a JSON dictionary.

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
        /// Merge this transformation with the animation transformation: translation/rotation are additive, scale are multiplied.
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
        /// Interpolates between two transformations based on frame type (translation, rotation, or scale).
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

    /// <summary>Transformation keyframe with time: record the transformation value at a certain point in time and whether to use tweening animation. </summary>
    public struct DBLiteTimedTransform
    {
        public float Start; // The starting frame number of the key frame
        public float Duration; // Key frame duration number of frames

        public bool Tween; // Whether to use tween animation (true means smooth transition, false means instant switching)

        public DBLiteTransform Transform; // The transform value of this keyframe
    }

    /// <summary>Display switching keyframe: record which display element is switched to at what point in time. </summary>
    public struct DBLiteDisplayFrame
    {
        public float Start; // Key frame starting frame number

        public float Duration; // Key frame duration number of frames

        public int Value; // The index of the display element to switch to (corresponding to the serial number in the DisplaysBySlot list)
    }

    /// <summary>Color keyframe: records the transparency change at what point in time, used for fade-in and fade-out effects. </summary>
    public struct DBLiteColorFrame
    {
        public float Start; // Key frame starting frame number

        public float Duration; // Key frame duration number of frames

        public float Alpha; // Transparency multiplier (0 is fully transparent, 1 is fully opaque)
    }

    /// <summary>Sub-texture data: records the name, position (X/Y) and size (width/height) of a small image in the atlas. </summary>
    public struct DBLiteSubTexture
    {
        public string Name; // Sub-texture name (corresponding to image name)

        public float X; // Horizontal pixel coordinates in the large image

        public float Y; // Vertical pixel coordinates in the large image
        public float Width; // Subtexture width (pixels)
        public float Height; // Subtexture height (pixels)
    }

    /// <summary>Keyframe type: Identifies whether the animation frame is a bone transformation frame, a slot display frame, or a color frame. </summary>
    public enum FrameKind
    {
        Translate, // Pan animation keyframes

        Rotate, // Rotate animation keyframes

        Scale // Zoom animation keyframes
    }
}
