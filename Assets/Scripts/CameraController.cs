using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
    public LayerMask cullingMask;
    public Color maskCol;
    public Material mat;
    [Range(0,1)]
    public float trans;

    public float xMin;
    public float xMax;

    public bool canMove;
    public Vector2 mouseLast;

    Camera c;

    private void Start()
    {
        c = new GameObject("EffectCamera").AddComponent<Camera>();
        c.transform.parent = Camera.main.transform;
        c.transform.SetPositionAndRotation(transform.position, transform.rotation);
        c.orthographic = true;
        c.orthographicSize = Camera.main.orthographicSize;
        c.cullingMask = cullingMask;
        c.backgroundColor = maskCol;

        Camera.main.cullingMask = ~cullingMask;

        RenderTexture rt = new RenderTexture(Mathf.FloorToInt(c.orthographicSize * 32 * c.aspect), Mathf.FloorToInt(c.orthographicSize * 32), 24, RenderTextureFormat.ARGB32);
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.filterMode = FilterMode.Point;
        c.targetTexture = rt;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            mouseLast = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        if (Input.GetMouseButton(2))
        {
            Vector2 delta = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - mouseLast;
            transform.position = new Vector3(Mathf.Clamp(transform.position.x - delta.x, xMin, xMax), transform.position.y, transform.position.z);
            mouseLast = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    public IEnumerator FocusOnPlayer(PlayerController pl, float sTime, float mSpd, System.Action callback)
    {
        canMove = false;
        Vector3 vel = Vector2.zero;
        Vector3 target = pl.transform.position + Vector3.back * 10;
        target.y = transform.position.y;
        target.x = Mathf.Clamp(target.x, xMin, xMax);

        while (Vector2.Distance(transform.position, target) > 0.1f)
        {
            transform.position = Vector3.SmoothDamp(transform.position, target, ref vel, sTime, mSpd, Time.deltaTime); 
            yield return null;
        }
        Debug.Log("[CameraController] Focus Finished");
        canMove = true;
        callback();
    }

    public IEnumerator Transition(float spd)
    {
        if (spd < 0)
        {
            while (trans > 0)
            {
                trans += Time.deltaTime * spd;
                yield return null;
            }
        }
        else
        {
            while (trans < 1)
            {
                trans += Time.deltaTime * spd;
                yield return null;
            }
        }
        trans = Mathf.Clamp01(trans);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        mat.SetTexture("_Mask", c.targetTexture);
        mat.SetFloat("_Trans", Mathf.Clamp01(trans));
        Graphics.Blit(source, destination, mat);
    }
}
