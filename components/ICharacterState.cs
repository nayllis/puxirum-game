using Godot;

public interface ICharacterState<T> where T : CharacterController
{
	void Enter(T c) { }
	void Exit(T c) { }
	void HandleInput(T c, InputEvent @event) { }
	void Update(T c, double delta) { }
	void Physics(T c, double delta) { }
}
