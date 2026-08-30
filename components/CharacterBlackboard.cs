using Godot;

public enum CharacterGoal
{
	Idle,
	Follow,
	MoveTo,
	Occupy
}

public enum CharacterFail
{
	None,
	Unreachable,
	SlotTaken,
	NotEnoughCharacters
}

public class CharacterBlackboard
{
	public CharacterGoal goal = CharacterGoal.Idle;

	public Vector3 moveTarget;
	public bool hasMoveTarget;

	public Interactable interactable;
	public Node3D slot;
	public bool occupying;

	public bool allowFollowSettle;

	public bool IsBusy => goal == CharacterGoal.Occupy;

	public Node3D focus;
	public CharacterFail lastFail;

	public void SetToGo(Vector3 point)
	{
		ClearOccupy();
		goal = CharacterGoal.MoveTo;
		moveTarget = point;
		hasMoveTarget = true;
		lastFail = CharacterFail.None;
	}

	public void SetFollowTarget(Vector3 point)
	{
		if (goal == CharacterGoal.Occupy)
			return;

		goal = CharacterGoal.Follow;
		moveTarget = point;
		hasMoveTarget = true;
		lastFail = CharacterFail.None;
	}

	public void SetFollow()
	{
		ClearOccupy();
		goal = CharacterGoal.Follow;
		lastFail = CharacterFail.None;
	}

	public void SetOccupy(Node3D occupySlot, Interactable occupyInteractable = null)
	{
		if (occupySlot == null)
		{
			lastFail = CharacterFail.SlotTaken;
			return;
		}

		slot = occupySlot;
		interactable = occupyInteractable;
		occupying = false;
		goal = CharacterGoal.Occupy;
		moveTarget = occupySlot.GlobalPosition;
		hasMoveTarget = true;
		lastFail = CharacterFail.None;
	}

	public void ArriveOccupy()
	{
		occupying = true;
		hasMoveTarget = false;
	}

	public void ClearOccupy()
	{
		interactable = null;
		slot = null;
		occupying = false;
		if (goal == CharacterGoal.Occupy)
			goal = hasMoveTarget ? CharacterGoal.MoveTo : CharacterGoal.Follow;
	}

	public void Idle()
	{
		hasMoveTarget = false;
		ClearOccupy();
		goal = CharacterGoal.Follow;
	}
}