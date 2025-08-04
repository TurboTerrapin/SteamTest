/*
    PlayerMove.cs
    - Handles player movement
    - Handles seating/unseating teleporting
    - Handles shifting while seated
    Contributor(s): John Aylward, Jake Schott
    Last Updated: 6/23/2025
*/

using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerMove : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static float SHIFT_SPEED = 1.5f;
    public float MOVE_SPEED = 5.0f;

    //[SerializeField]
    [SerializeField]
    private Vector2 moveDir = new Vector2();
    [SerializeField]
    private Rigidbody playerRB = null;

    private Coroutine sit_coroutine = null;
    private Coroutine shift_coroutine = null;
    private Coroutine move_coroutine = null;
    private int shift_index = -1; //used for shifting
    private bool shift_increasing = false; //used for shifting
    private SeatManager seat_manager = null;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void initialize()
    {
        seat_manager = GameObject.FindGameObjectWithTag("SeatHandler").GetComponent<SeatManager>();

        transform.GetComponent<CapsuleCollider>().excludeLayers = LayerMask.GetMask("None");
        transform.GetComponent<Rigidbody>().useGravity = true;

        move_coroutine = StartCoroutine(checkForMovement());
    }

    public void sitDown(int pos)
    {
        if (move_coroutine != null)
        {
            StopCoroutine(move_coroutine);
        }

        move_coroutine = null;

        //figure out which shift point the player is closer to
        int closest_index = -1;
        float closest_dist = 9999;
        GameObject position_info_holder = seat_manager.position_point_holders[pos];
        for (int i = 1; i < position_info_holder.transform.childCount; i++)
        {
            float temp_dist = Vector3.Distance(transform.position, position_info_holder.transform.GetChild(i).position);
            if (temp_dist < closest_dist)
            {
                closest_dist = temp_dist;
                closest_index = i;
                shift_index = i;
            }
        }

        transform.position = position_info_holder.transform.GetChild(closest_index).position;

        if (sit_coroutine != null)
        {
            StopCoroutine(sit_coroutine);
            sit_coroutine = null;
        }

        //captain doesn't shift
        if (pos != 3)
        {
            sit_coroutine = StartCoroutine(checkForShifting());
        }
    }

    public void getUp(int pos)
    {
        if (move_coroutine != null)
        {
            StopCoroutine(move_coroutine);
            move_coroutine = null;
        }

        if (shift_coroutine != null)
        {
            StopCoroutine(shift_coroutine);
            shift_coroutine = null;
        }

        if (sit_coroutine != null)
        {
            StopCoroutine(sit_coroutine);
            sit_coroutine = null;
        }

        if (pos == 3) //captain exception
        {
            transform.position = seat_manager.position_point_holders[3].transform.GetChild(0).position;
        }

        move_coroutine = StartCoroutine(checkForMovement());
    }

    IEnumerator shift(int pos)
    {
        GameObject pph = seat_manager.position_point_holders[pos];

        Vector3 start_pos = pph.transform.GetChild(shift_index).position;
        
        if (shift_index == pph.transform.childCount - 1) //must decrease
        {
            shift_index--;
            shift_increasing = false;
        }
        else if (shift_index != 1) //in the middle
        {
            if (shift_increasing == true)
            {
                shift_index++;
            }
            else
            {
                shift_index--;
            }
        }
        else //increasing, use default positions
        {
            shift_index++;
            shift_increasing = true;
        }
        Vector3 end_pos = pph.transform.GetChild(shift_index).position;

        float total_shift_time = Vector3.Distance(start_pos, end_pos) / SHIFT_SPEED;
        float shift_time = total_shift_time;
        while (shift_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            shift_time = Mathf.Max(0.0f, shift_time - dt);
            transform.localPosition =
                new Vector3(Mathf.Lerp(end_pos.x, start_pos.x, shift_time / total_shift_time),
                            Mathf.Lerp(end_pos.y, start_pos.y, shift_time / total_shift_time),
                            Mathf.Lerp(end_pos.z, start_pos.z, shift_time / total_shift_time));

            yield return null;
        }

        shift_coroutine = null;

        sit_coroutine = StartCoroutine(checkForShifting());
    }

    IEnumerator checkForShifting()
    {

        while (shift_coroutine == null)
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftShift) || UnityEngine.Input.GetKeyDown(KeyCode.RightShift))
            {
                int pos = ControlScript.Instance.currentSeat();
                shift_coroutine = StartCoroutine(shift(pos));
            }
            yield return null;
        }

        sit_coroutine = null;
    }

    IEnumerator checkForMovement()
    {
        while (true)
        {
            yield return null;
            updateMovement();
        }
    }

    private void updateMovement()
    {
        if (!gameObject.GetComponent<PlayerMove>().IsOwner) return;

        moveDir.x = Input.GetAxis("Horizontal");
        moveDir.y = Input.GetAxis("Vertical");
        Debug.DrawLine(transform.position, transform.position + transform.forward * 1.25f);
        if (moveDir.magnitude > 1)
        {
            moveDir.Normalize();
        }
        Move();

        //teleport back if you fall
        if (transform.localPosition.y < -10)
        {
            transform.localPosition = Vector3.zero;
            playerRB.linearVelocity = Vector3.zero;
        }
    }

    void Move()
    {
        Vector3 movement; //= Vector3.zero;

        if (transform.parent != null) //local movement
        {
            Quaternion combinedRotation = transform.parent.rotation * transform.localRotation;
            Vector3 localMovement = new Vector3(moveDir.x, 0, moveDir.y) * MOVE_SPEED * Time.deltaTime;
            movement = combinedRotation * localMovement;
            transform.position += movement;
        }
        else //world movement
        {
            movement = transform.TransformDirection(new Vector3(moveDir.x, 0, moveDir.y)) * MOVE_SPEED * Time.deltaTime;
            transform.position += movement;
        }
    }
}