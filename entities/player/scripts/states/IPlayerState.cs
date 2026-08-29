using Godot;

public interface IPlayerState
{
	public void Enter(PlayerController player){}
	public void Exit(PlayerController player){}
	public void HandleInput(PlayerController player, InputEvent @event){}
	public void Update(PlayerController player, double delta){}
	public void Physics(PlayerController player, double delta){}
}