using UnityEngine;

/// <summary>
/// PlayerController — Handles player input, movement, and lane switching.
/// TODO: Implement lane switching, jump, slide mechanics.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float forwardSpeed = 8f;
    public float laneWidth = 3f;
    public float laneSwitchSpeed = 12f;

    private int currentLane = 1; // 0=left, 1=center, 2=right
    private float targetX;

    void Start()
    {
        targetX = 0f;
    }

    void Update()
    {
        if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing) return;

        HandleInput();
        MoveForward();
        MoveLateral();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            SwitchLane(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            SwitchLane(1);
    }

    void SwitchLane(int direction)
    {
        int newLane = Mathf.Clamp(currentLane + direction, 0, 2);
        if (newLane == currentLane) return;
        currentLane = newLane;
        targetX = (currentLane - 1) * laneWidth;
    }

    void MoveForward()
    {
        transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime);
    }

    void MoveLateral()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, targetX, laneSwitchSpeed * Time.deltaTime);
        transform.position = pos;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            GameManager.Instance?.GameOver();
        }
        else if (other.CompareTag("Coin"))
        {
            GameManager.Instance?.AddScore(10);
            Destroy(other.gameObject);
        }
    }
}
