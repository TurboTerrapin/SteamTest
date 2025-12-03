/*
    CameraMove.cs
    - Handles pausing
    - Handles looking around
    - Handles camera zoom (using RMB or CTRL)
    Contributor(s): John Aylward, Jake Schott
    Last Updated: 11/26/2025
*/

using System.Collections;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    private Vector2 mouseMove = new Vector2();
    private Vector2 prevPos = new Vector2(0.0f, 0.0f); //x represents angle of camera, y represents angle of player capsule
    private Rigidbody rb = null;

    private float mouseSensitivity = 1f;
    private Camera my_camera;
    public Transform camera_transform;
    public Transform head_transform;
    private float zoomFOV = 40f;
    private Coroutine cameraUpdateCoroutine = null;

    public bool parentRotationLock = false;
    public bool captainMode = false;

    private void Start()
    {
        if (transform.gameObject.GetComponent<PlayerMove>().IsOwner == false) //Not owner, kill the camera
        {
            Destroy(camera_transform.gameObject);
            Destroy(this);
        }

        //hide eyes, hair
        foreach (Transform t in head_transform)
        {
            t.gameObject.SetActive(false);
        }
        my_camera = camera_transform.GetComponent<Camera>();


        if (my_camera != null)
        {
            my_camera.gameObject.AddComponent<AudioListener>();
        }
    }

    //Runs after scene is loaded and client matches
    public void initialize()
    {
        rb = transform.gameObject.GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;

        cameraUpdateCoroutine = StartCoroutine(cameraUpdater());
    }

    public void unlockCamera(Vector2 initial_pos)
    {
        camera_transform.parent = transform;
        if (cameraUpdateCoroutine == null)
        {
            prevPos = initial_pos;
            cameraUpdateCoroutine = StartCoroutine(cameraUpdater());
        }
    }

    public void lockCamera()
    {
        if (cameraUpdateCoroutine != null)
        {
            StopCoroutine(cameraUpdateCoroutine);
            cameraUpdateCoroutine = null;
        }
    }

    public void deactivateCamera()
    {
        my_camera.gameObject.SetActive(false);
    }

    public void reactivateCamera()
    {
        faceWindow();
        my_camera.gameObject.SetActive(true);

        if (my_camera != null)
        {
            my_camera.fieldOfView = 60.0f;
        }

        if (cameraUpdateCoroutine != null)
        {
            StopCoroutine(cameraUpdateCoroutine);
        }
        cameraUpdateCoroutine = StartCoroutine(cameraUpdater());
    }

    //Called by FailureHandler on game restart
    public void resetCamera()
    {
        if (cameraUpdateCoroutine != null)
        {
            StopCoroutine(cameraUpdateCoroutine);
            cameraUpdateCoroutine = null;
        }
        prevPos = new Vector2(0.0f, 0.0f);
        camera_transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        rb.angularVelocity = Vector3.zero;
        if (my_camera != null)
        {
            my_camera.fieldOfView = 60.0f;
        }
    }

    private void faceWindow()
    {
        Transform window = GameObject.Find("WindowLookPoint").transform;
        if (window == null)
        {
            return;
        }

        transform.LookAt(window.position);
        camera_transform.LookAt(window.position);

        prevPos.x = transform.localRotation.eulerAngles.y;
        prevPos.y = camera_transform.localRotation.eulerAngles.x - (360.0f - transform.localRotation.eulerAngles.x);
        prevPos.y = Mathf.Clamp(prevPos.y, -90.0f, 90.0f);

        transform.localRotation = Quaternion.AngleAxis(prevPos.x, Vector3.up);
        camera_transform.localRotation = Quaternion.AngleAxis(prevPos.y, Vector3.right);
    }

    //calls updateCamera() every frame
    IEnumerator cameraUpdater()
    {
        while (true)
        {
            updateCamera();
            yield return null;
        }
    }

    //Runs every frame after initialize() is called
    private void updateCamera()
    {
        //Make sure we are the owner
        if (!transform.gameObject.GetComponent<PlayerMove>().IsOwner) return;

        //Handle pause/unpause
        if (Input.GetKeyDown(KeyCode.Escape) && ControlScript.Instance.canPause())
        {
            if (!ControlScript.Instance.isPaused())
            {
                ControlScript.Instance.pause();
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                ControlScript.Instance.unpause();
            }
        }

        //If not paused
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            MouseMove();
        }
        if (!ControlScript.Instance.isPaused())
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.Mouse1))
            {
                my_camera.fieldOfView = Mathf.Max(zoomFOV, my_camera.fieldOfView -= 100.0f * Time.deltaTime);
                return;
            }
        }
        my_camera.fieldOfView = Mathf.Min(60.0f, my_camera.fieldOfView += 100.0f * Time.deltaTime);
    }

    void MouseMove()
    {
        Cursor.visible = false;

        //Gets mouse input
        mouseMove = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); 

        //Increases the sensitivity to movement
        mouseMove *= mouseSensitivity * Mathf.Min(1.0f, (1.1f - ((60.0f - my_camera.fieldOfView) / 20.0f)));

        prevPos.x += mouseMove.x;
        prevPos.y -= mouseMove.y;

        if (!parentRotationLock) //Free roaming
        {
            prevPos.y = Mathf.Clamp(prevPos.y, -70.0f, 85.0f);

            transform.localRotation = Quaternion.AngleAxis(prevPos.x, Vector3.up);
            camera_transform.localRotation = Quaternion.AngleAxis(prevPos.y, Vector3.right);
        }
        else //Sitting down
        {
            prevPos.y = Mathf.Clamp(prevPos.y, -10.0f, 50.0f);

            if (captainMode == false)
            {
                prevPos.x = Mathf.Clamp(prevPos.x, -120.0f, 120.0f);
            }
            else
            {
                prevPos.x = Mathf.Clamp(prevPos.x, 100.0f, 260.0f);
            }

            camera_transform.localRotation = Quaternion.AngleAxis(prevPos.x, Vector3.up) * Quaternion.AngleAxis(prevPos.y, Vector3.right);
        }

        camera_transform.position = head_transform.position;
    }

    //Used by settings
    public void SetMouseSensitvity(float newSensitivity)
    {
        mouseSensitivity = newSensitivity;
    }
}