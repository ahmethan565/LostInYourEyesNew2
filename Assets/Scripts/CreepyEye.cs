using UnityEngine;
public class CreepyEye : MonoBehaviour
{
    public float rotationSpeed = 500f;
    public bool randomizeAxes = true;

    void Update()
    {
        Vector3 axis = randomizeAxes ?
            new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) :
            Vector3.up;

        transform.Rotate(axis * rotationSpeed * Time.deltaTime);
    }

}
