using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour {
    public List<Move> moves;

    [Space]
    public float animationCountdown;
    public float delayBetweenMoves;
    public float range;
    public float animationLerpPow;

    [Space]
    public Animator anim;
    public SpriteRenderer rend;
    public Knife knife;

    [Space]
    public LayerMask guards;
    public LayerMask ground;

    bool isHanging;
    bool isAnimating;
    bool reversePow;
    float timer;
    int index;
    bool facingRight;

    public IEnumerator DoNextTurn()
    {
        yield return new WaitForSeconds(0.5f);
        isAnimating = true;

        for (int i = 0; i < moves.Count; i++)
        {
            Debug.Log("Starting move " + i);
            SetMoveDefaults(i);

            DoMove(moves[i], moves[i<=0?0:i-1]);

            yield return null;


            Vector3 a = Helper.GetTargetPos(moves, i, transform.position);
            Vector3 b = Helper.GetTargetPos(moves, i-1, transform.position);
            reversePow = a.y > b.y;
            //rend.flipY = moves[i].endOnCeiling;

            anim.SetBool("onCeiling", moves[i].endOnCeiling);
            anim.SetBool("onWall", moves[i].endOnWall);

            yield return new WaitForSeconds(animationCountdown-0.05f);

            rend.flipX = !moves[i].faceRight;// : !moves[i].faceRight;
            //transform.rotation = moves[i].targetRot;
            if (moves[i].action == Action.Move)
            {
                transform.position = moves[i].GetTargetPos();
            }
            if (moves[i].action == Action.Stab)
            {
                anim.SetTrigger("Land");
                if (moves[i].target == null) Debug.Log("SHIT");
                Debug.Log("Trying to get Guard component on " + moves[i].target.name);
                moves[i].target.GetComponent<Guard>().Kill();
            }
            Debug.Log("Move " + i + " finished");

            yield return new WaitForSeconds(delayBetweenMoves + 0.05f);
        }

        isAnimating = false;
        moves.Clear();

        yield return new WaitForSeconds(0.5f);

        GameManager.Instance.FinishPlayerTurn();
    }

    public void SetMoveDefaults(int i)
    {
        index = i;
        timer = 0;
        reversePow = false;

        anim.ResetTrigger("Flip");
    }

    public void DoMove(Move m, Move last)
    {
        // Move logic here (the move logic, dont need to move shit here)
        switch (m.action)
        {
            case Action.Move:
            case Action.Drop:
                anim.SetFloat("flipSpeed", 1f / animationCountdown);
                anim.SetBool("reverseFlip", m.endOnCeiling);
                anim.SetTrigger("Flip");

                break;
            case Action.Shoot:
                Shoot(m);
                break;
            case Action.Stab:
                anim.SetTrigger("Launch");
                break;
            default:
                break;
        }
    }

    public void Shoot(Move m)
    {
        // Kabooom, b**ch!
        Knife k = Instantiate(knife);

        k.target = m.target.transform;
        k.timeToImpact = Vector3.Distance(m.targetPos, m.myPos) / 20f;

        k.transform.position = m.myPos;
        k.transform.rotation = Quaternion.LookRotation(Vector3.back, (m.GetTargetPos() - m.myPos).normalized);
    }

    private void Update()
    {
        RunAnimation();

        DoInput();
    }

    void RunAnimation()
    {
        if (isAnimating && (moves[index].action & (Action.Move | Action.Drop | Action.Stab)) != 0)
        {
            timer += Time.deltaTime / (animationCountdown);
            timer = Mathf.Clamp01(timer);
            //Debug.Log(timer + " : " + index);
            transform.position = Vector3.Lerp(moves[index].myPos, moves[index].GetTargetPos(), reversePow ? (1 - Mathf.Pow(1 - timer, animationLerpPow)) : Mathf.Pow(timer, animationLerpPow));
            return;
        }
    }

    void DoInput()
    {
        if (isAnimating) return;
        GameManager.Instance.UpdateUI(moves);
        bool succ; // <insert lenny face here>
        if (EventSystem.current != null && !EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButtonDown(0) && GameManager.Instance.CanRegisterMove()) // Primary 
            {
                // Grapple
                GameManager.Instance.UpdateUI(Helper.AddMoveGrapple(moves, transform, range, ground, out succ));
                GameManager.Instance.RegisterMove(succ);
            }
            if (Input.GetMouseButtonDown(1) && GameManager.Instance.CanRegisterMove()) // Secondary
            {
                // Shoot
                GameManager.Instance.UpdateUI(Helper.AddMoveShoot(moves, transform.position, transform.rotation, range, guards | ground, out succ));
                GameManager.Instance.RegisterMove(succ);
            }
            if (Input.GetKeyDown(KeyCode.F) && GameManager.Instance.CanRegisterMove()) // Teritary
            {
                // Fall down + stab
                Debug.Log("Trying to stab");
                GameManager.Instance.UpdateUI(Helper.AddMoveStab(moves, transform, transform.rotation, range, ground, guards, out succ));
                GameManager.Instance.RegisterMove(succ);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 p = Helper.GetTargetPos(moves, moves.Count - 1, transform.position);
        Gizmos.color = Color.green;

        float f;
        Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
        new Plane(Vector3.back, 0).Raycast(r, out f);
        Gizmos.DrawLine(p, p + (r.GetPoint(f) - p).normalized * range * 2);
        Gizmos.DrawWireSphere(p, range);

        if (moves.Count <= 0) return;

        Gizmos.color = Color.red;

        for (int i = 0; i < moves.Count; i++)
        {
            Move m = moves[i];
            Gizmos.DrawLine(m.myPos, m.GetTargetPos());
        }
    }
}
#region Helpers and Structs
public static class Helper
{
    const float HALF_HEIGHT = 2.5f;

    public static Ray2D GetCameraRay(Vector3 pos)
    {
        float f;
        Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
        new Plane(Vector3.back, 0).Raycast(r, out f);
        return new Ray2D(pos, (r.GetPoint(f) - pos).normalized);
    }

    public static Vector3 GetTargetPos(List<Move> mvs, int index, Vector3 nullValue)
    {
        if (mvs.Count < 1) return nullValue;
        for (int i = index; i >= 0; i--)
        {
            if ((mvs[i].action & (Action.Move | Action.Drop | Action.Stab)) != 0) return mvs[i].GetTargetPos();
        }
        return nullValue;
    }

    public static bool GetLastEndOnCeiling(List<Move> mvs)
    {
        if (mvs.Count <= 0) return false;
        else return mvs[mvs.Count - 1].endOnCeiling;
    }

    public static bool GetLastEndOnWall(List<Move> mvs)
    {
        if (mvs.Count <= 0) return false;
        else return mvs[mvs.Count - 1].endOnWall;
    }

    public static RaycastHit2D GetRaycastHit(List<Move> moves, Vector3 pos, out Vector2 tpos, float range, LayerMask l)
    {
        tpos = GetTargetPos(moves, moves.Count - 1, pos);
        Ray2D r = GetCameraRay(tpos);
        Debug.DrawRay(r.origin, r.direction * range, Color.white, 5);
        RaycastHit2D h = Physics2D.Raycast(r.origin, r.direction, range, l);
        return h;
    }

    public static RaycastHit2D GetRaycastHit(List<Move> moves, out Vector2 tpos, Vector3 dir, Vector3 pos, float range, LayerMask l)
    {
        tpos = GetTargetPos(moves, moves.Count - 1, pos);
        Ray2D r = new Ray2D(tpos, dir);
        Debug.DrawRay(r.origin, r.direction * range, Color.blue, 5);
        RaycastHit2D h = Physics2D.Raycast(r.origin, r.direction, range, l);
        return h;
    }

    public static List<Move> AddMoveGrapple(List<Move> moves, Transform pos, float range, LayerMask l, out bool success)
    {
        Vector2 tpos;
        RaycastHit2D h = GetRaycastHit(moves, pos.position, out tpos, range, l);

        bool endOnWall = false;
        endOnWall = Vector2.Dot(Vector2.left, h.normal) > 0.95f ? true : endOnWall;
        endOnWall = Vector2.Dot(Vector2.left, h.normal) < -0.95f ? true : endOnWall;
        success = false;
        if (h.collider == null) return moves;
        moves.Add(
            new Move(
                Action.Move, 
                null, 
                tpos, 
                h.point + h.normal * HALF_HEIGHT, 
                Quaternion.LookRotation(pos.forward, h.normal), 
                endOnWall ? Vector2.Dot(Vector2.left, h.normal) > 0f : tpos.x > h.point.x, 
                Vector2.Dot(Vector2.down, h.normal) > 0.95f,
                endOnWall
                ));
        success = true;
        return moves;
    }

    public static List<Move> AddMoveShoot(List<Move> moves, Vector3 pos, Quaternion def, float range, LayerMask l, out bool success)
    {
        Vector2 tpos;
        RaycastHit2D h = GetRaycastHit(moves, pos, out tpos, range, l);
        success = false;
        if (h.collider == null) return moves;
        if (h.transform.root.GetComponent<Guard>() == null) return moves;
        moves.Add(new Move(Action.Shoot, h.transform.root.gameObject, tpos, h.point, moves.Count > 0 ? moves[(moves.Count - 1)].targetRot : def, tpos.x > h.point.x, GetLastEndOnCeiling(moves), GetLastEndOnWall(moves)));
        success = true;
        return moves;
    }

    public static List<Move> AddMoveDrop(List<Move> moves, Vector3 pos, float range, LayerMask l, out bool success)
    {
        Vector2 tpos;
        RaycastHit2D h = GetRaycastHit(moves, out tpos, Vector2.down, pos, Mathf.Infinity, l);
        success = false;
        if (h.collider == null) return moves;
        moves.Add(new Move(Action.Drop, null, tpos, h.point + h.normal * HALF_HEIGHT, Quaternion.identity, tpos.x > h.point.x));
        success = true;
        return moves;
    }

    public static List<Move> AddMoveStab(List<Move> moves, Transform pos, Quaternion def, float range, LayerMask l, LayerMask l2, out bool success)
    {
        Vector2 tpos;
        RaycastHit2D h = GetRaycastHit(moves, out tpos, Vector3.down, pos.position, range, l);
        success = false;
        if (h.transform == null)
        {
            Debug.Log("H MISS");
            return moves;
        }
         
        RaycastHit2D h2 = GetRaycastHit(moves, pos.position, out tpos, range, l2);
        if (h2.transform == null)
        {
            Debug.Log("H2 MISS");
            return moves;
        }

        moves.Add(new Move(Action.Move, null, tpos, h.point + h.normal * HALF_HEIGHT, Quaternion.LookRotation(pos.forward, h.normal), h2.point.x < h.point.x));

        tpos = GetTargetPos(moves, moves.Count - 1, pos.position);

        Debug.Log("H2");
        Debug.Log("\t" + h2.transform.gameObject.name);
        Debug.Log("\t" + h2.transform.root.gameObject.name);
        Debug.Log("\t" + h2.point);

        Debug.Log("H");
        Debug.Log("\t" + h.transform.gameObject.name);
        Debug.Log("\t" + h.transform.root.gameObject.name);
        Debug.Log("\t" + h.point);

        Vector2 p = h2.point;
        p.y = tpos.y;
        moves.Add(new Move(Action.Stab, h2.transform.root.gameObject, tpos, p, moves.Count > 0 ? moves[(moves.Count - 1)].targetRot : def, h2.point.x < h.point.x));
        success = true;
        return moves;
    }
}

[System.Serializable]
public struct Move
{
    public Action action;
    public Vector3 myPos;
    public GameObject target; // If appliciable
    public Vector3 targetPos; // This too 
    public bool faceRight;
    public bool endOnCeiling;
    public bool endOnWall;

    public Quaternion targetRot;

    public Vector3 GetTargetPos()
    {
        return targetPos;//target == null ? targetPos : target.transform.position;
    }

    public Move(Action action, GameObject target, Vector3 myPos, Vector3 targetPos, Quaternion targetRot, bool faceRight = false, bool endOnCeiling = false, bool endOnWall = false)
    {
        this.action = action;
        this.target = target;
        this.myPos = myPos;
        this.targetPos = targetPos;
        this.targetRot = targetRot;
        this.faceRight = faceRight;
        this.endOnCeiling = endOnCeiling;
        this.endOnWall = endOnWall;
    }
}
[System.Serializable]
public enum Action
{
    Move = 1, Drop = 2, Shoot = 4, Stab = 8
}
#endregion