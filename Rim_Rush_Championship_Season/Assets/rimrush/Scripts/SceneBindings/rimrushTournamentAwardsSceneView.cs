using UnityEngine;

namespace rimrush
{
    public sealed class rimrushTournamentAwardsSceneView : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        public GameObject Root => root != null ? root : gameObject;
    }
}
