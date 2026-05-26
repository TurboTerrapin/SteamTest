/*
    CameraMove.cs
    - Handles pausing
    - Handles looking around
    - Handles camera zoom (using RMB or CTRL)
    - Handles camera shaking
    - Handles displaying hints if hints enabled (ex. MISSION OBJECTIVE, POWER MONITORING)
    Contributor(s): John Aylward, Jake Schott
    Last Updated: 4/26/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    //CLASS CONSTANTS
    private static bool HIDE_HAIR_AND_EYES = true; //Hides character's hair/eyes locally
    private static bool CAMERA_SHAKE_POSSIBLE = true; //Used for testing
    private static float MAXIMUM_CAMERA_SHAKE = 0.015f;
    private static float ZOOMED_FOV = 40.0f;
    private static float DEFAULT_FOV = 60.0f;
    private static Vector2[] SITTING_CAMERA_HORIZONTAL_RANGES = new Vector2[] { new Vector2(-120.0f, 120.0f), new Vector2(-80.0f, 80.0f) }; //Non-captain and captain

    public Transform cameraHolder;
    public Transform headTransform;
    public bool parentRotationLock = false;
    private Camera myCamera;
    private Rigidbody rb;
    private AnimatorHandler animatorHandler = null;

    private bool cameraLocked = true; //If true, means camera cannot be moved with mouse
    private Vector2 mouseMove = new Vector2();
    private Vector2 prevPos = new Vector2(0.0f, 0.0f); //X represents angle of camera, Y represents angle of player capsule
    private Vector2 sittingHorizontalRange = SITTING_CAMERA_HORIZONTAL_RANGES[0];
    private Vector3 cameraOffset = Vector3.zero; //Offset (for camera shake)
    private float mouseSensitivity = 1.0f;
    private List<Vector2> cameraShakeEffects = new List<Vector2>(); //Any current camera shake effects (X = time remaining, Y = intensity)
    private float cameraShakeIntensity = 0.0f; //Ranges from 0-1, 1 being full shake

    private void Start()
    {
        if (transform.gameObject.GetComponent<PlayerMove>().IsOwner == false) //Not owner, kill the camera
        {
            Destroy(cameraHolder.gameObject);
            Destroy(this);
        }

        //Hide eyes, hair
        if (HIDE_HAIR_AND_EYES == true)
        {
            foreach (Transform t in headTransform.parent)
            {
                if (t != headTransform)
                {
                    t.gameObject.SetActive(false);
                }
            }
        }

        myCamera = cameraHolder.transform.GetChild(0).GetComponent<Camera>();
        if (myCamera != null)
        {
            myCamera.gameObject.AddComponent<AudioListener>();
        }

        animatorHandler = transform.Find("CharacterModel").GetComponent<AnimatorHandler>();
    }

    //Runs after scene is loaded
    public void Initialize()
    {
        rb = transform.gameObject.GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        cameraLocked = false;

        StartCoroutine(CameraUpdater());
    }

    public void UnlockCamera(Vector2 initialPos)
    {
        cameraHolder.parent = transform;
        cameraLocked = false;
        prevPos = initialPos;
    }

    public void LockCamera()
    {
        cameraLocked = true;
    }

    public void DeactivateCamera()
    {
        myCamera.gameObject.SetActive(false);
    }

    public void ReactivateCamera()
    {
        cameraShakeEffects.Clear();
        FaceWindow();
        myCamera.gameObject.SetActive(true);

        if (myCamera != null)
        {
            myCamera.fieldOfView = DEFAULT_FOV;
        }
        cameraLocked = false;
    }

    public void SetCaptainMode(bool captain)
    {
        if (captain == true)
        {
            sittingHorizontalRange = SITTING_CAMERA_HORIZONTAL_RANGES[1];
        }
        else
        {
            sittingHorizontalRange = SITTING_CAMERA_HORIZONTAL_RANGES[0];
        }
    }

    //Called by FailureHandler.cs on game restart
    public void ResetCamera()
    {
        StopAllCoroutines();
        parentRotationLock = false;
        cameraLocked = false;
        prevPos = new Vector2(0.0f, 0.0f);
        cameraShakeEffects.Clear();
        cameraShakeIntensity = 0.0f;
        cameraHolder.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        rb.angularVelocity = Vector3.zero;
        if (myCamera != null)
        {
            myCamera.fieldOfView = DEFAULT_FOV;
        }
    }

    //Faces window if standing
    private void FaceWindow()
    {
        if (parentRotationLock == true)
        {
            return;
        }

        Transform window = GameObject.Find("WindowLookPoint").transform;
        if (window == null)
        {
            return;
        }

        transform.LookAt(window.position);
        cameraHolder.LookAt(window.position);

        prevPos.x = transform.localRotation.eulerAngles.y % 360.0f;
        prevPos.y = Mathf.Clamp(cameraHolder.localRotation.eulerAngles.x, -90.0f, 90.0f);

        transform.localRotation = Quaternion.AngleAxis(prevPos.x, Vector3.up);
        cameraHolder.localRotation = Quaternion.AngleAxis(prevPos.y, Vector3.right);
    }

    //Calls UpdateCamera() every frame
    IEnumerator CameraUpdater()
    {
        float cameraShakeDelay = 0.0f;
        while (true)
        {
            cameraShakeDelay += Time.deltaTime;
            if (CAMERA_SHAKE_POSSIBLE == true)
            {
                //Update camera shake every 0.05s
                if (cameraShakeDelay > 0.05f)
                {
                    //Adjust camera shake effects
                    for (int i = cameraShakeEffects.Count - 1; i >= 0; i--)
                    {
                        float time_remaining = Mathf.Max(0.0f, cameraShakeEffects[i].x - cameraShakeDelay);
                        if (time_remaining <= 0.0f)
                        {
                            cameraShakeEffects.RemoveAt(i);
                        }
                        else
                        {
                            cameraShakeEffects[i] = new Vector2(time_remaining, cameraShakeEffects[i].y);
                        }
                    }

                    //Pick most intense camera effect
                    float max_intensity = 0.0f;
                    for (int i = 0; i < cameraShakeEffects.Count; i++)
                    {
                        if (cameraShakeEffects[i].y > max_intensity)
                        {
                            max_intensity = cameraShakeEffects[i].y;
                        }
                    }

                    cameraShakeIntensity = Mathf.MoveTowards(cameraShakeIntensity, max_intensity, 0.05f);
                    cameraOffset = Random.insideUnitSphere * cameraShakeIntensity * MAXIMUM_CAMERA_SHAKE;
                    cameraShakeDelay = 0.0f;
                }
            }

            UpdateCamera();
            yield return null;
        }
    }

    //Runs every frame after Initialize() is called
    private void UpdateCamera()
    {
        //Reposition camera to offset value (for shaking)
        cameraHolder.transform.GetChild(0).localPosition = cameraOffset;

        //Handle pause/unpause
        if (Input.GetKeyDown(KeyCode.Escape) && PrimaryScript.Instance.canPause())
        {
            if (!PrimaryScript.Instance.isPaused())
            {
                PrimaryScript.Instance.pause();
            }
            else
            {
                PrimaryScript.Instance.unpause();
            }
        }
        
        //If not paused
        if (Cursor.lockState == CursorLockMode.Locked && cameraLocked == false)
        {
            MouseMove();
        }

        //Check for pausing/hints toggling
        if (!PrimaryScript.Instance.isPaused())
        {
            //Check for info overlay toggling (hints)
            if (PrimaryScript.Instance.getHUD() < 2 && PrimaryScript.Instance.infoVisibilityEnabled() && PrimaryScript.Instance.isActive())
            {
                PrimaryScript.Instance.GetComponent<SecondaryScript>().checkInfoOverlayInputs(false);
            }

            if (cameraLocked == false)
            {
                //Zoom in
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.Mouse1))
                {
                    myCamera.fieldOfView = Mathf.Max(ZOOMED_FOV, myCamera.fieldOfView -= 100.0f * Time.deltaTime);
                    return;
                }
            }
        }
        else
        {
            //Freeze rotation to prevent infinite spinning
            rb.angularVelocity = Vector3.zero;
        }

        //Zoom out
        myCamera.fieldOfView = Mathf.Min(DEFAULT_FOV, myCamera.fieldOfView += 100.0f * Time.deltaTime);
    }

    private void MouseMove()
    {
        Cursor.visible = false;

        //Gets mouse input
        mouseMove = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); 

        //Sensitivity changes based on zoom amount
        mouseMove *= mouseSensitivity * Mathf.Min(1.0f, (1.1f - ((DEFAULT_FOV - myCamera.fieldOfView) / (DEFAULT_FOV - ZOOMED_FOV))));

        prevPos.x += mouseMove.x;
        prevPos.y -= mouseMove.y;

        if (!parentRotationLock) //Free roaming
        {
            prevPos.y = Mathf.Clamp(prevPos.y, -70.0f, 85.0f);

            transform.localRotation = Quaternion.AngleAxis(prevPos.x, Vector3.up);
            cameraHolder.localRotation = Quaternion.AngleAxis(prevPos.y, Vector3.right);
        }
        else //Sitting down
        {
            prevPos.y = Mathf.Clamp(prevPos.y, -10.0f, 50.0f);
            prevPos.x = Mathf.Clamp(prevPos.x, sittingHorizontalRange.x, sittingHorizontalRange.y);

            cameraHolder.localRotation = Quaternion.AngleAxis(prevPos.x, Vector3.up) * Quaternion.AngleAxis(prevPos.y, Vector3.right);

            if(PrimaryScript.Instance.currentSeat() == 3)
            {
                animatorHandler.chestlookat = 0;
                //animatorHandler.chestlookat = -Mathf.Abs(prevPos.x / 180) + 1;
            }
            else
            {
                animatorHandler.chestlookat = Mathf.Abs(prevPos.x / 180);
            }

                animatorHandler.chestlookat *= (prevPos.y + 10) / 100;
        }

        cameraHolder.position = headTransform.position;
    }

    //Shakes camera based on time and intensity (can have multiple shakes at once)
    public void ShakeCamera(float time, float intensity)
    {
        intensity = Mathf.Clamp01(intensity);
        cameraShakeEffects.Add(new Vector2(time, intensity));
    }

    //Used by settings
    public void SetMouseSensitvity(float newSensitivity)
    {
        mouseSensitivity = newSensitivity;
    }
}