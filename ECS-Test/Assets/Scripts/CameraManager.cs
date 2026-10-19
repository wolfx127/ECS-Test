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
    
    public CameraManager(MoveCameraInputAction inputAction, Transform cameraTransform)
    {
        this.inputAction = inputAction;
        transform = cameraTransform;
        defaultHeight = cameraTransform.position.y;
        
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
        Quaternion flat = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        transform.position += flat * new Vector3(v.x, 0f, v.y) * (moveSpeed * dt);
    }

    private void Rotate(float dt)
    {
        Vector2 v = inputAction.Action.Rotate.ReadValue<Vector2>();
        //上下
        float current = Mathf.DeltaAngle(0f, transform.eulerAngles.x);
        float target  = Mathf.Clamp(current - v.y * rotateSpeed * dt, 0f, 90f);
        transform.Rotate(target - current, 0f, 0f, Space.Self);
        //左右
        transform.Rotate(0f, v.x * rotateSpeed * dt, 0f, Space.World);
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
