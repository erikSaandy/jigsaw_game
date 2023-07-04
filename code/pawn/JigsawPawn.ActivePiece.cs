using Sandbox;
using System.Linq;

namespace Jigsaw;

public partial class JigsawPawn : AnimatedEntity
{
	[Net]
	public PuzzlePiece ActivePiece { get; set; } = null;

	private readonly int ActivePieceRotationStep = 30;

	private float SmoothTime = 1f;
	private Vector3 dampVelocity = Vector3.Zero;
	private Vector3 PositionOld = Vector3.Zero;

	public Vector3 HeldOffset = Vector3.Zero;
	private readonly int MaxHeldDistance = 128;

	public void SimulateActivePiece( IClient cl )
	{

		if ( Game.IsClient ) return;

		PuzzlePieceInput();

		if ( ActivePiece == null ) return;

		#region Active Piece Position

		Vector3 pWanted = GetWantedPosition();
		DebugOverlay.Sphere( pWanted, 5, Color.Green );

		PositionOld = ActivePiece.Position;
		Vector3 pNew = Vector3.SmoothDamp( ActivePiece.Position, pWanted + HeldOffset, ref dampVelocity, SmoothTime, Time.Delta * 100 );
		Vector3 vel = pNew - PositionOld;
		ActivePiece.PhysicsBody.Velocity = vel * 10;

		//ActivePiece.Position = Math2d.Lerp( ActivePiece.Position, pWanted, Time.Delta * 10 );
		//ActivePiece.Position = EyePosition + (EyeRotation.Forward * 64); // + (EyeRotation.Right * 64);
		//ActivePiece.Position = pWanted;

		//float velY = ActivePiece.Velocity.y;
		//float velZ = ActivePiece.Velocity.z;
		//float X = Math2d.SmoothDamp( ActivePiece.Position.x, pWanted.x, ref smoothVelocityX, SmoothTime );
		//float Y = Math2d.SmoothDamp( ActivePiece.Position.y, pWanted.y, ref smoothVelocityY, SmoothTime );
		//float Z = Math2d.SmoothDamp( ActivePiece.Position.z, pWanted.z, ref smoothVelocityZ, SmoothTime );

		//ActivePiece.Position = new Vector3(X, Y, Z); 
		//Math2d.Lerp( ActivePiece.Position, pWanted, Time.Delta);
		//ActivePiece.Position = GetWantedPosition();

		#endregion

		// Rotate active piece
		if ( Input.Down( "attack2" ) && ActivePiece != null )
		{
			Vector2 deltaRot = new Vector2();
			if ( Input.Pressed( "Forward" ) ) deltaRot.y += ActivePieceRotationStep;
			if ( Input.Pressed( "Backward" ) ) deltaRot.y -= ActivePieceRotationStep;
			if ( Input.Pressed( "Right" ) ) deltaRot.x -= ActivePieceRotationStep;
			if ( Input.Pressed( "Left" ) ) deltaRot.x += ActivePieceRotationStep;

			Angles a = ActivePiece.Rotation.Angles();
			//ActivePiece.LocalRotation = new Angles( a.pitch + deltaRot.x, a.yaw + deltaRot.y, a.roll ).ToRotation();

			//ActivePiece.Rotation *= (Rotation)Quaternion.CreateFromAxisAngle( Vector3.Up, (a.pitch + deltaRot.x).DegreeToRadian() );
			//ActivePiece.Rotation *= (Rotation)Quaternion.CreateFromAxisAngle( ActivePiece.Rotation.Right, (a.yaw + deltaRot.y).DegreeToRadian() );

			//ActivePiece.Rotation = new Rotation( ActivePiece.Rotation.y + deltaRot.y, ActivePiece.Rotation.x + deltaRot.x, ActivePiece.Rotation.z, ActivePiece.Rotation.w );
		}

		if(ActivePiece.TimeSincePickedUp > 0.5f ) {
			ActivePiece.CheckForConnections();
		}

	}

	private Vector3 GetWantedPosition()
	{

		float radius = JigsawGame.PieceScale / 2;

		PuzzlePiece[] group = ActivePiece.GetGroupPieces().Where( x => x.IsValid ).ToArray();

		float maxDst = MaxHeldDistance;
		bool anyHit = false;

		// percent of max distance
		float pOfMaxDst = (ActivePiece.Position - EyePosition).Length / MaxHeldDistance;

		foreach ( PuzzlePiece p in group )
		{
			Vector3 start = p.Transform.Position - (EyeRotation.Forward * (MaxHeldDistance * pOfMaxDst));
			TraceResult trace = Trace.Body( p.PhysicsBody, new Transform( start, p.Rotation ), start + (EyeRotation.Forward * MaxHeldDistance) )
				.Ignore( ActivePiece, true )
				.Run();

			//DebugOverlay.TraceResult( trace );
			//DebugOverlay.Sphere( start, 4f, Color.Green, 10 );
			//DebugOverlay.Line( start, start + (EyeRotation.Forward * MaxHeldDistance), Color.Green, 10 );

			if ( trace.Hit )
			{
				if ( trace.Distance < maxDst || !anyHit )
				{
					maxDst = trace.Distance;
					anyHit = true;
				}
			}
		}

		TraceResult trace2 = Trace.Body( ActivePiece.PhysicsBody, new Transform( EyePosition, ActivePiece.Rotation ), EyePosition + (EyeRotation.Forward * maxDst) )
			.Ignore( ActivePiece, true )
			.Run();

		return trace2.EndPosition;
	}

	public void BuildActivePieceInput()
	{

	}

	private void PuzzlePieceInput()
	{
		if ( Input.StopProcessing )
			return;

		float rayMag = 256;

		if ( Game.IsClient ) return;

		if ( Input.Pressed( "attack1" ))
		{

			TraceResult tr = Trace.Ray(EyePosition, EyePosition + (EyeRotation.Forward * MaxHeldDistance) )
				.UseHitboxes()
				.WithTag("puzzlepiece")
				.Ignore(this)
				.Run();

			if (tr.Hit)
			{
				PuzzlePiece root = (tr.Entity as PuzzlePiece).GetRoot();
				HeldOffset = root.Position - tr.HitPosition;									
				SetActivePiece( root );
			}
		}

		// Throw piece
		else if(Input.Released("attack1"))
		{
			if ( ActivePiece != null )
			{
				//DebugOverlay.Line( ActivePiece.Position, ActivePiece.Position + ActivePiece.Position - PositionOld, Color.Red, 10 );

				ActivePiece.Velocity =((ActivePiece.Position) - PositionOld ) * 20;
				ClearActivePiece();
				return;
			}
		}


	}


	private void SetActivePiece( PuzzlePiece piece )
	{
		if ( Game.IsServer )
		{
			piece.Owner = this;
			ActivePiece = piece;
			piece.HeldBy = this;

			piece.PhysicsEnabled = true;
			piece.UsePhysicsCollision = true;
			piece.EnableAllCollisions = true;

			piece.TimeSincePickedUp = 0;

			Angles a = piece.LocalRotation.Angles();
			piece.LocalRotation = new Angles(
				a.pitch - (a.pitch % ActivePieceRotationStep),
				a.yaw - (a.yaw % ActivePieceRotationStep),
				a.roll - (a.roll % ActivePieceRotationStep)
				).ToRotation();

			EnableGroupGravity( piece, false );

			PositionOld = ActivePiece.Position;
		}

	}

	private void EnableGroupGravity(PuzzlePiece root, bool enable = true)
	{
		PuzzlePiece[] group = root.GetGroupPieces().Where( x => x.IsValid ).ToArray();
		foreach ( PuzzlePiece e in group )
		{
			PhysicsBody[] bodies = e.PhysicsGroup.Bodies.ToArray();
			foreach ( PhysicsBody b in bodies )
			{
				b.GravityEnabled = enable;
			}
		}
	}

	private void ClearActivePiece()
	{
		if ( Game.IsServer )
		{
			EnableGroupGravity( ActivePiece, true );
		}

		ActivePiece.UsePhysicsCollision = true;
		ActivePiece.EnableAllCollisions = true;

		//ActivePiece.Owner = null;
		ActivePiece = null;
	}

}
