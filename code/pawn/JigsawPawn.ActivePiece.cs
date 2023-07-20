using Saandy;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jigsaw;

public partial class JigsawPawn : AnimatedEntity
{
	private readonly string[] CollisionTags = { "default", "solid", "player" };

	private readonly string ConnectSound = "sounds/piece_connections/piece_connect.sound";

	[Net, Predicted] public PuzzlePiece ActivePiece { get; set; } = null;

	private readonly float SmoothTime = 0.5f;
	private Vector3 DampVelocity = Vector3.Zero;
	[Net] private Vector3 PositionOld { get; set; } = Vector3.Zero;
	[Net] private Vector3 PositionNew { get; set; } = Vector3.Zero;
	[Net, Predicted] public Vector3 PieceVelocity { get; set; } = Vector3.Zero;
	[Net] public Vector3 HeldOffset { get; set; } = Vector3.Zero;

	private readonly float YawSmoothTime = 0.5f;
	private float YawDampVelocity = 0;
	[Net] public Angles WantedAngleOffset { get; set; } = Angles.Zero;

	[Net] public float YawOld { get; set; } = 0;

	[Predicted] Particles HoldSplashParticle { get; set; }

	private readonly int MaxHeldDistance = 128;

	public void SimulateActivePiece( IClient cl )
	{

		ActivePieceInput();

		if ( ActivePiece == null ) return;

		Angles yawDelta = Angles.Zero;

		if ( Input.Down( "use" ) )
		{
			yawDelta = new Angles( 0, -Input.MouseDelta.x * Time.Delta * 4, 0 );
			WantedAngleOffset += yawDelta;
		}

		if ( Game.IsServer )
		{
			using ( Prediction.Off() )
			{
				// pos

				// Get wanted pos instead, based on all piewces...
				Vector3 velocity = GetWantedVelocity();
				//Vector3 velocity = EyeRotation.Forward * MaxHeldDistance;
				//GetWantedVelocity( ActivePiece.Rotation, ref velocity );

				PositionOld = ActivePiece.Position;
				PositionNew = Vector3.SmoothDamp( ActivePiece.Position, ActivePiece.Position + velocity, ref DampVelocity, SmoothTime, Time.Delta * 2);
				velocity = PositionNew - PositionOld;
				ActivePiece.Velocity = velocity * 100;

				//rot

				UpdateHeldOffset( YawOld, ActivePiece.Rotation.Yaw() );	

				YawOld = ActivePiece.Rotation.Yaw();
				float yawWanted = EyeRotation.Yaw() + WantedAngleOffset.yaw;
				float yaw = Math2d.SmoothDampAngle( YawOld, yawWanted, ref YawDampVelocity, 0.2f, Time.Delta * 2, Math2d.Infinity );

				float m = ActivePiece.PhysicsBody.Mass * Time.Delta;
				ActivePiece.PhysicsBody.AngularVelocity = new Vector3( 
					-ActivePiece.Rotation.x * m,
					-ActivePiece.Rotation.y * m,
					(yaw - YawOld) );

			}

		}

		HoldSplashParticle?.SetPosition( 1, ActivePiece.Position - HeldOffset );	

		if ( ActivePiece.TimeSincePickedUp > 0.5f )
		{
			if ( ActivePiece.TryConnecting( out PuzzlePiece neighbor ) )
			{

				if ( Game.IsServer )
				{
					ConnectRoots( ActivePiece, neighbor );
					OnConnected( Client );
				}

				return;
			}
		}
	}

	private void UpdateHeldOffset(float withYaw)
	{
		Rotation old = ActivePiece.Rotation;
		Rotation _new = ActivePiece.Rotation.Angles().WithYaw( withYaw ).ToRotation();

		float delta = (_new.Angles() - old.Angles()).yaw;
		Vector3 v = Math2d.RotateByAngle( HeldOffset, -delta ) * HeldOffset.Length;

		HeldOffset = v;
		
	}

	private void UpdateHeldOffset( float yawOld, float yawNew )
	{
		float delta = (yawNew - yawOld);
		HeldOffset = Math2d.RotateByAngle( HeldOffset, -delta ) * HeldOffset.Length;
	}

	// This looks dumb and probably is dumb, but clients don't reliably call this function.
	private static void OnConnected(IClient cl)
	{
		OnConnectedClient(To.Single( cl ));
	}

	[ConCmd.Client("on_connected_client", CanBeCalledFromServer = true)]
	public static void OnConnectedClient()
	{
		if(Game.LocalClient == ConsoleSystem.Caller)
		{
			Sandbox.Services.Stats.Increment( "piece_connections", 1 );
		}

		Actionfeed.AddEntry( ConsoleSystem.Caller.Name + " made a connection!" );
	}

	private void ConnectRoots( PuzzlePiece piece, PuzzlePiece other )
	{
		if ( Game.IsClient ) return;

		// connect all pieces.
		PuzzlePiece thisRoot = piece.GetRoot();
		PuzzlePiece otherRoot = other.GetRoot();

		ClearActivePiece();

		#region Piece Side Checks

		IEnumerable<Entity> pNew = thisRoot.GetGroupPieces();
		IEnumerable<Entity> pOther = otherRoot.GetGroupPieces();

		int cCount = 0;

		// For each piece being connected
		foreach ( PuzzlePiece n in pNew )
		{
			bool pConnected = false;

			// Check against all pieces in other root
			foreach ( PuzzlePiece o in pOther )
			{
				if( TryConnectSides( n, o ) && !pConnected )
				{
					n.PlaySoundDelayed( ConnectSound, 75 * cCount );
					pConnected = true;
					cCount++;
				}
			}
		}

		#endregion

		// COLLAPSE GROUP and PARENT

		// This code seems redundant, but doing it any other way causes weird behaviour. I'm just glad it works...
		PuzzlePiece[] group = thisRoot.GetGroupPieces().Where( x => x != thisRoot ).ToArray();
		thisRoot.Parent = otherRoot;
		thisRoot.SetRoot(otherRoot);
		thisRoot.Rotation = Rotation.Identity;
		thisRoot.LocalRotation = Rotation.Identity;
		thisRoot.LocalPosition = new Vector3( (thisRoot.X - otherRoot.X) * JigsawGame.PieceScale, (thisRoot.Y - otherRoot.Y) * JigsawGame.PieceScale );

		foreach ( PuzzlePiece p in group )
		{
			// Set null, because otherwise it won't change the hierarchy
			// (parent is already otherRoot, even though parent is another piece that is parented to otherRoot)
			p.Parent = null;

			p.Parent = otherRoot;
			p.SetRoot(otherRoot);
			p.Rotation = Rotation.Identity;
			p.LocalRotation = Rotation.Identity;
			p.LocalPosition = new Vector3( (p.X - otherRoot.X) * JigsawGame.PieceScale, (p.Y - otherRoot.Y) * JigsawGame.PieceScale );
		}

		// Transfer Collision boxes
		IEnumerable<PhysicsShape> shapes = thisRoot.PhysicsBody.Shapes;
		foreach ( PhysicsShape s in shapes )
		{
			otherRoot.PhysicsBody.AddCloneShape( s );
		}
		thisRoot.PhysicsClear();

		// Check completion state of the puzzle.
		JigsawManager.CheckPuzzleCompletionRelative( otherRoot );

		// // // // //

		// Check if piece has a neighboring side with this piece, and connect them.
		bool TryConnectSides( PuzzlePiece piece, PuzzlePiece other )
		{
			if ( piece == other ) { return false; }

			Vector2 dir = new Vector2( other.X - piece.X, other.Y - piece.Y );
			if ( dir.Length > 1 ) { return false; } // piece is not a direct neighbor.
			int deg = dir.DirectionToQuadrant();

			//Log.Error( "-------------" );
			//Log.Error( "pos: " + new Vector2( piece.X, piece.Y ) + ", other pos: " + new Vector2( other.X, other.Y ) + ", dir: " + dir + ", deg: " + deg );

			switch ( deg )
			{
				// up
				case 0:
					//Log.Error( "Connect right" );
					piece.ConnectedRight = true;
					other.ConnectedLeft = true;
					return true;
				case 1:
					//Log.Error( "Connect Top" );
					piece.ConnectedTop = true;
					other.ConnectedBottom = true;
					return true;
				case 2:
					//Log.Error( "Connect Left" );
					piece.ConnectedLeft = true;
					other.ConnectedRight = true;
					return true;
				case 3:
					//Log.Error( "Connect Down" );
					piece.ConnectedBottom = true;
					other.ConnectedTop = true;
					return true;

			}

			return false;

		}
	}

	private Vector3 GetWantedVelocity()
	{
		// The distance from eye to active piece expressed as a percentage of MaxHeldDistance.
		//float pOfDst = (ActivePiece.Position - EyePosition).Length / MaxHeldDistance;

		Vector3 start = EyePosition;

		// Trace from active piece out in the eye direction until we reach MaxHeldDistance from the player eye.
		TraceResult trace = Trace.Body( ActivePiece.PhysicsBody,
			new Transform( start, ActivePiece.Rotation ), // START
			EyePosition + (EyeRotation.Forward * MaxHeldDistance) ) // END
				.WithAnyTags( CollisionTags )
				.Ignore( ActivePiece, true )
				.Ignore( this )
				.Run();

		if(JigsawGame.Current.Debug)
			DebugOverlay.Sphere( trace.EndPosition, 5, Color.Green );

		//Vector3 up = (Vector3.Up * JigsawGame.PieceThickness * JigsawGame.PieceScale / 2);
		Vector3 r = trace.EndPosition - ActivePiece.Position + HeldOffset;

		return trace.EndPosition - ActivePiece.Position + HeldOffset;

	}

	private void GetWantedVelocity(Rotation rot, ref Vector3 vel)
	{
		PuzzlePiece[] pieces = ActivePiece.GetGroupPieces().Where( x => x.IsValid ).ToArray();

		float pOfDst = (ActivePiece.Position - EyePosition).Length / MaxHeldDistance;
		Vector3 move = vel.Normal * MaxHeldDistance * pOfDst;

		foreach ( PuzzlePiece piece in pieces )
		{
			TraceResult trace = Trace.Sweep( piece.PhysicsBody, 
				new Transform( piece.PhysicsBody.Position, piece.PhysicsBody.Rotation.Angles().WithYaw(YawOld).ToRotation() ), // FROM
				new Transform( piece.PhysicsBody.Position + move, rot ) ) // TO
				.WithAnyTags( CollisionTags )
				.Ignore( ActivePiece, true )
				.Run();

			if ( trace.Hit )
			{
				Vector3 v = trace.Direction.Normal * trace.Distance;

				// We need to check each axis of the vector to know how far to move in each direction.
				//This vector's X is smaller than any other vector so far (i.e first collision)
				if ( MathF.Abs( v.x ) < MathF.Abs( move.x ) )
				{
					vel.x = v.x;
				}

				if ( MathF.Abs( v.y ) < MathF.Abs( move.y ) )
				{
					vel.y = v.y;
				}

				if ( MathF.Abs( v.z ) < MathF.Abs( move.z ) )
				{
					vel.z = v.z;
				}
			}
		}
	}

	private void ActivePieceInput()
	{

		if ( Input.StopProcessing || Inventory.ActiveChild?.GetType() != typeof (Fists) )
			return;

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
				SetActivePiece( root, tr.HitPosition );
			}
		}

		// Throw piece
		else if(Input.Released("attack1"))
		{
			if ( ActivePiece != null )
			{
				//DebugOverlay.Line( ActivePiece.Position, ActivePiece.Position + ActivePiece.Position - PositionOld, Color.Red, 10 );

				//ActivePiece.PhysicsBody.Velocity = ( ActivePiece.Position - PositionOld ) * 20;
				ClearActivePiece();
				return;
			}
		}

	}


	private void SetActivePiece( PuzzlePiece piece, Vector3 hitPosition )
	{

		piece.Owner = this;
		ActivePiece = piece;
		piece.HeldBy = this;

		piece.TimeSincePickedUp = 0;

		piece.EnableGroupPhysics( false );

		PositionOld = ActivePiece.Position;

		HeldOffset = piece.Position - hitPosition;

		WantedAngleOffset = (ActivePiece.Rotation.Angles() - EyeRotation.Angles()).WithPitch( 0 ).WithRoll( 0 );
		YawOld = ActivePiece.Rotation.Yaw();

		HoldSplashParticle?.Destroy();
		//HoldSplashParticle = Particles.Create( "particles/hold_splash.vpcf", ActivePiece, "", true );
	}

	public void ClearActivePiece()
	{
		if ( Game.IsServer )
		{
			ActivePiece?.EnableGroupPhysics( true );
		}

		HoldSplashParticle?.Destroy();

		if ( ActivePiece != null )
		{
			ActivePiece.HeldBy = null;
			ActivePiece.Owner = null;
			ActivePiece = null;
		}

	}

}
