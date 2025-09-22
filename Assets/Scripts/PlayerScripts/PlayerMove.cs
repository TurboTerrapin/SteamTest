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
    private bool is_left = false; //used for shifting
    private SeatManager seat_manager = null;

    [SerializeField]
    private Animator animator = null;

    AnimationController myAnimationController = null;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        myAnimationController = GetComponent<AnimationController>();
        //animator = GetComponent<Animator>();
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
        animator.SetBool("Sitting", true);
        myAnimationController.setPlayerRotationLock(true);
        
        //Sets the rotation of the player to forward
        myAnimationController.setPlayerForwardRotation();
        //myAnimationController.setCharacterPosition(new Vector3(-0.63f, -0.3f, 0));
        myAnimationController.setCharacterPosition(new Vector3(0, -0.3f, 0));

        //Sets the specific rotation of the player to 135 degrees from forward, to fit with the direction of the engineer position
        if (pos == 2)
        {
            myAnimationController.setPlayerForwardRotation(Quaternion.AngleAxis(135, Vector3.up));
        }

        if (pos == 3)
        {
            myAnimationController.setCharacterPosition(new Vector3(0, -0.3f, 0));
            //myAnimationController.setPlayerForwardRotation(Quaternion.AngleAxis(180, Vector3.up));

        }

        if (move_coroutine != null)
        {
            StopCoroutine(move_coroutine);
        }

        move_coroutine = null;

        //figure out which shift point the player is closer to
        float dist_to_left_pos = Vector3.Distance(transform.position, seat_manager.left_shift_position_points[pos].transform.position);
        float dist_to_right_pos = Vector3.Distance(transform.position, seat_manager.right_shift_position_points[pos].transform.position);

        is_left = (dist_to_left_pos < dist_to_right_pos);

        if (is_left == true)
        {
            transform.position = seat_manager.left_shift_position_points[pos].transform.position;
        }
        else
        {
            transform.position = seat_manager.right_shift_position_points[pos].transform.position;
        }

        if (sit_coroutine != null)
        {
            StopCoroutine(sit_coroutine);
            sit_coroutine = null;
        }

        sit_coroutine = StartCoroutine(checkForShifting());
    }

    public void getUp(int pos)
    {
        myAnimationController.setPlayerRotationLock(false);
        animator.SetBool("Sitting", false);

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
            transform.localPosition = seat_manager.position_points[3].transform.localPosition;
        }

        move_coroutine = StartCoroutine(checkForMovement());
    }

    IEnumerator shift(Vector3 start_pos, Vector3 end_pos)
    {
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

        is_left = !is_left;

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
                if (is_left == true) //left to right
                {
                    shift_coroutine = StartCoroutine(shift(seat_manager.left_shift_position_points[pos].transform.localPosition, seat_manager.right_shift_position_points[pos].transform.localPosition));
                }
                else //right to left
                {
                    shift_coroutine = StartCoroutine(shift(seat_manager.right_shift_position_points[pos].transform.localPosition, seat_manager.left_shift_position_points[pos].transform.localPosition));
                }
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

        animator.SetFloat("Movement", moveDir.magnitude);
        animator.SetFloat("Forward", moveDir.y);

        if (transform.parent != null) //local movement
        {
            Quaternion combinedRotation = transform.localRotation;
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


    //OLD CODE

    /*
    void FixedUpdate()
    {
        moveDir.x = Input.GetAxis("Horizontal");
        moveDir.y = Input.GetAxis("Vertical");
        Debug.DrawLine(transform.position, transform.position + transform.forward * 10);
        if (moveDir.magnitude > 1)
        {
            moveDir.Normalize();
        }
        Move();

        if (transform.localPosition.y < -10)
        {
            transform.localPosition = new Vector3(0f, 1f, 1f);
            playerRB.linearVelocity = Vector3.zero;
        }
        else if (moveDir == Vector2.zero)
        {
            playerRB.linearVelocity = Vector3.zero;
        }
    }
    
    */

    /*
    
    void Move()
    {
        //transform.localPosition += new Vector3  (moveDir.x, 0, moveDir.y) * moveSpeed * Time.deltaTime;
        transform.localPosition += transform.right * moveDir.x * moveSpeed * Time.deltaTime;
        transform.localPosition += transform.forward * moveDir.y * moveSpeed * Time.deltaTime;
    }
    */
}