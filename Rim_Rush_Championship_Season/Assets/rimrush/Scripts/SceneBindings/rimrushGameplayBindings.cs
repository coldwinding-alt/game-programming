using UnityEngine;

namespace rimrush
{
    public sealed class rimrushGameplayBindings : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private rimrushArenaView arenaView;
        [SerializeField] private rimrushBasketView leftBasketView;
        [SerializeField] private rimrushBasketView rightBasketView;
        [SerializeField] private rimrushBallView ballView;
        [SerializeField] private rimrushPlayerView leftPlayerView;
        [SerializeField] private rimrushPlayerView rightPlayerView;
        [SerializeField] private Transform leftNeutralSpawn;
        [SerializeField] private Transform rightNeutralSpawn;
        [SerializeField] private Transform leftServeSpawn;
        [SerializeField] private Transform rightServeSpawn;
        [SerializeField] private rimrushEnergyBarSceneView energyBarSlot0;
        [SerializeField] private rimrushEnergyBarSceneView energyBarSlot1;
        [SerializeField] private rimrushEnergyBarSceneView energyBarSlot2;
        [SerializeField] private rimrushTeleportFxView leftTeleportFxView;
        [SerializeField] private rimrushTeleportFxView rightTeleportFxView;
        [SerializeField] private rimrushShieldView leftShieldView;
        [SerializeField] private rimrushShieldView rightShieldView;
        [SerializeField] private rimrushHudSceneView hudView;

        public Transform Root => root != null ? root : transform;
        public rimrushArenaView ArenaView => arenaView;
        public rimrushBasketView LeftBasketView => leftBasketView;
        public rimrushBasketView RightBasketView => rightBasketView;
        public rimrushBallView BallView => ballView;
        public rimrushHudSceneView HudView => hudView;

        public rimrushPlayerView GetPlayerView(int side)
        {
            return side == -1 ? leftPlayerView : rightPlayerView;
        }

        public Vector2 GetSpawnPosition(int side, bool serve)
        {
            var spawn = serve
                ? (side == -1 ? leftServeSpawn : rightServeSpawn)
                : (side == -1 ? leftNeutralSpawn : rightNeutralSpawn);
            if (spawn == null)
            {
                if (serve)
                {
                    return new Vector2(
                        side == -1 ? rimrushObjectsData.IndentGeneralX : rimrushConstants.Width - rimrushObjectsData.IndentGeneralX,
                        rimrushObjectsData.PlayerIndentY);
                }

                return new Vector2(
                    rimrushConstants.Width2 + side * rimrushObjectsData.PlayerIndentX,
                    rimrushObjectsData.PlayerIndentY);
            }

            return rimrushConstants.WorldToPixel(spawn.position);
        }

        public rimrushEnergyBarSceneView GetEnergyBarView(int controllerSlot)
        {
            switch (controllerSlot)
            {
                case 1:
                    return energyBarSlot1;
                case 2:
                    return energyBarSlot2;
                default:
                    return energyBarSlot0;
            }
        }

        public rimrushTeleportFxView GetTeleportFxView(int side)
        {
            return side == -1 ? leftTeleportFxView : rightTeleportFxView;
        }

        public rimrushShieldView GetShieldView(int side)
        {
            return side == -1 ? leftShieldView : rightShieldView;
        }
    }
}
