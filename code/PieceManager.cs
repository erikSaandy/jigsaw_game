using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Jigsaw;
public static partial class PieceManager
{
	public const string ConnectSound = "sounds/piece_connections/piece_connect.sound";

	// This looks dumb and probably is dumb, but clients don't reliably call this function.
	public static void OnConnected( IClient cl )
	{
		OnConnectedClient( To.Single( cl ) );
	}

	[ConCmd.Client( "on_connected_client", CanBeCalledFromServer = true )]
	public static void OnConnectedClient()
	{
		if ( Game.LocalClient == ConsoleSystem.Caller )
		{
			Sandbox.Services.Stats.Increment( "piece_connections", 1 );
		}

		Actionfeed.AddEntry( ConsoleSystem.Caller.Name + " made a connection!" );
	}

	public static void ConnectRoots( IClient cl, PuzzlePiece piece, PuzzlePiece other )
	{
		if ( Game.IsClient ) return;

		// connect all pieces.
		PuzzlePiece thisRoot = piece.GetRoot();
		PuzzlePiece otherRoot = other.GetRoot();

		ClearActivePiece(cl);

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
				if ( TryConnectSides( n, o ) && !pConnected )
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
		thisRoot.SetRoot( otherRoot );
		thisRoot.Rotation = Rotation.Identity;
		thisRoot.LocalRotation = Rotation.Identity;
		thisRoot.LocalPosition = new Vector3( (thisRoot.X - otherRoot.X) * JigsawGame.PieceScale, (thisRoot.Y - otherRoot.Y) * JigsawGame.PieceScale );

		foreach ( PuzzlePiece p in group )
		{
			// Set null, because otherwise it won't change the hierarchy
			// (parent is already otherRoot, even though parent is another piece that is parented to otherRoot)
			p.Parent = null;

			p.Parent = otherRoot;
			p.SetRoot( otherRoot );
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

	public static void SetActivePiece( IClient cl, PuzzlePiece piece, Vector3 hitPosition )
	{
		JigsawPawn pawn = cl.Pawn as JigsawPawn;

		piece.Owner = pawn;
		pawn.ActivePiece = piece;
		piece.HeldBy = pawn;

		piece.TimeSincePickedUp = 0;

		piece.EnableGroupPhysics( false );

		pawn.PositionOld = pawn.ActivePiece.Position;
		pawn.HeldOffset = piece.Position - hitPosition;
		pawn.WantedAngleOffset = (pawn.ActivePiece.Rotation.Angles() - pawn.EyeRotation.Angles()).WithPitch( 0 ).WithRoll( 0 );
		pawn.YawOld = pawn.ActivePiece.Rotation.Yaw();
		pawn.HoldSplashParticle?.Destroy();
		//HoldSplashParticle = Particles.Create( "particles/hold_splash.vpcf", ActivePiece, "", true );
	}

	public static void ClearActivePiece(IClient cl)
	{
		JigsawPawn pawn = cl.Pawn as JigsawPawn;

		if ( Game.IsServer )
		{
			pawn.ActivePiece?.EnableGroupPhysics( true );
		}

		pawn.HoldSplashParticle?.Destroy();

		if ( pawn.ActivePiece != null )
		{
			pawn.ActivePiece.HeldBy = null;
			pawn.ActivePiece.Owner = null;
			pawn.ActivePiece = null;
		}

	}
}
