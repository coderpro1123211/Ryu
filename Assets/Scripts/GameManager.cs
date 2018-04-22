using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

    public Turn currentTurn;
    public PlayerController player;
    public int moveLimit;
    public int movesLeft;
    public Text mvsLft;

    public List<Guard> guards;
    bool isRunning;
    bool isPaused;
    CameraController ctr;

    public static GameManager Instance { get; private set; }

    public GameObject ui;
    public GameObject winScr;
    public Text winTxt;
    public GameObject pausePanel;

    public LineRenderer lr;
    public bool hasWon;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Unpause();
        //lr = ui.GetComponentInChildren<LineRenderer>();
        ctr = FindObjectOfType<CameraController>();
        movesLeft = moveLimit;
    }

    public void FinishPlayerTurn()
    {
        Debug.Log("[GameManager] FinishPlayerTurn()");
        ctr.canMove = true;
        currentTurn = Turn.Enemy; 
        isRunning = false;
        lr.positionCount = 0;
        DoNextTurn();
    }

    public void Pause()
    {
        if (hasWon) return;
        isPaused = true;
        pausePanel.SetActive(isPaused);
        Time.timeScale = 0;
    }
    public void Unpause()
    {
        if (hasWon) return;
        isPaused = false;
        pausePanel.SetActive(isPaused);
        Time.timeScale = 1;
    }
    public void TogglePause()
    {
        if (isPaused) Unpause();
        else Pause();
    }
    public void GotoMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public bool CanRegisterMove()
    {
        return movesLeft > 0 && !isPaused;
    }

    public void RegisterMove(bool success)
    {
        if (success) movesLeft--;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
        if (Input.GetKeyDown(KeyCode.E) && !isRunning && !isPaused)
        {
            DoNextTurn();
        }
    }

    public void DoNextTurn()
    {
        Debug.Log("[GameManager] DoNextTurn()");
        isRunning = true;
        switch (currentTurn)
        {
            case Turn.Player:
                StartCoroutine(ctr.FocusOnPlayer(player, 0.2f, 50, () =>
                {
                    ctr.canMove = false;
                    StartCoroutine(ctr.Transition(2));
                    StartCoroutine(player.DoNextTurn());
                }));
                ui.SetActive(false);
                movesLeft = moveLimit;
                break;
            case Turn.Enemy:
                StartCoroutine(DoEnemyTurn());
                break;
            default:
                break;
        }

        SwitchTurn();
    }

    public IEnumerator DoEnemyTurn()
    {
        Debug.Log("[GameManager] DoEnemyTurn()");
        float maxTime = 0;
        int trig = 0;
        yield return new WaitForSeconds(0.5f);

        foreach (Guard g in guards)
        {
            if (g != null)
            {
                maxTime = Mathf.Max(maxTime, g.EstimateTurnTime());
                StartCoroutine(g.DoTurn());
                trig++;
            }
        }
        if (trig <= 0)
        {
            Debug.Log("[GameManager] Player has won");
            hasWon = true;
            winScr.SetActive(true);
            winTxt.text = "You Win!";
            yield break;
        }
        yield return new WaitForSeconds(maxTime);
        StartCoroutine(ctr.Transition(-2));
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[GameManager] EnemyTurnFinish()");
        StartCoroutine(ctr.FocusOnPlayer(player, 0.5f, 10, () =>
        {
            isRunning = false;
            ui.SetActive(true);
        }));
    }
    public void PlayerLose()
    {
        winScr.SetActive(true);
        winTxt.text = "You Lose";
    }
    public void ReloadLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void UpdateUI(List<Move> moves)
    {
        // Line Renderer
        List<Vector3> positions = new List<Vector3>();
        if (moves.Count <= 0)
        {
            Ray2D rr = Helper.GetCameraRay(Helper.GetTargetPos(moves, moves.Count - 1, player.transform.position));
            positions.Add(player.transform.position);
            positions.Add(rr.origin + rr.direction * player.range);
            lr.positionCount = positions.Count;
            lr.SetPositions(positions.ToArray());
            goto after; // What could POSSIBLY go wrong :D
        }

        positions.Add(player.moves[0].myPos + Vector3.back * 0.05f);
        foreach (Move m in moves)
        {
            switch (m.action)
            {
                case Action.Stab:
                case Action.Move:
                case Action.Drop:
                    positions.Add(m.GetTargetPos() + Vector3.back * 0.05f);
                    break;
                case Action.Shoot:
                    positions.Add(m.GetTargetPos() + Vector3.back * 0.05f);
                    positions.Add(m.myPos + Vector3.back * 0.05f);
                    break;
                default:
                    Debug.LogError("[GameManager] INVALID MOVE " + m.action + " AT " + m.myPos);
                    break;
            }
        }
        Ray2D r = Helper.GetCameraRay(Helper.GetTargetPos(moves, moves.Count - 1, player.transform.position));
        positions.Add(r.origin + r.direction * player.range);
        lr.positionCount = positions.Count;
        lr.SetPositions(positions.ToArray());

        after:
        mvsLft.text = "Moves Left: " + movesLeft;

        //Other UI Elements
    }

    public void SwitchTurn()
    {
        Debug.Log("[GameManager] SwitchTurn()");
        if (currentTurn == Turn.Enemy) currentTurn = Turn.Player;
        else currentTurn = Turn.Enemy;
    }
}

public enum Turn
{
    Player, Enemy
}
