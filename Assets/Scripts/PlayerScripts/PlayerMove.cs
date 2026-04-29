/*
    PlayerMove.cs
    - Names the player prefab to USERNAME_STEAMID if client, or OTHER_CLIENT if not
    - Handles player movement
    - Handles seating/unseating teleporting
    - Handles shifting while seated
    - Enables collisions/rigidbody/gravity on the player character
    Contributor(s): John Aylward, Jake Schott
    Last Updated: 4/26/2026
*/

using System.Collections;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

public class PlayerMove : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static float SHIFT_SPEED = 1.5f;
    private static float MOVE_SPEED = 5.0f;

    private Vector2 moveDir = new Vector2();
    [SerializeField]
    private Rigidbody playerRB = null;

    private Coroutine seatChangeCoroutine = null;
    private Coroutine shiftCoroutine = null;
    private Coroutine moveCoroutine = null;
    private Coroutine repositionCoroutine = null;
    private SeatManager seatManager = null;

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
        seatManager = GameObject.FindGameObjectWithTag("SeatHandler").GetComponent<SeatManager>();

        if (moveCoroutine == null)
        {
            moveCoroutine = StartCoroutine(checkForMovement());
        }
    }

    //called by FailureHandler.cs on game restart
    public void resetPlayerMove()
    {
        resetCoroutines();
        myAnimationController.setAnimatorBool("IsLeft", false);
        myAnimationController.setAnimatorInteger("Seat", 0);
        myAnimationController.setAnimatorFloat("Movement", 0.0f);
        myAnimationController.setAnimatorFloat("Forward", 0.0f);
        myAnimationController.setAnimatorBool("SittingDown", false);
        myAnimationController.setAnimatorBool("GettingUp", false);
        myAnimationController.setCharacterPosition(Vector3.zero);
    }

    //called by resetPlayerMove()
    private void resetCoroutines()
    {
        StopAllCoroutines();
        moveCoroutine = null;
        seatChangeCoroutine = null;
        shiftCoroutine = null;
        repositionCoroutine = null;
    }

    //called by PrimaryScript.cs
    public void sitDown(int pos)
    {
        resetCoroutines();
        seatChangeCoroutine = StartCoroutine(sitDownSequence(pos));
    }

    //Handles sit down sequence
    IEnumerator sitDownSequence(int pos)
    {
        animator.transform.GetComponent<AnimatorHandler>().setIKActive(false);
        GameObject to_orient = seatManager.getSitDownPosition(pos, transform.position);
        myAnimationController.setAnimatorBool("IsLeft", seatManager.getSitDownDirection(pos, transform.position));
        myAnimationController.setAnimatorInteger("Seat", pos);
        myAnimationController.setAnimatorFloat("Movement", 0.0f);
        myAnimationController.setAnimatorFloat("Forward", 0.0f);
        repositionCoroutine = StartCoroutine(repositionPlayer(to_orient.transform.localPosition + to_orient.transform.parent.localPosition, to_orient.transform.localRotation.eulerAngles.y, 0.2f));

        yield return repositionCoroutine;
        repositionCoroutine = null;

        animator.applyRootMotion = (pos != 3);
        myAnimationController.setAnimatorBool("SittingDown", true);
        myAnimationController.setAnimatorBool("GettingUp", false); //Trigger sit down animation
    }

    public void getUp(int pos)
    {
        resetCoroutines();
        seatChangeCoroutine = StartCoroutine(getUpSequence(pos));
    }

    //Orients camera for get up
    IEnumerator getUpSequence(int pos)
    {
        Transform cameraHolder = transform.GetComponent<CameraMove>().cameraHolder;

        myAnimationController.setIKActive(false);
        transform.GetComponent<CameraMove>().LockCamera();

        Quaternion startingRotation = cameraHolder.localRotation;

        float animTime = 0.15f;
        while (animTime > 0.0f)
        {
            animTime = Mathf.Max(0.0f, animTime - Time.deltaTime);

            cameraHolder.localRotation = Quaternion.Lerp(Quaternion.Euler(30.0f, 0.0f, 0.0f), startingRotation, animTime / 0.15f);
            cameraHolder.position = transform.GetComponent<CameraMove>().headTransform.position;

            yield return null;
        }

        cameraHolder.parent = transform.GetComponent<CameraMove>().headTransform;
        myAnimationController.setAnimatorBool("IsLeft", seatManager.getGetUpDirection(pos));
        myAnimationController.setAnimatorBool("GettingUp", true); //Trigger get up animation
    }

    //Orients player and camera for sit down
    IEnumerator repositionPlayer(Vector3 newPosition, float newRotation, float time)
    {
        Transform cameraHolder = transform.GetComponent<CameraMove>().cameraHolder;

        Vector3 startingPosition = transform.localPosition;
        float startingRotation = transform.localRotation.eulerAngles.y;
        float startingCamRotation = cameraHolder.localRotation.eulerAngles.x;
        if (newRotation == 0.0f && startingRotation > 180.0f)
        {
            startingRotation = 0.0f - (360.0f - startingRotation);
        }

        float animTime = time;
        while (animTime > 0.0f)
        {
            animTime = Mathf.Max(0.0f, animTime - Time.deltaTime);

            transform.localPosition = Vector3.Lerp(newPosition, startingPosition, animTime / time);
            transform.localRotation = Quaternion.Euler(0.0f, Mathf.Lerp(newRotation, startingRotation, animTime / time), 0.0f);
            cameraHolder.localRotation = Quaternion.Euler(Mathf.Lerp(30.0f, startingCamRotation, animTime / time), 0.0f, 0.0f);
            cameraHolder.position = transform.GetComponent<CameraMove>().headTransform.position;

            yield return null;
        }
    }

    //Returns true if currently shifting
    public bool isShifting()
    {
        return (shiftCoroutine != null);
    }

    //Returns true if shifting or sitting/getting up
    public bool isAnimating()
    {
        return (shiftCoroutine != null || seatChangeCoroutine != null);
    }

    public void seatShift(int pos)
    {
        if (pos == 3) //Captain doesn't shift
        {
            return;
        }

        if (shiftCoroutine != null)
        {
            StopCoroutine(shiftCoroutine);
        }

        shiftCoroutine = StartCoroutine(shift(pos));
        PrimaryScript.Instance.onShiftChange();
    }

    //Adjust the player prefab (bean) and tells SeatManager to adjust seat during a shift
    IEnumerator shift(int pos)
    {
        bool look_direction = transform.GetComponent<CameraMove>().cameraHolder.localRotation.eulerAngles.y < 120;
        int new_seat_index = seatManager.getShiftLocation(pos, look_direction);
        Vector3 start_pos = seatManager.physical_seats[pos].transform.localPosition;
        Vector3 end_pos = new Vector3(SeatManager.SEAT_COORDINATES[pos][new_seat_index].x, start_pos.y, SeatManager.SEAT_COORDINATES[pos][new_seat_index].y);

        Vector3 offset = seatManager.physical_seats[pos].transform.localPosition - transform.localPosition;

        float total_shift_time = Vector3.Distance(start_pos, end_pos) / SHIFT_SPEED;
        float shift_time = total_shift_time;

        seatManager.beginShift(pos);

        while (shift_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            shift_time = Mathf.Max(0.0f, shift_time - dt);
            transform.localPosition = Vector3.Lerp(end_pos, start_pos, shift_time / total_shift_time) - offset;
            if (seatManager.physical_seats[pos] != null)
            {
                seatManager.physical_seats[pos].transform.localPosition = Vector3.Lerp(end_pos, start_pos, shift_time / total_shift_time);
            }

            yield return null;
        }

        seatManager.updateSeatIndex(pos, new_seat_index);

        shiftCoroutine = null;
        PrimaryScript.Instance.onShiftChange();
    }

    //Runs on Update() time
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

        if (!PrimaryScript.Instance.isPaused())
        {
            moveDir.x = Input.GetAxis("Horizontal");
            moveDir.y = Input.GetAxis("Vertical");
        }
        else
        {
            moveDir.x = 0.0f;
            moveDir.y = 0.0f;
        }

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

    private void Move()
    {
        Vector3 movement; //= Vector3.zero;

        myAnimationController.setAnimatorFloat("Movement", moveDir.magnitude);
        myAnimationController.setAnimatorFloat("Forward", moveDir.y);

        if (transform.parent != null) //Local movement
        {
            Quaternion combinedRotation = transform.parent.rotation * transform.localRotation;
            Vector3 localMovement = new Vector3(moveDir.x, 0, moveDir.y) * MOVE_SPEED * Time.deltaTime;
            movement = combinedRotation * localMovement;
            transform.position += movement;
        }
        else //World movement
        {
            movement = transform.TransformDirection(new Vector3(moveDir.x, 0, moveDir.y)) * MOVE_SPEED * Time.deltaTime;
            transform.position += movement;
        }
    }
}