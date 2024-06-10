using Godot;

namespace R2R;
public partial class Enemy : AnimatedSprite2D {
  Game game;
	RandomNumberGenerator rnd = new();
	bool goingLeft;
	Vector2 direction;
	float Speed;
	EnemyType type;
	bool completedAction = false;
	bool fleeing = false;
	double catchTime = 0;
	internal void Spawn(Game game, EnemyType enemyType) {
		this.game = game;
		type = enemyType;
		if (rnd.Randf() < .5f) goingLeft = true;

		GlobalPosition = new(goingLeft ? 2064 : -155, 462);
		Scale = (goingLeft ? Vector2.Left : Vector2.Right) + Vector2.Down;
		direction = goingLeft ? Vector2.Left : Vector2.Right;

		if (enemyType == EnemyType.Police) Speed = rnd.RandfRange(150f, 250f);
		else if (enemyType == EnemyType.Orban) Speed = rnd.RandfRange(100f, 150f);
		else Speed = rnd.RandfRange(200f, 275f);
  }


	bool wasInsideCrossroad = false;

	public void ProcessEnemy(double delta, Vector2[] crossroads, float streetX) {
		if (!completedAction) {
			catchTime += delta;
			if (catchTime > 30) StopAndFlee();
		}

    switch (type) {
			case EnemyType.Police:
				ProcessPolice(delta, crossroads, streetX);
				break;
			case EnemyType.DrunkGuy:
				ProcessDrunkGuy(delta);
				break;
      case EnemyType.Robber:
      case EnemyType.Maga:
			case EnemyType.ZTurd:
			case EnemyType.Orban:
        ProcessRobber(delta, crossroads, streetX);
				break;
		}
	}


	public void StopAndFlee() {
		if (fleeing) return;
		// Go back and flee
		fleeing = true;
    completedAction = true;
    goingLeft = !goingLeft;
    direction = goingLeft ? Vector2.Left : Vector2.Right;
    Scale = (goingLeft ? Vector2.Left : Vector2.Right) + Vector2.Down;
    if (type == EnemyType.Robber) Play("Flee");
  }


  void ProcessPolice(double delta, Vector2[] crossroads, float streetX) {
		if (Speed == 0) return;
		GlobalPosition += (float)(Speed * delta) * direction;

		if (goingLeft && GlobalPosition.X < -144) {
			game.EnemyGone();
			Speed = 0;
		}
		else if (!goingLeft && GlobalPosition.X > 2064) {
			game.EnemyGone();
			Speed = 0;
		}

		float npcX = GlobalPosition.X;
		bool inside = false;
		for (int i = 0; i < crossroads.Length; i++) {
			if (npcX > crossroads[i].X + streetX && npcX < crossroads[i].Y + streetX) {
				inside = true;
				break;
			}
		}
		if (inside && !wasInsideCrossroad) {
			Position += Vector2.Down * 30;
			wasInsideCrossroad = true;
		}
		else if (!inside && wasInsideCrossroad) {
			Position -= Vector2.Down * 30;
			wasInsideCrossroad = false;
		}


		// After a while we should stop and go back (but depends on the type, policemens may just continue ignoring you)
		if (Mathf.Abs(GlobalPosition.X - game.Player.GlobalPosition.X) < 50 && game.status != Game.Status.Win) {
			if (game.sleepingOnBench) {
				game.JailTime("You were jailed because sleeping on bench is illegal");
				game.EnemyGone();
			}
			if (game.currentRoad == game.TopBoulevard) {
				int bad = 0;
				if (game.clothes == Clothes.Rags) bad++;
				if (game.totalSmell > 80) bad++;
				if (game.beard > 80) bad++;
				if (bad >= 2) {
					game.JailTime("You were too stinky to walk on Top Boluvard!");
					game.EnemyGone();
				}
			}
			if (game.currentRoad == game.NorthRoad) {
				int bad = 0;
				if (game.clothes == Clothes.Rags) bad++;
				if (game.totalSmell > 80) bad++;
				if (game.beard > 80) bad++;
				if (bad == 3) {
					game.JailTime("You were too stinky to walk on North Road!");
					game.EnemyGone();
				}
			}
		}
	}

	double drunkGuyTime = 100;
	void ProcessDrunkGuy(double delta) {
		if (!Visible) return;
		// Consume its time to live, when it is completed, make it disappear when the player is away
		drunkGuyTime -= delta;
		if (drunkGuyTime < 0 && game.Player.GlobalPosition.DistanceSquaredTo(GlobalPosition) > 2000) {
			game.SpawnBottlesForDrunkGuy();
			drunkGuyTime = rnd.RandfRange(80, 200);
			return;
		}
	}

  void ProcessRobber(double delta, Vector2[] crossroads, float streetX) {
    if (Speed == 0) return;
    GlobalPosition += (float)(Speed * delta) * direction;

    if (goingLeft && GlobalPosition.X < -144) {
      game.EnemyGone();
      Speed = 0;
    }
    else if (!goingLeft && GlobalPosition.X > 2064) {
      game.EnemyGone();
      Speed = 0;
    }

    float npcX = GlobalPosition.X;
    bool inside = false;
    for (int i = 0; i < crossroads.Length; i++) {
      if (npcX > crossroads[i].X + streetX && npcX < crossroads[i].Y + streetX) {
        inside = true;
        break;
      }
    }
    if (inside && !wasInsideCrossroad) {
      Position += Vector2.Down * 30;
      wasInsideCrossroad = true;
    }
    else if (!inside && wasInsideCrossroad) {
      Position -= Vector2.Down * 30;
      wasInsideCrossroad = false;
    }


    // After a while we should stop and go back (but depends on the type, policemens may just continue ignoring you)
    if (!completedAction && Mathf.Abs(GlobalPosition.X - game.Player.GlobalPosition.X) < 50) {
			completedAction = true;
			// Is the player in an unreacheable status?
			if (game.IsPlayerReacheable()) {
				// Rob the money
				game.RobPlayer(type);
			}
			// Go back and flee
      goingLeft = !goingLeft;
      direction = goingLeft ? Vector2.Left : Vector2.Right;
      Scale = (goingLeft ? Vector2.Left : Vector2.Right) + Vector2.Down;
			if (type == EnemyType.Robber) Play("Flee");
    }
  }

}

public enum EnemyType { None, Police, DrunkGuy, Robber, Maga, ZTurd, Orban };