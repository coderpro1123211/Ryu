using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knife : MonoBehaviour {
    public float timeToImpact;
    public Transform target;
    Vector3 pos;
    float startAngle;

    float t;

    private void Start()
    {
        pos = transform.position;
        startAngle = transform.rotation.eulerAngles.z;
    }

    void Update ()
    {
        t += Time.deltaTime / timeToImpact;
        transform.position = Vector3.Lerp(pos, target.position, t);
        transform.rotation = Quaternion.Euler(0, 0, t*720+startAngle);

        if (t > 1)
        {
            Destroy(target.gameObject);
            Destroy(gameObject);
        }
	}
}
