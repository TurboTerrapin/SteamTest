/*
    CameraMove.cs
    - Handles pausing
    - Handles looking around
    - Handles camera zoom (using RMB)
    Contributor(s): John Aylward, Jake Schott
    Last Updated: 8/11/2025
*/

using System.Collections;
using UnityEngine;


public class CameraMove : MonoBehaviour
{
    private Vector2 mouseMove = new Vector2();
    private Vector2 prevPos = new Vector2(0f, 0);
    private Rigidbody rb = null;

    private float mouseSensitivity = 1f;
    public Camera my_camera;
    private float zoom_FOV = 40f;

    private void Start()
    {
        if (transform.parent.gameObject.GetComponent<PlayerMove>().IsOwner == false) //not owner, kill the camera
        {
            Destroy(gameObject);
        }

        my_camera = transform.GetComponent<Camera>();
        if (my_camera != null)
        {
            my_camera.gameObject.AddComponent<AudioListener>();
        }
    }

    //runs after scene is loaded and client matches
    public void initialize()
    {
        rb = transform.parent.gameObject.GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;

        ControlScript.Instance.my_camera = my_camera;

        StartCoroutine(cameraUpdater());
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

    //runs every frame after initialize() is called
    private void updateCamera()
    {
        //make sure we are the owner
        if (!transform.parent.gameObject.GetComponent<PlayerMove>().IsOwner) return;

        //handle pause/unpause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                ControlScript.Instance.pause();
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                ControlScript.Instance.unpause();
            }
        }

        //if not paused
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            MouseMove();
        }
        if (!ControlScript.Instance.isPaused())
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.Mouse1))
            {
                my_camera.fieldOfView = Mathf.Max(zoom_FOV, my_camera.fieldOfView -= 100f * Time.deltaTime);
                return;
            }
        }
        my_camera.fieldOfView = Mathf.Min(60f, my_camera.fieldOfView += 100f * Time.deltaTime);
    }

    void MouseMove()
    {
        Cursor.visible = false;
        //Gets mouse input
        mouseMove = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
 
        //Increases the sensitivity to movement
        mouseMove *= mouseSensitivity * Mathf.Min(1f, (1.1f - ((60f - my_camera.fieldOfView) / 20f)));

        prevPos.y = Mathf.Clamp(prevPos.y, -90f, 90f);

        prevPos.y -= mouseMove.y;
        prevPos.x += mouseMove.x;
        transform.localRotation = Quaternion.AngleAxis(prevPos.y, Vector3.right);
        transform.parent.localRotation = Quaternion.AngleAxis(prevPos.x, Vector3.up);
    }

    //used by settings
    public void SetMouseSensitvity(float newSensitivity)
    {
        mouseSensitivity = newSensitivity;
    }
}