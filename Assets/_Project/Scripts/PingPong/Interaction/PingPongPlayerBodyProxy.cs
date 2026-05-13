using UnityEngine;

[DefaultExecutionOrder(-20)]
public class PingPongPlayerBodyProxy : MonoBehaviour
{
    public Transform hmdTransform;
    public float floorY = 0f;
    public float bodyHeightMeters = 1.2f;
    public float bodyRadiusMeters = 0.18f;
    public string playerBodyTag = "PlayerBody";
    public string playerBodyLayerName = "PlayerBody";

    private CapsuleCollider _collider;
    private Camera _camera;

    private void OnEnable()
    {
        ResolveReferences();
        ConfigureIdentity();
        ConfigureCollider();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        if (hmdTransform == null) return;

        var position = hmdTransform.position;
        transform.position = new Vector3(position.x, floorY + bodyHeightMeters * 0.5f, position.z);

        var forward = Vector3.ProjectOnPlane(hmdTransform.forward, Vector3.up);
        if (forward.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        ConfigureCollider();
    }

    private void ResolveReferences()
    {
        if (hmdTransform != null) return;

        if (_camera == null)
        {
            _camera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>(true);
        }

        if (_camera != null)
        {
            hmdTransform = _camera.transform;
        }
    }

    private void ConfigureIdentity()
    {
        if (!string.IsNullOrEmpty(playerBodyTag))
        {
            try
            {
                gameObject.tag = playerBodyTag;
            }
            catch (UnityException)
            {
                gameObject.tag = "Untagged";
            }
        }

        if (!string.IsNullOrEmpty(playerBodyLayerName))
        {
            var layer = LayerMask.NameToLayer(playerBodyLayerName);
            if (layer >= 0)
            {
                gameObject.layer = layer;
            }
        }
    }

    private void ConfigureCollider()
    {
        if (_collider == null)
        {
            _collider = GetComponent<CapsuleCollider>();
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<CapsuleCollider>();
            }
        }

        _collider.isTrigger = true;
        _collider.direction = 1;
        _collider.height = Mathf.Max(bodyRadiusMeters * 2f, bodyHeightMeters);
        _collider.radius = Mathf.Max(0.01f, bodyRadiusMeters);
        _collider.center = Vector3.zero;
    }
}
