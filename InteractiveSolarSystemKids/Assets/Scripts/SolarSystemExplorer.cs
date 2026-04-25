using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SolarSystemExplorer : MonoBehaviour
{
    [Header("Camera")]
    public Camera mainCamera;
    public float cameraMoveSpeed = 4.5f;
    public float cameraTurnSpeed = 6.5f;

    [Header("UI")]
    public Text titleText;
    public Text factText;
    public Button backButton;

    [Header("Main View Text")]
    public string mainTitle = "Solar System Explorer";
    public string mainFact = "Click Sun, Earth, or Moon to learn a space fact.";

    private SolarSystemTarget currentTarget;
    private Vector3 mainViewPosition;
    private Quaternion mainViewRotation;

    private void Awake()
    {
        if (mainFact == "Click Earth or Moon to learn a space fact.")
        {
            mainFact = "Click Sun, Earth, or Moon to learn a space fact.";
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            mainViewPosition = mainCamera.transform.position;
            mainViewRotation = mainCamera.transform.rotation;
        }

        EnsureKnownTarget(
            "Sun",
            "Sun",
            "The Sun is a star. It gives light and warmth to the planets.",
            new Vector3(0f, 0.55f, -2f),
            1.18f);
    }

    private void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(ReturnToMainView);
            backButton.gameObject.SetActive(false);
        }

        ShowMainViewText();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            ReturnToMainView();
        }

        if (Input.GetMouseButtonDown(0) && !PointerIsOverUi())
        {
            TrySelectClickedTarget();
        }
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector3 desiredPosition = mainViewPosition;
        Quaternion desiredRotation = mainViewRotation;

        if (currentTarget != null)
        {
            desiredPosition = currentTarget.transform.position + currentTarget.cameraOffset;
            Vector3 lookDirection = currentTarget.transform.position - desiredPosition;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            }
        }

        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            desiredPosition,
            Time.deltaTime * cameraMoveSpeed);

        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            desiredRotation,
            Time.deltaTime * cameraTurnSpeed);
    }

    public void ReturnToMainView()
    {
        currentTarget = null;

        if (backButton != null)
        {
            backButton.gameObject.SetActive(false);
        }

        ShowMainViewText();
    }

    private void TrySelectClickedTarget()
    {
        if (mainCamera == null)
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            SolarSystemTarget target = GetTargetFromHit(hit.collider);

            if (target != null)
            {
                SelectTarget(target);
            }
        }
    }

    private void SelectTarget(SolarSystemTarget target)
    {
        currentTarget = target;
        currentTarget.PlaySelectedFeedback();

        if (titleText != null)
        {
            titleText.text = target.displayName;
        }

        if (factText != null)
        {
            factText.text = target.factText;
        }

        if (backButton != null)
        {
            backButton.gameObject.SetActive(true);
        }
    }

    private void ShowMainViewText()
    {
        if (titleText != null)
        {
            titleText.text = mainTitle;
        }

        if (factText != null)
        {
            factText.text = mainFact;
        }
    }

    private SolarSystemTarget GetTargetFromHit(Collider hitCollider)
    {
        SolarSystemTarget target = hitCollider.GetComponentInParent<SolarSystemTarget>();

        if (target != null)
        {
            return target;
        }

        Transform current = hitCollider.transform;

        while (current != null)
        {
            if (current.name == "Sun")
            {
                return EnsureKnownTarget(
                    "Sun",
                    "Sun",
                    "The Sun is a star. It gives light and warmth to the planets.",
                    new Vector3(0f, 0.55f, -2f),
                    1.18f);
            }

            current = current.parent;
        }

        return null;
    }

    private SolarSystemTarget EnsureKnownTarget(
        string objectName,
        string displayName,
        string fact,
        Vector3 cameraOffset,
        float pulseScale)
    {
        GameObject targetObject = GameObject.Find(objectName);

        if (targetObject == null)
        {
            return null;
        }

        SolarSystemTarget target = targetObject.GetComponent<SolarSystemTarget>();

        if (target == null)
        {
            target = targetObject.AddComponent<SolarSystemTarget>();
        }

        target.displayName = displayName;
        target.factText = fact;
        target.cameraOffset = cameraOffset;
        target.pulseScaleMultiplier = pulseScale;

        return target;
    }

    private static bool PointerIsOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
