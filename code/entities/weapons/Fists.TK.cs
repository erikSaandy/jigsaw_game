using Saandy;
using Sandbox;
using System;

namespace Jigsaw;

[Spawnable]
[Library( "weapon_fists", Title = "Fists" )]
partial class Fists : Weapon
{

	void SimulateTK(IClient cl)
	{
		JigsawPawn pawn = Owner as JigsawPawn;
		if ( pawn.ActivePiece == null ) return;

		Angles yawDelta = Angles.Zero;

		if ( Input.Down( "use" ) )
		{
			yawDelta = new Angles( 0, -Input.MouseDelta.x * Time.Delta * 4, 0 );
			pawn.WantedAngleOffset += yawDelta;
		}

		if ( Game.IsServer )
		{
			using ( Prediction.Off() )
			{

				// Get wanted pos instead, based on all piewces...
				Vector3 velocity = GetWantedVelocity();

				pawn.PositionOld = pawn.ActivePiece.Position;
				pawn.PositionNew = Vector3.SmoothDamp( pawn.ActivePiece.Position, pawn.ActivePiece.Position + velocity, ref pawn.DampVelocity, pawn.SmoothTime, Time.Delta * 2 );
				velocity = pawn.PositionNew - pawn.PositionOld;
				pawn.ActivePiece.Velocity = velocity * 100;

				//rot

				UpdateHeldOffset( pawn.YawOld, pawn.ActivePiece.Rotation.Yaw() );

				pawn.YawOld = pawn.ActivePiece.Rotation.Yaw();
				float yawWanted = pawn.EyeRotation.Yaw() + pawn.WantedAngleOffset.yaw;
				float yaw = Math2d.SmoothDampAngle( pawn.YawOld, yawWanted, ref pawn.YawDampVelocity, 0.2f, Time.Delta * 2, Math2d.Infinity );

				float m = pawn.ActivePiece.PhysicsBody.Mass * Time.Delta;
				pawn.ActivePiece.PhysicsBody.AngularVelocity = new Vector3(
					-pawn.ActivePiece.Rotation.x * m,
					-pawn.ActivePiece.Rotation.y * m,
					(yaw - pawn.YawOld) );

			}

		}

		pawn.HoldSplashParticle?.SetPosition( 1, pawn.ActivePiece.Position - pawn.HeldOffset );

		if ( pawn.ActivePiece.TimeSincePickedUp > 0.5f )
		{
			if ( pawn.ActivePiece.TryConnecting( out PuzzlePiece neighbor ) )
			{
				if ( Game.IsServer )
				{
					PieceManager.ConnectRoots(Owner.Client, pawn.ActivePiece, neighbor );
					OnConnected( Client );
					PieceManager.OnConnected( Client );
				}

				return;
			}
		}

	}

	private void UpdateHeldOffset( float yawOld, float yawNew )
	{
		JigsawPawn pawn = Owner as JigsawPawn;
		float delta = (yawNew - yawOld);
		pawn.HeldOffset = Math2d.RotateByAngle( pawn.HeldOffset, -delta ) * pawn.HeldOffset.Length;
	}

	private Vector3 GetWantedVelocity()
	{
		JigsawPawn pawn = Owner as JigsawPawn;

		// The distance from eye to active piece expressed as a percentage of MaxHeldDistance.
		//float pOfDst = (ActivePiece.Position - EyePosition).Length / MaxHeldDistance;

		Vector3 start = pawn.EyePosition;

		// Trace from active piece out in the eye direction until we reach MaxHeldDistance from the player eye.
		TraceResult trace = Trace.Body( pawn.ActivePiece.PhysicsBody,
			new Transform( start, pawn.ActivePiece.Rotation ), // START
			pawn.EyePosition + (pawn.EyeRotation.Forward * JigsawPawn.MaxHeldDistance ) ) // END
				.WithAnyTags( pawn.CollisionTags )
				.Ignore( pawn.ActivePiece, true )
				.Ignore( this )
				.Run();

		if ( JigsawGame.Current.Debug )
			DebugOverlay.Sphere( trace.EndPosition, 5, Color.Green );

		//Vector3 up = (Vector3.Up * JigsawGame.PieceThickness * JigsawGame.PieceScale / 2);
		Vector3 r = trace.EndPosition - pawn.ActivePiece.Position + pawn.HeldOffset;

		return trace.EndPosition - pawn.ActivePiece.Position + pawn.HeldOffset;

	}

	public void OnConnected( IClient cl )
	{
		EnableTK( false );
		OnConnectedClient();
	}

	[ClientRpc]
	public void OnConnectedClient()
	{
		EnableTK( false );
	}

}

