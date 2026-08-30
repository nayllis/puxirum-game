using Godot;

public partial class HoldInteractable : Interactable
{
	public override bool OnCommandWhenFull()
	{
		ReleaseAll();
		return true;
	}
}
