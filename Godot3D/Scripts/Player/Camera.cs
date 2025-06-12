using Godot;

public partial class Camera : Node
{
    [Export] private Camera3D playerCamera;
    [Export] private float mouseSensitivity = 0.002f;

    private float cameraXRotation = 0f;

    // Camera shake variables
    private float shakeIntensity;
    private float shakeDuration;
    private float shakeTimer;
    private Vector3 originalRotation;

    public override void _Ready()
    {
        originalRotation = playerCamera.Rotation;
    }

    public override void _Process(double delta)
    {
        ProcessShake(delta);
    }

    public void ShakeCamera(float intensity = 0.1f, float duration = 0.5f)
    {
        shakeIntensity = intensity;
        shakeDuration = duration;
        shakeTimer = duration;
    }

    private void ProcessShake(double delta)
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= (float)delta;

            float shakeOffsetX = (float)(GD.RandRange(-1.0, 1.0) * shakeIntensity);
            float shakeOffsetY = (float)(GD.RandRange(-1.0, 1.0) * shakeIntensity);

            Vector3 shakeOffset = new Vector3(shakeOffsetX, shakeOffsetY, 0);
            playerCamera.Rotation = new Vector3(cameraXRotation, 0, 0) + shakeOffset;

            shakeIntensity = Mathf.Lerp(shakeIntensity, 0, (float)delta * 5f);
        }
        else
        {
            playerCamera.Rotation = new Vector3(cameraXRotation, 0, 0);
        }
    }

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

        if (@event is InputEventKey keyEvent2 && keyEvent2.Keycode == Key.J && keyEvent2.Pressed && !keyEvent2.Echo)
        {
            ShakeCamera(.025f, 0.2f);
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

        if (shakeTimer <= 0)
            playerCamera.Rotation = new Vector3(cameraXRotation, 0, 0);

        Components.Instance.Player.rb.RotateY(-mouseMotion.Relative.X * mouseSensitivity);
    }
}
