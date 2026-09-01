using Godot;
using System;

public partial class NavRegion : NavigationRegion3D
{
	public override void _Ready()
	{
		BakeNavigationMesh();
	}
}
