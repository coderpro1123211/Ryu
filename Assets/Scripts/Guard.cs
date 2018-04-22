using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guard : MonoBehaviour {

    public float falloffRange;
    public float falloffPow;

    public Vector2 patrollA;
    public Vector2 patrollB;

    public PlayerController player;
    public Vector2 origin;

    bool isAtA = true;
    FieldOfView fov;

    private void Start()
    {
        GameManager.Instance.guards.Add(this);
        origin = transform.position;
        fov = GetComponent<FieldOfView>();
        player = FindObjectOfType<PlayerController>();
    }

    public void Kill()
    {
        Debug.Log("Oof " + gameObject.name);
        GameManager.Instance.guards.Remove(this);
        Destroy(gameObject);
    }

    public float EstimateTurnTime()
    {
        Vector3 target;
        if (!isAtA) target = patrollA + origin;
        else target = patrollB + origin;

        return Vector3.Distance(transform.position, target) / 2f;
    }

    public float GetFalloff(float d, Vector2 pos)
    {
        float p = 0.1f;
        if (Mathf.Abs(Vector2.SignedAngle((pos - (Vector2)transform.position).normalized, GetComponentInChildren<SpriteRenderer>().flipX ? Vector2.left : Vector2.right)) < fov.viewAngle / 2)
        {
            if (d < fov.viewRadius)
            {
                p = 1;
            }
        }

        return (Mathf.Pow(falloffRange - d, falloffPow) / Mathf.Pow(falloffRange, falloffPow)) * p;
    }

    public float GetFalloff(float d, Vector2 pos, out float pp)
    {
        float p = 0.1f;
        if (Mathf.Abs(pp=Vector2.SignedAngle((pos - (Vector2)transform.position).normalized, GetComponentInChildren<SpriteRenderer>().flipX ? Vector2.left : Vector2.right)) < fov.viewAngle / 2)
        {
            if (d < fov.viewRadius)
            {
                p = 1;
            }
        }

        return (Mathf.Pow(falloffRange - d, falloffPow) / Mathf.Pow(falloffRange, falloffPow)) * p;
    }

    public float GetFalloff(float d)
    {
        return (Mathf.Pow(falloffRange - d, falloffPow) / Mathf.Pow(falloffRange, falloffPow));
    }

    public IEnumerator DoTurn()
    {
        Vector3 target;
        float d;

        if (!isAtA) target = patrollA + origin;
        else target = patrollB + origin;
        fov.reverseAngle = GetComponentInChildren<SpriteRenderer>().flipX = ((target - transform.position).x < 0);
        GetComponent<Animator>().SetTrigger("Walk");

        do
        {
            transform.position += Vector3.ClampMagnitude(target - transform.position, Mathf.Min(d = Vector3.Distance(transform.position, target), Time.deltaTime * 2f));
            if (GameManager.Instance.hasWon)
            {
                GetComponent<Animator>().SetTrigger("Idle");
                yield break;
            }
            yield return null;
        } while (d > 0.05f);

        float r = Random.value;

        float p;
        Debug.Log("[" + gameObject.name + "] " + r + ":" + GetFalloff(d, player.transform.position, out p));
        Debug.Log("[" + gameObject.name + "] " + p);
        Debug.Log("[" + gameObject.name + "] " + d + " units");
        if (r < GetFalloff(d, player.transform.position))
        {
            if (Physics2D.Raycast(transform.position, (player.transform.position - transform.position).normalized, (player.transform.position - transform.position).magnitude, LayerMask.GetMask("Ground")).transform == null)
            {
                Debug.Log("[" + gameObject.name + "] Enemy Spotted!");
                GameManager.Instance.PlayerLose();
                Destroy(player.gameObject);
            }
        }

        isAtA = !isAtA;
        GetComponent<Animator>().SetTrigger("Idle");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (Application.isPlaying)
        {
            Gizmos.DrawLine(origin + patrollA, origin + patrollB);
        }
        else
        {
            Gizmos.DrawLine((Vector2)transform.position + patrollA, (Vector2)transform.position + patrollB);
        }
        Gizmos.color = new Color(1, 0, 0, GetFalloff(7));
        Gizmos.DrawSphere(transform.position, 7);
        Gizmos.color = new Color(0, 1, 0, GetFalloff(3));
        Gizmos.DrawSphere(transform.position, 3);
    }
}
