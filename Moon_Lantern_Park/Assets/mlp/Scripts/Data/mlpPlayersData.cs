// 角色数据和动画管理
// 定义 8 个角色的属性（名字、头像、动画骨骼），负责加载角色模型、应用外观、播放动画。创建球员时都会来这里获取角色信息。

using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 角色数据管理器：定义 8 个角色的属性（名字、头像、动画骨骼），负责加载角色模型、应用外观、播放动画。
    /// </summary>
    public static class mlpPlayersData
    {
        private const int ActiveCharacterSkinCount = 8;
        private const float PortraitAtlasSourceScale = 4f;
        private const int PortraitCropPaddingPixels = 6;
        private const byte PortraitVisibleAlphaThreshold = 8;
        private const float GlobalCharacterModelScaleMultiplier = 1.08f;

        private sealed class mlpCharacterDefinition
        {
            public string DisplayName;
            public int SkinIndex;
            public int FormIndex;
            public int SuperId;
            public bool Enabled;
            public string PortraitSpriteName;
            public float HeadOffsetX;
            public float HeadOffsetY;
            public float HeadScale = 1f;
            public float ModelScaleMultiplier = 1f;
            public float PreviewScaleMultiplier = 1f;
            public float PreviewOffsetY;
            public float PortraitScaleMultiplier = 1f;
            // 头像偏移量以源精灵像素为单位，这样可以根据不同的 UI 槽位大小自动缩放。
            public float PortraitOffsetY;
        }

        private static DBLiteTextureAtlas portraitAtlas;
        private static readonly Dictionary<string, Sprite> PortraitDisplaySprites = new Dictionary<string, Sprite>();

        private static readonly mlpCharacterDefinition[] CharacterDefinitions =
        {
            new mlpCharacterDefinition { DisplayName = "REAPER", SkinIndex = 0, FormIndex = 0, SuperId = 3, Enabled = true, PortraitSpriteName = "custom_head_pumpkin", HeadOffsetX = 0.75f, HeadOffsetY = 9f, HeadScale = 1.02f, ModelScaleMultiplier = 1.08f, PreviewScaleMultiplier = 1f, PortraitScaleMultiplier = 1f, PortraitOffsetY = 8f },
            new mlpCharacterDefinition { DisplayName = "GHOST CLOWN", SkinIndex = 1, FormIndex = 1, SuperId = 0, Enabled = true, PortraitSpriteName = "custom_head_frankenstein", HeadOffsetX = 4.5f, HeadOffsetY = 1f, HeadScale = 1f, ModelScaleMultiplier = 1.06f, PreviewScaleMultiplier = 0.99f, PortraitScaleMultiplier = 0.98f, PortraitOffsetY = 9f },
            new mlpCharacterDefinition { DisplayName = "SKULL PIRATE", SkinIndex = 2, FormIndex = 2, SuperId = 1, Enabled = true, PortraitSpriteName = "custom_head_mummy", HeadOffsetX = 1.5f, HeadOffsetY = 0f, HeadScale = 1.02f, ModelScaleMultiplier = 1.07f, PreviewScaleMultiplier = 1f, PreviewOffsetY = -2f, PortraitScaleMultiplier = 0.98f, PortraitOffsetY = 9f },
            new mlpCharacterDefinition { DisplayName = "VAMPIRE", SkinIndex = 3, FormIndex = 3, SuperId = 2, Enabled = true, PortraitSpriteName = "custom_head_vampire", HeadOffsetY = -10.5f, HeadScale = 0.95f, PreviewScaleMultiplier = 0.96f, PortraitScaleMultiplier = 1f, PortraitOffsetY = 12f },
            new mlpCharacterDefinition { DisplayName = "CANDLEMAN", SkinIndex = 4, FormIndex = 4, SuperId = 3, Enabled = true, PortraitSpriteName = "custom_head_candle", HeadOffsetX = 2.75f, HeadOffsetY = 6f, HeadScale = 0.96f, PreviewScaleMultiplier = 0.94f, PortraitScaleMultiplier = 0.85f, PortraitOffsetY = -9f },
            new mlpCharacterDefinition { DisplayName = "SCARECROW", SkinIndex = 5, FormIndex = 5, SuperId = 0, Enabled = true, PortraitSpriteName = "custom_head_scarecrow", HeadOffsetY = 7f, HeadScale = 1.05f, PreviewScaleMultiplier = 0.97f, PreviewOffsetY = 2f, PortraitScaleMultiplier = 1.05f, PortraitOffsetY = -10f },
            new mlpCharacterDefinition { DisplayName = "WITCH", SkinIndex = 6, FormIndex = 6, SuperId = 2, Enabled = true, PortraitSpriteName = "custom_head_witch", HeadOffsetX = 3.5f, HeadOffsetY = 8f, HeadScale = 1.1f, PreviewScaleMultiplier = 0.98f, PreviewOffsetY = 2f, PortraitScaleMultiplier = 1.12f, PortraitOffsetY = -9f },
            new mlpCharacterDefinition { DisplayName = "BLACK CAT", SkinIndex = 7, FormIndex = 7, SuperId = 1, Enabled = true, PortraitSpriteName = "custom_head_blackcat", HeadOffsetX = 6f, HeadOffsetY = 7f, HeadScale = 0.99f, PreviewScaleMultiplier = 0.97f, PreviewOffsetY = 1f, PortraitScaleMultiplier = 0.96f, PortraitOffsetY = -5f }
        };

        private static readonly int[] Hands = { 1, 2, 3, 4, 5, 6, 7, 8 };
        private static readonly string[] Legs =
        {
            "leg1",
            "leg2",
            "leg3",
            "leg4",
            "leg5",
            "leg6",
            "leg7",
            "leg8"
        };

        public static int CharacterCount => CharacterDefinitions.Length;

        /// <summary>
        /// 初始化玩家角色系统。游戏启动时调用一次，准备好所有角色数据。
        /// </summary>
        public static void SetupPlayers()
        {
            // 当前万圣节角色使用显式的 8 角色 DragonBones 骨架集。
        }

        /// <summary>
        /// 加载并构建一个用于实战的 DragonBones 骨架。返回的骨架可以直接播放动画。
        /// </summary>
        /// <param name="name">要构建的骨架名称（例如角色骨骼名称）。</param>
        /// <returns>构建完成的 DBLiteArmature 实例，可用于动画播放。</returns>
        public static DBLiteArmature BuildGameplayArmature(string name)
        {
            DBLiteFactory.Instance.EnsureLoaded();
            return DBLiteFactory.Instance.BuildArmature("playerSmall", name);
        }

        /// <summary>
        /// 获取所有已启用角色的 ID 数组。用于角色选择界面或随机分配角色。
        /// </summary>
        /// <returns>已启用角色的索引数组。</returns>
        public static int[] GetActiveCharacterIds()
        {
            // 1. 创建一个临时列表，用来存放所有已启用角色的编号
            var active = new List<int>(CharacterDefinitions.Length);
            // 2. 遍历所有角色定义，把已启用的角色编号加入列表
            for (var i = 0; i < CharacterDefinitions.Length; i++)
            {
                if (CharacterDefinitions[i].Enabled)
                {
                    active.Add(i);
                }
            }

            // 3. 把列表转成数组返回
            return active.ToArray();
        }

        /// <summary>
        /// 验证角色 ID 是否有效且已启用。如果请求的角色不可用，则返回备用角色或第一个可用角色。
        /// </summary>
        /// <param name="requestedCharacterId">调用方想要的角色 ID。</param>
        /// <param name="fallbackCharacterId">当请求的角色被禁用时，使用的备用角色 ID。</param>
        /// <returns>一个有效且已启用的角色 ID。</returns>
        public static int SanitizeCharacterId(int requestedCharacterId, int fallbackCharacterId = 0)
        {
            if (IsCharacterEnabled(requestedCharacterId))
            {
                return requestedCharacterId;
            }

            if (IsCharacterEnabled(fallbackCharacterId))
            {
                return fallbackCharacterId;
            }

            var active = GetActiveCharacterIds();
            return active.Length > 0 ? active[0] : 0;
        }

        /// <summary>
        /// 切换到列表中下一个或上一个已启用的角色，到达末尾时自动循环到开头。
        /// </summary>
        /// <param name="currentCharacterId">当前选中的角色 ID。</param>
        /// <param name="direction">+1 表示下一个，-1 表示上一个。</param>
        /// <returns>下一个（或上一个）已启用角色的 ID。</returns>
        public static int StepCharacterId(int currentCharacterId, int direction)
        {
            // 1. 获取所有已启用角色的编号列表
            var active = GetActiveCharacterIds();
            if (active.Length == 0)
            {
                return 0;
            }

            // 2. 在列表中找到当前角色的位置（索引）
            var currentIndex = 0;
            for (var i = 0; i < active.Length; i++)
            {
                if (active[i] == currentCharacterId)
                {
                    currentIndex = i;
                    break;
                }
            }

            // 3. 根据方向（+1 下一个，-1 上一个）计算下一个位置，超出范围时循环回来
            var nextIndex = (currentIndex + direction) % active.Length;
            if (nextIndex < 0)
            {
                nextIndex += active.Length;
            }

            // 4. 返回下一个角色的编号
            return active[nextIndex];
        }

        /// <summary>
        /// 获取角色的显示名称，例如 "REAPER"、"WITCH" 等。
        /// </summary>
        /// <param name="characterId">角色索引。</param>
        /// <returns>角色的显示名称。</returns>
        public static string GetCharacterName(int characterId)
        {
            return GetCharacterDefinition(characterId).DisplayName;
        }

        /// <summary>
        /// 获取角色的体型索引，用于选择正确的身体动画。
        /// </summary>
        /// <param name="characterId">角色索引。</param>
        /// <returns>用于身体动画的体型索引。</returns>
        public static int GetCharacterFormIndex(int characterId)
        {
            return GetCharacterDefinition(characterId).FormIndex;
        }

        /// <summary>
        /// 获取角色的必杀技 ID，对应 mlpCharacterSkillType 中的技能类型。
        /// </summary>
        /// <param name="characterId">角色索引。</param>
        /// <returns>必杀技 ID。</returns>
        public static int GetCharacterSuperId(int characterId)
        {
            return GetCharacterDefinition(characterId).SuperId;
        }

        /// <summary>
        /// 获取角色预览模型的缩放倍数，用于菜单和角色选择界面中的模型显示。
        /// </summary>
        /// <param name="characterId">角色索引。</param>
        /// <returns>综合预览缩放倍数。</returns>
        public static float GetCharacterPreviewScaleMultiplier(int characterId)
        {
            var definition = GetCharacterDefinition(characterId);
            return definition.PreviewScaleMultiplier * GlobalCharacterModelScaleMultiplier * definition.ModelScaleMultiplier;
        }

        /// <summary>
        /// 获取角色在实战中的模型缩放倍数。
        /// </summary>
        /// <param name="characterId">角色索引。</param>
        /// <returns>实战综合缩放倍数。</returns>
        public static float GetCharacterGameplayScaleMultiplier(int characterId)
        {
            var definition = GetCharacterDefinition(characterId);
            return GlobalCharacterModelScaleMultiplier * definition.ModelScaleMultiplier;
        }

        /// <summary>
        /// 获取角色预览模型在菜单中的垂直偏移量。
        /// </summary>
        /// <param name="characterId">角色索引。</param>
        /// <returns>Y 轴偏移量（像素）。</returns>
        public static float GetCharacterPreviewOffsetY(int characterId)
        {
            return GetCharacterDefinition(characterId).PreviewOffsetY;
        }

        /// <summary>
        /// 获取角色裁剪后的头像精灵，首次调用时会自动创建并缓存。
        /// </summary>
        /// <param name="characterId">角色索引。</param>
        /// <param name="desiredMaxPixels">可选的最大像素尺寸提示（当前未使用）。</param>
        /// <returns>裁剪后的头像精灵，如果图集缺失则返回 null。</returns>
        public static Sprite GetCharacterPortraitSprite(int characterId, float desiredMaxPixels = 0f)
        {
            var definition = GetCharacterDefinition(characterId);
            var baseSprite = GetPortraitBaseSprite(definition);
            if (baseSprite == null)
            {
                return null;
            }

            return GetOrCreatePortraitDisplaySprite(definition.PortraitSpriteName, baseSprite);
        }

        /// <summary>
        /// 获取角色头像在 UI 中显示时的缩放倍数。
        /// </summary>
        /// <param name="characterId">角色索引。</param>
        /// <returns>头像缩放倍数。</returns>
        public static float GetCharacterPortraitScaleMultiplier(int characterId)
        {
            return GetCharacterDefinition(characterId).PortraitScaleMultiplier;
        }

        /// <summary>
        /// 获取角色头像精灵在 UI 中的垂直偏移量。如果提供了精灵，会根据精灵大小自动调整偏移。
        /// </summary>
        /// <param name="characterId">角色索引。</param>
        /// <param name="portraitSprite">可选的精灵，用于相对于基础头像大小缩放偏移量。</param>
        /// <returns>Y 轴偏移量（源精灵像素单位，已乘以图集源缩放系数）。</returns>
        public static float GetCharacterPortraitOffsetY(int characterId, Sprite portraitSprite = null)
        {
            // 1. 获取角色定义，计算基础偏移量（乘以图集缩放系数转为像素单位）
            var definition = GetCharacterDefinition(characterId);
            var baseOffset = definition.PortraitOffsetY * PortraitAtlasSourceScale;
            // 2. 如果没有提供参考精灵，直接返回基础偏移量
            if (portraitSprite == null)
            {
                return baseOffset;
            }

            // 3. 获取图集中的原始头像精灵
            var baseSprite = GetPortraitBaseSprite(definition);
            if (baseSprite == null)
            {
                return baseOffset;
            }

            // 4. 比较原始精灵和参考精灵的大小，按比例缩放偏移量
            var baseMaxPixels = Mathf.Max(baseSprite.rect.width, baseSprite.rect.height);
            var spriteMaxPixels = Mathf.Max(portraitSprite.rect.width, portraitSprite.rect.height);
            if (baseMaxPixels <= 0.0001f || spriteMaxPixels <= 0.0001f)
            {
                return baseOffset;
            }

            // 5. 用参考精灵与原始精灵的大小比例来缩放偏移量
            return baseOffset * (spriteMaxPixels / baseMaxPixels);
        }

        /// <summary>
        /// 将角色的皮肤、体型和位置调整应用到骨架上。在生成玩家时调用。
        /// </summary>
        /// <param name="armature">要配置的骨架。</param>
        /// <param name="characterId">要应用外观的角色索引。</param>
        public static void ApplyCharacter(DBLiteArmature armature, int characterId)
        {
            var definition = GetCharacterDefinition(characterId);
            SwitchPlayer(armature, definition.SkinIndex, definition.FormIndex);
            ApplyCharacterTuning(armature, definition);
        }

        /// <summary>
        /// 随机选取一个已启用的角色，可排除指定的角色。用于 AI 对手的选择。
        /// </summary>
        /// <param name="excludedCharacterIds">要排除的角色 ID 列表（例如玩家已选择的角色）。</param>
        /// <returns>随机选取的已启用角色 ID。</returns>
        public static int GetRandomCharacterId(IList<int> excludedCharacterIds = null)
        {
            // 1. 创建候选列表，筛选出所有"已启用"且"不在排除名单中"的角色
            var candidates = new List<int>(CharacterDefinitions.Length);
            for (var i = 0; i < CharacterDefinitions.Length; i++)
            {
                // 跳过被禁用的角色
                if (!CharacterDefinitions[i].Enabled)
                {
                    continue;
                }

                // 跳过需要排除的角色（比如玩家已经选了这个角色）
                if (excludedCharacterIds != null && excludedCharacterIds.Contains(i))
                {
                    continue;
                }

                candidates.Add(i);
            }

            // 2. 如果没有可用的候选角色，返回一个安全的默认值
            if (candidates.Count == 0)
            {
                return SanitizeCharacterId(0);
            }

            // 3. 从候选列表中随机选一个返回
            return candidates[Random.Range(0, candidates.Count)];
        }

        /// <summary>
        /// 切换骨架的头部、身体、手部和腿部，使其匹配指定的皮肤和体型。切换完成后自动播放待机动画。
        /// </summary>
        /// <param name="armature">要更新的骨架。</param>
        /// <param name="skinId">皮肤索引（0-7），控制头部、手部和腿部的外观。</param>
        /// <param name="formId">体型索引，控制身体动画。</param>
        public static void SwitchPlayer(DBLiteArmature armature, int skinId, int formId)
        {
            if (armature == null)
            {
                return;
            }

            // 1. 将皮肤编号和体型编号限制在有效范围内
            skinId = Mathf.Clamp(skinId, 0, ActiveCharacterSkinCount - 1);
            formId = Mathf.Max(0, formId);

            // 2. 根据皮肤编号查找对应的手部和腿部动画名称
            var hand = Hands[skinId];
            var leg = Legs[skinId];

            // 3. 分别切换头部、身体、左手、右手、挖球手的动画
            armature.GetChildArmature("head")?.Play("head" + (skinId + 1));
            armature.GetChildArmature("body")?.Play("body" + (formId + 1));
            armature.GetChildArmature("left hand")?.Play("hand" + hand);
            armature.GetChildArmature("right hand")?.Play("hand" + hand);
            armature.GetChildArmature("dighand")?.Play("hand" + hand);
            // 4. 切换左腿、右腿和挖球腿的动画
            armature.GetChildArmature("left leg")?.Play(leg);
            armature.GetChildArmature("right leg")?.Play(leg);
            armature.GetChildArmature("digleg")?.Play(leg);
            // 5. 播放待机动画，让角色呈现站立状态
            armature.Play("idle");
        }

        /// <summary>
        /// 根据 ID 查找内部角色定义，会先验证 ID 的有效性。
        /// </summary>
        /// <param name="characterId">角色索引。</param>
        /// <returns>匹配的 mlpCharacterDefinition 实例。</returns>
        private static mlpCharacterDefinition GetCharacterDefinition(int characterId)
        {
            return CharacterDefinitions[SanitizeCharacterId(characterId)];
        }

        /// <summary>
        /// 从图集中获取角色的原始（未裁剪）头像精灵。
        /// </summary>
        /// <param name="definition">要查找的角色定义。</param>
        /// <returns>头像图集中的基础精灵，如果未找到则返回 null。</returns>
        private static Sprite GetPortraitBaseSprite(mlpCharacterDefinition definition)
        {
            var atlas = GetPortraitAtlas();
            return atlas?.Sprite(definition.PortraitSpriteName);
        }

        /// <summary>
        /// 从 Resources 文件夹加载并缓存头像纹理图集。如果资源缺失则返回 null。
        /// </summary>
        /// <returns>缓存的 DBLiteTextureAtlas 实例，加载失败时返回 null。</returns>
        private static DBLiteTextureAtlas GetPortraitAtlas()
        {
            // 1. 如果已经加载过头像图集，直接返回缓存的结果
            if (portraitAtlas != null)
            {
                return portraitAtlas;
            }

            // 2. 拼接头像图集的资源路径，加载纹理和 JSON 配置文件
            var portraitAtlasPath = mlpAssets.Portraits.ResourcePath(mlpAssets.Portraits.UiAtlas);
            var textureJsonAsset = Resources.Load<TextAsset>(portraitAtlasPath);
            var texture = Resources.Load<Texture2D>(portraitAtlasPath);
            // 3. 如果资源文件缺失，输出警告并返回空
            if (textureJsonAsset == null || texture == null)
            {
                Debug.LogWarning("Missing UI portrait atlas resources.");
                return null;
            }

            // 4. 设置纹理的过滤模式为"点采样"（保持像素风格清晰），包裹模式为"钳制"（边缘不重复）
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            // 5. 解析 JSON 配置，把纹理和配置组合成图集对象，缓存起来
            portraitAtlas = DBLiteTextureAtlas.Parse(mlpAssets.Portraits.UiAtlas, texture, textureJsonAsset.text);
            return portraitAtlas;
        }

        /// <summary>
        /// 获取或创建用于 UI 显示的裁剪头像精灵。结果会被缓存，每个角色只裁剪一次。
        /// </summary>
        /// <param name="portraitSpriteName">用作缓存键的精灵名称。</param>
        /// <param name="baseSprite">要裁剪的原始未裁剪图集精灵。</param>
        /// <returns>裁剪并缓存后的头像精灵。</returns>
        private static Sprite GetOrCreatePortraitDisplaySprite(string portraitSpriteName, Sprite baseSprite)
        {
            // 1. 如果这个头像已经裁剪过并缓存了，直接返回缓存的结果
            if (PortraitDisplaySprites.TryGetValue(portraitSpriteName, out var cached))
            {
                return cached;
            }

            // 2. 获取头像所在的纹理，确认纹理可以读取像素数据
            var texture = baseSprite.texture;
            if (texture == null || !texture.isReadable)
            {
                Debug.LogWarning($"Portrait atlas texture must be readable to crop UI portraits: {portraitSpriteName}");
                return baseSprite;
            }

            // 3. 扫描像素，计算头像可见区域的边界框
            var visibleRect = CalculatePortraitVisibleRect(texture, baseSprite.rect);
            if (visibleRect.width <= 0.0001f || visibleRect.height <= 0.0001f)
            {
                return baseSprite;
            }

            // 4. 计算裁剪后精灵的中心点（锚点），让它保持在原来的位置
            var baseCenter = baseSprite.rect.center;
            var pivot = new Vector2(
                Mathf.InverseLerp(visibleRect.xMin, visibleRect.xMax, baseCenter.x),
                Mathf.InverseLerp(visibleRect.yMin, visibleRect.yMax, baseCenter.y));
            // 5. 创建新的精灵，只包含可见区域的部分
            var sprite = UnityEngine.Sprite.Create(
                texture,
                visibleRect,
                pivot,
                baseSprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = $"{portraitSpriteName}_ui_crop";

            // 6. 把裁剪后的精灵存入缓存，下次直接使用
            PortraitDisplaySprites[portraitSpriteName] = sprite;
            return sprite;
        }

        /// <summary>
        /// 扫描头像纹理的像素，找到所有可见（非透明）像素的边界框，并添加内边距。
        /// </summary>
        /// <param name="sourceTexture">包含头像像素的纹理。</param>
        /// <param name="sourceRect">纹理中原始精灵的矩形区域。</param>
        /// <returns>围绕可见像素的紧凑矩形，已添加内边距。</returns>
        private static Rect CalculatePortraitVisibleRect(Texture2D sourceTexture, Rect sourceRect)
        {
            // 1. 确定要扫描的像素范围（原始精灵在纹理中的矩形区域）
            var xStart = Mathf.Clamp(Mathf.FloorToInt(sourceRect.xMin), 0, sourceTexture.width - 1);
            var xEnd = Mathf.Clamp(Mathf.CeilToInt(sourceRect.xMax), xStart + 1, sourceTexture.width);
            var yStart = Mathf.Clamp(Mathf.FloorToInt(sourceRect.yMin), 0, sourceTexture.height - 1);
            var yEnd = Mathf.Clamp(Mathf.CeilToInt(sourceRect.yMax), yStart + 1, sourceTexture.height);
            // 2. 一次性读取纹理中所有像素的颜色数据
            var pixels = sourceTexture.GetPixels32();
            // 3. 初始化边界值为"最不可能"的初始状态，方便后续用 Min/Max 更新
            var minX = xEnd;
            var maxX = xStart - 1;
            var minY = yEnd;
            var maxY = yStart - 1;

            // 4. 逐行逐列扫描每个像素，找到所有不透明像素的最左、最右、最上、最下位置
            for (var y = yStart; y < yEnd; y++)
            {
                var rowOffset = y * sourceTexture.width;
                for (var x = xStart; x < xEnd; x++)
                {
                    // 跳过透明和半透明的像素（透明度低于阈值的视为不可见）
                    if (pixels[rowOffset + x].a <= PortraitVisibleAlphaThreshold)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            // 5. 如果没有找到任何可见像素，直接返回原始区域
            if (maxX < minX || maxY < minY)
            {
                return sourceRect;
            }

            // 6. 在可见区域四周加上一些内边距（留白），防止裁剪得太紧
            minX = Mathf.Max(xStart, minX - PortraitCropPaddingPixels);
            maxX = Mathf.Min(xEnd - 1, maxX + PortraitCropPaddingPixels);
            minY = Mathf.Max(yStart, minY - PortraitCropPaddingPixels);
            maxY = Mathf.Min(yEnd - 1, maxY + PortraitCropPaddingPixels);
            // 7. 返回最终的裁剪矩形（x, y, 宽度, 高度）
            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>
        /// 调整骨架上头部和身体的变换，使其匹配角色的位置和缩放设定。
        /// </summary>
        /// <param name="armature">要调整的骨架。</param>
        /// <param name="definition">包含偏移和缩放值的角色定义。</param>
        private static void ApplyCharacterTuning(DBLiteArmature armature, mlpCharacterDefinition definition)
        {
            if (armature == null)
            {
                return;
            }

            // 1. 调整头部的位置和大小，使其对齐到像素网格（防止模糊）
            var head = armature.GetChildArmature("head");
            if (head != null)
            {
                var headPosition = head.transform.localPosition;
                headPosition.x = definition.HeadOffsetX;
                headPosition.y = definition.HeadOffsetY;
                headPosition.z = 0f;
                head.transform.localPosition = mlpConstants.SnapLocalPositionToScreenPixels(head.transform.parent, headPosition);
                head.transform.localScale = new Vector3(definition.HeadScale, definition.HeadScale, 1f);
            }

            // 2. 调整身体的位置（重置 Y 和 Z 为 0），同样对齐到像素网格
            var body = armature.GetChildArmature("body");
            if (body != null)
            {
                var bodyPosition = body.transform.localPosition;
                bodyPosition.y = 0f;
                bodyPosition.z = 0f;
                body.transform.localPosition = mlpConstants.SnapLocalPositionToScreenPixels(body.transform.parent, bodyPosition);
                body.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// 检查角色 ID 是否在有效范围内且已被标记为启用。
        /// </summary>
        /// <param name="characterId">要检查的角色索引。</param>
        /// <returns>如果角色存在且已启用则返回 true。</returns>
        private static bool IsCharacterEnabled(int characterId)
        {
            return characterId >= 0
                && characterId < CharacterDefinitions.Length
                && CharacterDefinitions[characterId].Enabled;
        }
    }
}
