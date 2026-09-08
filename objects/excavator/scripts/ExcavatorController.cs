using Godot;
using System;

public partial class ExcavatorController : Node3D
{
    [Export] public Marker3D firstRotation;
    [Export] public Marker3D lastRotation;
    [Export] public Marker3D firstPosition;
    [Export] public Marker3D lastPosition;
    [Export] public float rotationSpeed = 1f;
    [Export] public Tween.TransitionType transitionType = Tween.TransitionType.Sine;
    [Export] public Tween.EaseType easeType = Tween.EaseType.InOut;
    [Export] public Interactable toggle;
    [Export] public Node3D cabin;
    [Export] public Marker3D armTarget;

    private float _firstRotAngle;
    private float _lastRotAngle;
    private float _targetRotAngle;
    private Tween _tweenRot;
    private Tween _tweenPos;

    private void NodeSetup()
    {
        if (firstRotation is not null)
        {
            firstRotation.TopLevel = true;
            _firstRotAngle = firstRotation.RotationDegrees.Y;
            _targetRotAngle = _firstRotAngle;
        }

        if (lastRotation is not null)
        {
            lastRotation.TopLevel = true;
            _lastRotAngle = lastRotation.RotationDegrees.Y;
        }

        if (toggle is not null)
        {
            toggle.SatisfiedChanged += OnToggle;
        }

        cabin ??= FindChild("Cabin") as Node3D;
        if (cabin is not null) cabin.RotationDegrees = new Vector3(0f, _firstRotAngle, 0f);
        if (armTarget is not null) armTarget.Position = firstPosition.Position;
    }

    public override void _Ready()
    {
        NodeSetup();
    }

    private void RotateTo(float targetAngle)
    {
        if (cabin is null) return;
        if (Mathf.IsEqualApprox(cabin.RotationDegrees.Y, targetAngle)) return;

        _tweenRot?.Kill();
        _tweenRot = CreateTween();
        _tweenRot.TweenProperty(cabin, "rotation_degrees:y", targetAngle, 1f / rotationSpeed).SetTrans(transitionType).SetEase(easeType);
    }
    private void MoveTo(Vector3 targetPosition)
    {
        if (cabin is null) return;
        if (cabin.Position.IsEqualApprox(targetPosition)) return;

        _tweenPos?.Kill();
        _tweenPos = CreateTween();
        _tweenPos.TweenProperty(armTarget, "position", targetPosition, 1f / rotationSpeed).SetTrans(transitionType).SetEase(easeType);
    }

    private void OnToggle(bool satisfied)
    {
        RotateTo(satisfied ? _lastRotAngle : _firstRotAngle);
        MoveTo(satisfied ? lastPosition.Position : firstPosition.Position);
    }
}
