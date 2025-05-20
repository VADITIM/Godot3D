using Godot;

public partial class Camera : Node
{
    [Export] private Camera3D playerCamera;
    [Export] private float mouseSensitivity = 0.002f;
    private float cameraXRotation = 0f;

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            HandleMouseLook(mouseMotion);
        }

        if (@event is InputEventKey keyEvent && keyEvent.Keycode == Key.Escape && keyEvent.Pressed && !keyEvent.Echo)
        {
            MouseCapture();
        }
    }
    
    private void MouseCapture()
    {
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
            Input.MouseMode = Input.MouseModeEnum.Visible;
        else
            Input.MouseMode = Input.MouseModeEnum.Captured;
    }
    
    private void HandleMouseLook(InputEventMouseMotion mouseMotion)
    {
        cameraXRotation -= mouseMotion.Relative.Y * mouseSensitivity;
        cameraXRotation = Mathf.Clamp(cameraXRotation, -Mathf.Pi / 2, Mathf.Pi / 2);
        playerCamera.Rotation = new Vector3(cameraXRotation, 0, 0);

        PlayerComponents.Instance.Player.player.RotateY(-mouseMotion.Relative.X * mouseSensitivity);
    }

}
