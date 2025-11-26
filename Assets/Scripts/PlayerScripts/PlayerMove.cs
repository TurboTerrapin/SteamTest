/*
    PlayerMove.cs
    - Names the player prefab to USERNAME_STEAMID if client, or OTHER_CLIENT if not
    - Handles player movement
    - Handles seating/unseating teleporting
    - Handles shifting while seated
    - Enables collisions/rigidbody/gravity on the player character
    Contributor(s): John Aylward, Jake Schott
    Last Updated: 11/22/2025
*/

using System.Collections;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

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

    private Coroutine seat_change_coroutine = null;
    private Coroutine shift_coroutine = null;
    private Coroutine move_coroutine = null;
    private Coroutine reposition_coroutine = null;
    private SeatManager seat_manager = null;

    [SerializeField]
    private Animator animator = null;

    AnimationController myAnimationController = null;

    void Start()
    {
        DontDestroyOnLoad(gameObject);

        myAnimationController = GetComponent<AnimationController>();

        if (transform.gameObject.GetComponent<NetworkObject>().IsOwner == true)
        {
            //USERNAME_STEAMID
            transform.name = SteamClient.Name + "_" + SteamClient.SteamId.ToString();
        }
        else
        {
            transform.name = "OTHER_CLIENT";
        }
    }

    public void initialize()
    {
        seat_manager = GameObject.FindGameObjectWithTag("SeatHandler").GetComponent<SeatManager>();

        if (move_coroutine == null)
        {
            move_coroutine = StartCoroutine(checkForMovement());
        }
    }

    //called by FailureHandler on game restart
    public void resetPlayerMove()
    {
        StopAllCoroutines();
        move_coroutine = null;
        seat_change_coroutine = null;
        shift_coroutine = null;
        reposition_coroutine = null;
    }

    //called by ControlScript
    public void sitDown(int pos)
    {
        resetPlayerMove();
        seat_change_coroutine = StartCoroutine(sitDownSequence(pos));
    }

    //handles sit down sequence
    IEnumerator sitDownSequence(int pos)
    {
        animator.transform.GetComponent<AnimatorHandler>().setIKActive(false);
        GameObject to_orient = seat_manager.getSitDownPosition(pos, transform.position);
        animator.SetBool("IsLeft", seat_manager.getSitDownDirection(pos, transform.position));
        animator.SetInteger("Seat", pos);
        reposition_coroutine = StartCoroutine(repositionPlayer(to_orient.transform.localPosition + to_orient.transform.parent.localPosition, to_orient.transform.localRotation.eulerAngles.y, 0.2f));

        yield return reposition_coroutine;
        reposition_coroutine = null;

        animator.applyRootMotion = true;
        animator.SetBool("SittingDown", true);
        animator.SetBool("GettingUp", false); //trigger sit down animation
    }

    public void getUp(int pos)
    {
        resetPlayerMove();
        seat_change_coroutine = StartCoroutine(getUpSequence(pos));
    }

    //orients camera for get up
    IEnumerator getUpSequence(int pos)
    {
        Transform camera_transform = transform.GetComponent<CameraMove>().camera_transform;

        myAnimationController.setCharacterPosition(new Vector3(0, 0.12f, 0));
        animator.transform.GetComponent<AnimatorHandler>().setIKActive(false);
        transform.GetComponent<CameraMove>().lockCamera();

        Quaternion starting_rotation = camera_transform.localRotation;
        float dest_angle_x = 0.0f;
        if (pos == 3)
        {
            dest_angle_x = 180.0f;
        }
        
        float anim_time = 0.15f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            camera_transform.localRotation = Quaternion.Lerp(Quaternion.Euler(30.0f, dest_angle_x, 0.0f), starting_rotation, anim_time / 0.15f);
            camera_transform.position = transform.GetComponent<CameraMove>().head_transform.position;

            yield return null;
        }
        yield return new WaitForSeconds(0.05f);

        camera_transform.parent = transform.GetComponent<CameraMove>().head_transform;
        animator.SetBool("IsLeft", seat_manager.getGetUpDirection(pos));
        animator.SetBool("GettingUp", true); //trigger get up animation
    }

    //orients player and camera for sit down
    IEnumerator repositionPlayer(Vector3 new_position, float new_rotation, float time)
    {
        Transform camera_transform = transform.GetComponent<CameraMove>().camera_transform;

        Vector3 starting_position = transform.localPosition;
        float starting_rotation = transform.localRotation.eulerAngles.y;
        float starting_cam_rotation = camera_transform.localRotation.eulerAngles.x;
        if (new_rotation == 0.0f && starting_rotation > 180.0f)
        {
            starting_rotation = 0.0f - (360.0f - starting_rotation);
        }

        float anim_time = time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            transform.localPosition = Vector3.Lerp(new_position, starting_position, anim_time / time);
            transform.localRotation = Quaternion.Euler(0.0f, Mathf.Lerp(new_rotation, starting_rotation, anim_time / time), 0.0f);
            camera_transform.localRotation = Quaternion.Euler(Mathf.Lerp(30.0f, starting_cam_rotation, anim_time / time), 0.0f, 0.0f);
            camera_transform.position = transform.GetComponent<CameraMove>().head_transform.position;

            yield return null;
        }

        yield return new WaitForSeconds(0.05f);
    }

    //returns true if currently shifting
    public bool isShifting()
    {
        return (shift_coroutine != null);
    }

    //returns true if shifting or sitting/getting up
    public bool isAnimating()
    {
        return (shift_coroutine != null || seat_change_coroutine != null);
    }

    public void seatShift(int pos)
    {
        if (pos == 3) //captain doesn't shift
        {
            return;
        }

        if (shift_coroutine != null)
        {
            StopCoroutine(shift_coroutine);
        }

        shift_coroutine = StartCoroutine(shift(pos));
    }

    //adjust the player prefab (bean) and tells SeatManager to adjust seat during a shift
    IEnumerator shift(int pos)
    {
        bool look_direction = transform.GetComponent<CameraMove>().camera_transform.localRotation.eulerAngles.y < 120;
        int new_seat_index = seat_manager.getShiftLocation(pos, look_direction);
        Vector3 start_pos = seat_manager.physical_seats[pos].transform.localPosition;
        Vector3 end_pos = new Vector3(SeatManager.SEAT_COORDINATES[pos][new_seat_index].x, start_pos.y, SeatManager.SEAT_COORDINATES[pos][new_seat_index].y);

        Vector3 offset = seat_manager.physical_seats[pos].transform.localPosition - transform.localPosition;

        float total_shift_time = Vector3.Distance(start_pos, end_pos) / SHIFT_SPEED;
        float shift_time = total_shift_time;

        while (shift_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            shift_time = Mathf.Max(0.0f, shift_time - dt);
            transform.localPosition = Vector3.Lerp(end_pos, start_pos, shift_time / total_shift_time) - offset;
            seat_manager.updateSeatLocation(pos, Vector3.Lerp(end_pos, start_pos, shift_time / total_shift_time));

            yield return null;
        }

        seat_manager.updateSeatIndex(pos, new_seat_index);

        shift_coroutine = null;
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