using Godot;
using System;

public partial class UprightObject : Node3D
{
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        GlobalBasis = GameManager.GetUprightBasis(GlobalBasis);
    }

}
