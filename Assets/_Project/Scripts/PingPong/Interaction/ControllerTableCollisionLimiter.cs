using UnityEngine;

[DefaultExecutionOrder(100)]
public class ControllerTableCollisionLimiter : MonoBehaviour
{
    public Transform tableTransform;
    public Vector2 tableSize = new Vector2(PingPongGeometry.TableWidth, PingPongGeometry.TableLength);
    public float tableTopY = PingPongGeometry.TableTopHeight;
    public float horizontalMargin = 0.035f;
    public float verticalMargin = 0.025f;

    private void LateUpdate()
    {
        if (tableTransform == null)
        {
            var table = GameObject.Find("Table");
            if (table != null) tableTransform = table.transform;
        }

        if (tableTransform == null) return;

        var position = transform.position;
        var local = position - tableTransform.position;
        var halfX = tableSize.x * 0.5f + horizontalMargin;
        var halfZ = tableSize.y * 0.5f + horizontalMargin;
        var insideX = Mathf.Abs(local.x) < halfX;
        var insideZ = Mathf.Abs(local.z) < halfZ;
        var belowTop = position.y < tableTopY + verticalMargin;

        if (!insideX || !insideZ || !belowTop) return;

        var pushX = halfX - Mathf.Abs(local.x);
        var pushZ = halfZ - Mathf.Abs(local.z);

        if (pushX < pushZ)
        {
            position.x += (local.x >= 0f ? 1f : -1f) * pushX;
        }
        else
        {
            position.z += (local.z >= 0f ? 1f : -1f) * pushZ;
        }

        transform.position = position;
    }
}
