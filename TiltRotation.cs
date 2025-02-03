using System.Collections;
using UnityEngine;

public class TiltRotation : MonoBehaviour
{
    public Vector2Int waitRange, rotationRange;
    public float speed;

    [HideInInspector]
    public Vector2 tilt;

    Vector3 initalRotation;
    Quaternion targetRotation;

    public void Start()
    {
        initalRotation = transform.localRotation.eulerAngles;
        targetRotation = transform.rotation;

        StartCoroutine(StartRepeating());
    }

    public void LateUpdate()
    {
        GetTargetRotation();
        Rotate();
    }

    /// <summary>
    /// Sets the target rotation using current and tilt rotation.
    /// </summary>
    void GetTargetRotation()
    {
        targetRotation = Quaternion.Euler(initalRotation + (Vector3)tilt);
    }

    IEnumerator StartRepeating()
    {
        while (true)
        {
            SetTiltRandom();

            int randomWaitForSeconds = Random.Range(waitRange.x, waitRange.y);

            yield return new WaitForSeconds(randomWaitForSeconds);
        }
    }

    /// <summary>
    /// Sets the tilt rotation random using <c>rotationRange</c>.
    /// </summary>
    void SetTiltRandom()
    {
        tilt = new(Random.Range(rotationRange.x, rotationRange.y), Random.Range(rotationRange.x, rotationRange.y));
    }

    /// <summary>
    /// Lerps the rotation to the target rotation smoothly.
    /// </summary>
    void Rotate()
    {
        Quaternion currentRotation = transform.rotation;
        transform.rotation = Quaternion.Lerp(currentRotation, targetRotation, speed * Time.deltaTime);
    }
}
