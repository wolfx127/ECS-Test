using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager
{
    private MoveCameraInputAction inputAction;
    private Transform transform;
    private float moveSpeed = 70;
    private float rotateSpeed = 70;
    private float heightSpeed = 70;

    private float defaultHeight;

    private float yaw;
    private float pitch;

    public CameraManager(MoveCameraInputAction inputAction, Transform cameraTransform)
    {
        this.inputAction = inputAction;
        transform = cameraTransform;
        defaultHeight = cameraTransform.position.y;

        Vector3 e = cameraTransform.eulerAngles;
        yaw = e.y;
        pitch = Mathf.DeltaAngle(0f, e.x);

        inputAction.Enable();
        inputAction.Action.Enable();
        inputAction.Action.Move.Enable();
        inputAction.Action.Rotate.Enable();
    }

    public void Tick(float dt)
    {
        Move(dt);
        Rotate(dt);
        MoveHeight(dt);
    }

    private void Move(float dt)
    {
        Vector2 v = inputAction.Action.Move.ReadValue<Vector2>();

        Quaternion flat = Quaternion.Euler(0f, yaw, 0f);
        transform.position += flat * new Vector3(v.x, 0f, v.y) * (moveSpeed * dt);
    }

    private void Rotate(float dt)
    {
        Vector2 v = inputAction.Action.Rotate.ReadValue<Vector2>();

        pitch = Mathf.Clamp(pitch - v.y * rotateSpeed * dt, 0f, 90f);
        yaw += v.x * rotateSpeed * dt;

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void MoveHeight(float dt)
    {
        var mouse = Mouse.current;
        if (mouse == null) return; 
        
        float scroll = -mouse.scroll.ReadValue().y;
        if (scroll != 0f)
            transform.position += Vector3.up * (scroll * dt * heightSpeed);

        if (mouse.middleButton.wasPressedThisFrame)
        {
            Vector3 v = transform.position;
            v.y = defaultHeight;
            transform.position = v;
        }
    }
}
