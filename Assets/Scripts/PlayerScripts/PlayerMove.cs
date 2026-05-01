/*
    PlayerMove.cs
    - Names the player prefab to USERNAME_STEAMID if client, or OTHER_CLIENT if not
    - Handles player movement
    - Handles sitting down and getting up movement/animations
    - Handles shifting while seated
    - Enables collisions/rigidbody/gravity on the player character
    Contributor(s): John Aylward, Jake Schott
    Last Updated: 4/30/2026
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
    private static Vector2[] SEAT_PUSH_IN_ADJUSTMENTS = new Vector2[] { new Vector2(0.0f, 0.33f), new Vector2(0.0f, 0.33f), new Vector2(0.23f, -0.23f), Vector2.zero }; //pilot, tactician, engineer, captain 

    private Vector2 moveDir = new Vector2();
    [SerializeField]
    private Rigidbody playerRB = null;

    private Coroutine seatChangeCoroutine = null; //Used for sit down or get up animations
    private Coroutine shiftCoroutine = null; //Used for seat shifting
    private Coroutine moveCoroutine = null; //Used for movement checking
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

    public void Initialize()
    {
        seatManager = GameObject.FindGameObjectWithTag("SeatHandler").GetComponent<SeatManager>();

        if (moveCoroutine == null)
        {
            moveCoroutine = StartCoroutine(CheckForMovement());
        }
    }

    //called by FailureHandler.cs on game restart
    public void ResetPlayerMove()
    {
        ResetCoroutines();
        myAnimationController.setAnimatorBool("IsLeft", false);
        myAnimationController.setAnimatorInteger("Seat", 0);
        myAnimationController.setAnimatorFloat("Movement", 0.0f);
        myAnimationController.setAnimatorFloat("Forward", 0.0f);
        myAnimationController.setAnimatorBool("SittingDown", false);
        myAnimationController.setAnimatorBool("GettingUp", false);
        myAnimationController.setCharacterPosition(Vector3.zero);
    }

    //Called by ResetPlayerMove()
    private void ResetCoroutines()
    {
        StopAllCoroutines();
        moveCoroutine = null;
        seatChangeCoroutine = null;
        shiftCoroutine = null;
    }

    //Called by PrimaryScript.cs
    public void TriggerSitDownAnimation(int pos)
    {
        ResetCoroutines();
        seatChangeCoroutine = StartCoroutine(SitDownAnimation(pos));
    }

    //Called by PrimaryScript.cs
    public void TriggerGetUpAnimation(int pos)
    {
        ResetCoroutines();
        seatChangeCoroutine = StartCoroutine(GetUpAnimation(pos));
    }

    //Used to move the bean during a sit or get up animation, timed to match the in place animation to simulate movement
    IEnumerator PlayerAnimationTransformationAdjustment(Vector3 movePosition)
    {
        yield return new WaitForSeconds(1.0f);

        float animTime = 1.0f;
        Vector3 startPos = transform.localPosition;
        while (animTime > 0.0f)
        {
            animTime = Mathf.Max(animTime - Time.deltaTime, 0.0f);

            transform.localPosition = Vector3.Lerp(movePosition, startPos, animTime / 1.0f);

            yield return null;
        }
    }

    //Handles sit down sequence
    IEnumerator SitDownAnimation(int pos)
    {
        animator.transform.GetComponent<AnimatorHandler>().setIKActive(false);

        bool isLeft = seatManager.getSitDownDirection(pos, transform.position);
        myAnimationController.setAnimatorBool("IsLeft", isLeft);
        myAnimationController.setAnimatorInteger("Seat", pos);
        myAnimationController.setAnimatorFloat("Movement", 0.0f);
        myAnimationController.setAnimatorFloat("Forward", 0.0f);

        GameObject toOrient = seatManager.getSitDownPosition(pos, transform.position);
        yield return StartCoroutine(RepositionPlayer(toOrient.transform.localPosition + toOrient.transform.parent.localPosition, toOrient.transform.localRotation.eulerAngles.y, 0.2f));

        myAnimationController.setAnimatorBool("SittingDown", true);
        myAnimationController.setAnimatorBool("GettingUp", false); //Trigger sit down animation

        //If not captain, move the player DURING the animation because the animation happens in place
        if (pos != 3)
        {
            Vector3 endPos = seatManager.physical_seats[pos].transform.localPosition + seatManager.physical_seats[pos].transform.GetChild(2).localPosition;
            yield return StartCoroutine(PlayerAnimationTransformationAdjustment(endPos));
        }

        seatChangeCoroutine = null;
    }

    //Handles get up sequence
    IEnumerator GetUpAnimation(int pos)
    {
        Transform cameraHolder = transform.GetComponent<CameraMove>().cameraHolder;

        bool isLeft = seatManager.getGetUpDirection(pos);
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

        //If not captain, push seat back
        if (pos != 3)
        {
            SeatPush(pos, false);
            yield return shiftCoroutine;
        }

        cameraHolder.parent = transform.GetComponent<CameraMove>().headTransform;
        myAnimationController.setAnimatorBool("IsLeft", isLeft);
        myAnimationController.setAnimatorBool("GettingUp", true); //Trigger get up animation

        //If not captain, move the player DURING the animation because the animation happens in place
        if (pos != 3)
        {
            Vector3 endPos = seatManager.physical_seats[pos].transform.localPosition + seatManager.physical_seats[pos].transform.GetChild(0).localPosition;
            if (isLeft == true)
            {
                endPos = seatManager.physical_seats[pos].transform.localPosition + seatManager.physical_seats[pos].transform.GetChild(1).localPosition;
            }
            yield return StartCoroutine(PlayerAnimationTransformationAdjustment(endPos));
        }

        seatChangeCoroutine = null;
    }

    //Orients player and camera for sit down
    IEnumerator RepositionPlayer(Vector3 newPosition, float newRotation, float time)
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
    public bool IsShifting()
    {
        return (shiftCoroutine != null);
    }

    //Returns true if shifting or sitting/getting up
    public bool IsAnimating()
    {
        return (shiftCoroutine != null || seatChangeCoroutine != null);
    }

    //Called when shifting seat positions laterally (captain doesn't shift)
    public void SeatShift(int pos)
    {
        if (pos == 3) //Captain doesn't shift
        {
            return;
        }

        if (shiftCoroutine != null)
        {
            StopCoroutine(shiftCoroutine);
        }

        shiftCoroutine = StartCoroutine(Shift(pos));
        PrimaryScript.Instance.onShiftChange();
    }

    //Called when sitting down or getting up
    public void SeatPush(int pos, bool forward)
    {
        if (pos == 3) //Captain doesn't push
        {
            return;
        }

        if (shiftCoroutine != null)
        {
            StopCoroutine(shiftCoroutine);
        }

        shiftCoroutine = StartCoroutine(Push(pos, forward));
        PrimaryScript.Instance.onShiftChange();
    }

    //Pushes the player in or out during sit down or get up animations
    IEnumerator Push(int pos, bool forward)
    {
        seatManager.beginShift(pos);
        GameObject physicalSeat = seatManager.physical_seats[pos];
        if (physicalSeat == null)
        {
            yield break;
        }

        Vector3 personStartPos = transform.localPosition;
        Vector3 personEndPos = new Vector3(personStartPos.x + SEAT_PUSH_IN_ADJUSTMENTS[pos].x, personStartPos.y, personStartPos.z + SEAT_PUSH_IN_ADJUSTMENTS[pos].y);
        Vector3 seatStartPos = physicalSeat.transform.localPosition;
        Vector3 seatEndPos = new Vector3(seatStartPos.x + SEAT_PUSH_IN_ADJUSTMENTS[pos].x, seatStartPos.y, seatStartPos.z + SEAT_PUSH_IN_ADJUSTMENTS[pos].y);
        if (forward == false)
        {
            personEndPos = new Vector3(personStartPos.x - SEAT_PUSH_IN_ADJUSTMENTS[pos].x, personStartPos.y, personStartPos.z - SEAT_PUSH_IN_ADJUSTMENTS[pos].y);
            seatEndPos = new Vector3(seatStartPos.x - SEAT_PUSH_IN_ADJUSTMENTS[pos].x, seatStartPos.y, seatStartPos.z - SEAT_PUSH_IN_ADJUSTMENTS[pos].y);
        }

        float animTime = 0.5f;
        while (animTime > 0.0f)
        {
            animTime = Mathf.Max(0.0f, animTime - Time.deltaTime);

            transform.localPosition = Vector3.Lerp(personEndPos, personStartPos, animTime / 0.5f);
            if (physicalSeat != null)
            {
                physicalSeat.transform.localPosition = Vector3.Lerp(seatEndPos, seatStartPos, animTime / 0.5f);
            }

            yield return null;
        }

        shiftCoroutine = null;
        PrimaryScript.Instance.onShiftChange();
    }

    //Adjust the player prefab (bean) and tells SeatManager to adjust seat during a shift
    IEnumerator Shift(int pos)
    {
        bool lookDirection = transform.GetComponent<CameraMove>().cameraHolder.localRotation.eulerAngles.y < 120;
        int newSeatIndex = seatManager.getShiftLocation(pos, lookDirection);
        Vector3 pushDir = new Vector3(SEAT_PUSH_IN_ADJUSTMENTS[pos].x, 0.0f, SEAT_PUSH_IN_ADJUSTMENTS[pos].y);
        Vector3 startPos = seatManager.physical_seats[pos].transform.localPosition;
        Vector3 endPos = new Vector3(SeatManager.SEAT_COORDINATES[pos][newSeatIndex].x, startPos.y, SeatManager.SEAT_COORDINATES[pos][newSeatIndex].y) + pushDir;

        Vector3 offset = seatManager.physical_seats[pos].transform.localPosition - transform.localPosition;

        float totalShiftTime = Vector3.Distance(startPos, endPos) / SHIFT_SPEED;
        float shiftTime = totalShiftTime;

        seatManager.beginShift(pos);

        while (shiftTime > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            shiftTime = Mathf.Max(0.0f, shiftTime - dt);
            transform.localPosition = Vector3.Lerp(endPos, startPos, shiftTime / totalShiftTime) - offset;
            if (seatManager.physical_seats[pos] != null)
            {
                seatManager.physical_seats[pos].transform.localPosition = Vector3.Lerp(endPos, startPos, shiftTime / totalShiftTime);
            }

            yield return null;
        }

        seatManager.updateSeatIndex(pos, newSeatIndex);

        shiftCoroutine = null;
        PrimaryScript.Instance.onShiftChange();
    }

    //Runs on Update() time
    IEnumerator CheckForMovement()
    {
        while (true)
        {
            yield return null;
            UpdateMovement();
        }
    }

    //Checks inputs and triggers move
    private void UpdateMovement()
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

        //Teleport back if you fall
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