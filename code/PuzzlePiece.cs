using Sandbox;
using System;
using System.Linq;
using Saandy;
using System.Collections.Generic;
using static Sandbox.CitizenAnimationHelper;

namespace Jigsaw;

public partial class PuzzlePiece : ModelEntity
{
	private readonly int ConnectionDistance = 2;

	[Net] private PuzzlePiece rootPiece { get; set; } = null;
	public PuzzlePiece RootPiece => GetRoot();
	public PuzzlePiece GetRoot()
	{
		if ( rootPiece == null ) { rootPiece = this; }
		return rootPiece;
	}

	[Net] public bool freeFall { get; set; } = false;
	public bool FreeFall { get { return GetRoot().freeFall; } private set { GetRoot().freeFall = value; } }

	[Net]
	public int Index { get; private set; } = 0;

	[Net]
	public int X { get; set; } = 0;
	[Net]
	public int Y { get; set; } = 0;

	[Net, Predicted]
	private  JigsawPawn heldBy { get; set; } = null;

	public JigsawPawn HeldBy {
		get
		{
			return GetRoot().heldBy;
		}
		set
		{
			GetRoot().heldBy = value;
		}
	}

	/// <summary>
	/// Returns all pieces in the same connected group as this piece.
	/// </summary>
	/// <returns></returns>
	public IEnumerable<PuzzlePiece> GetGroupPieces()
	{
		return GetRoot().Children.Append( GetRoot() ).OfType<PuzzlePiece>();
	}

	[Net]
	public TimeSince TimeSincePickedUp { get; set; } = 0;

	[Net] public bool ConnectedLeft { get; set; } = false;
	[Net] public bool ConnectedRight { get; set; } = false;
	[Net] public bool ConnectedTop { get; set; } = false;
	[Net] public bool ConnectedBottom { get; set; } = false;

	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public void Respawn()
	{
		base.Spawn();
		ChatBox.SayInformation( "I found this in the abyss...\rI'll leave it in the center of the map! 😮‍💨" );
		Position = (Vector3.Up * 64);
		Random r = new Random();
		Rotation = new Rotation( 0, 0, 180, r.Next( 0, 360 ) );
		Velocity = Vector3.Zero;
	}

	public PuzzlePiece() : base() {  }

	public PuzzlePiece( int x, int y ) : base()
	{
		this.X = x;
		this.Y = y;
		Index = Math2d.ArrayIndex( x, y, JigsawGame.Current.PieceCountX, JigsawGame.Current.PieceCountY );
		Tags.Add( "puzzlepiece" );
		Name = "PuzzlePiece" + " (" + X + ", " + Y + ")"; 
		
		// Generate
		//SetupPhysicsFromModel( PhysicsMotionType.Dynamic );


		GetBoundingBox(X, Y, out Vector3 mins, out Vector3 maxs );
		SetupPhysicsFromOBB( PhysicsMotionType.Dynamic, mins, maxs );
		//GeneratePipCollision();

		PhysicsEnabled = true;
		UsePhysicsCollision = true;

	}

	public void GenerateClient()
	{
		Model = JigsawGame.Current.PieceModels?[Index];
		GetBoundingBox(X, Y, out Vector3 mins, out Vector3 maxs );
		SetupPhysicsFromOBB( PhysicsMotionType.Dynamic, mins, maxs );
		//GeneratePipCollision();
	}

	public static void GetBoundingBox(int X, int Y, out Vector3 mins, out Vector3 maxs)
	{
		// TODO: Edge pieces are inaccurate, and pips are not accounted for.

		float wMin = JigsawGame.GetWobbleAt( X * JigsawGame.PieceScale, Y * JigsawGame.PieceScale );
		float wMax = JigsawGame.GetWobbleAt( (X+1) * JigsawGame.PieceScale, (Y+1) * JigsawGame.PieceScale );

		mins = new Vector3(
			-(JigsawGame.PieceScale / 2) + ((X == 0) ? 0 : wMin),
			-(JigsawGame.PieceScale / 2) + ((Y == 0) ? 0 : wMin),
			-(JigsawGame.PieceScale * JigsawGame.PieceThickness / 2)
		);

		maxs = new Vector3(
			(JigsawGame.PieceScale/2) + ((X == JigsawGame.Current.PieceCountX - 1) ? 0 : wMax),
			(JigsawGame.PieceScale/2) + ((Y == JigsawGame.Current.PieceCountY - 1) ? 0 : wMax),
			(JigsawGame.PieceScale * JigsawGame.PieceThickness / 2)
		);
	}

	private void GeneratePipCollision()
	{
		foreach ( Vector2 c in JigsawGame.Current.PieceMeshData[X, Y].pipCenters )
		{
			if ( c != Vector2.Zero )
			{
				PhysicsBody?.AddBoxShape( c, Rotation.Identity, (new Vector3( JigsawGame.PipScale, JigsawGame.PipScale, JigsawGame.PieceThickness ) * JigsawGame.PieceScale / 2) );
			}
		}
	}

	[GameEvent.Tick]
	void Tick()
	{
		if ( Game.IsClient && JigsawGame.Current.Debug )
		{
			DebugOverlay.Text( "[" + X + ", " + Y + "]", Position );

			Vector3 center = Position + (Transform.Rotation.Up * (JigsawGame.PieceThickness*16));
			DebugOverlay.Line( center, center + (Transform.Rotation.Forward * 32), Color.Blue );
			DebugOverlay.Line( center, center + (Transform.Rotation.Right * 32), Color.Red );
			DebugOverlay.Line( center, center + (Transform.Rotation.Up * 32), Color.Green );

			DebugOverlay.Sphere( Position, 4, Color.Red, 0.05f );
			if ( ConnectedTop ) { DebugOverlay.Sphere( Position + Transform.Rotation.Forward * 4, 2, Color.Red, 0.05f ); }
			if ( ConnectedLeft ) { DebugOverlay.Sphere( Position + Transform.Rotation.Left * 4, 2, Color.Green, 0.05f ); }
			if ( ConnectedRight ) { DebugOverlay.Sphere( Position + Transform.Rotation.Right * 4, 2, Color.Blue, 0.05f ); }
			if ( ConnectedBottom ) { DebugOverlay.Sphere( Position + Transform.Rotation.Backward * 4, 2, Color.Yellow, 0.05f ); }
		}

		if(Game.IsServer)
		{
			UpdateFreeFall();
		}

	}

	public TimeSince FreeFallTime = 0;
	private void UpdateFreeFall()
	{
		if ( Game.IsServer )
		{
			if ( GetRoot() == this ) // Do this only for root pieces.
			{
				if( GetRoot().Velocity.z < -500 ) {
					FreeFall = true;
					if(FreeFallTime >= 10) // Fell for 10 seconds.
					{
						GetRoot().Respawn();
					}
				}
				else {
					FreeFallTime = 0;
					FreeFall = false;
				}
			}
		}

	}

	public bool TryConnecting()
	{
		// Get the root of active piece.
		if(GetRoot() != this ) { return GetRoot().TryConnecting(); }

		// This is root

		PuzzlePiece neighbor = null;

		// Find close neighbor with active piece root.
		if( FindCloseNeighbor(out neighbor ) ) { ConnectRoots( this.GetRoot(), neighbor.GetRoot() ); OnConnectedServer(); return true; }

		// Find close neighbor with pieces connected to active piece root.
		foreach(PuzzlePiece c in Children )
		{
			if ( c.FindCloseNeighbor( out neighbor ) ) { ConnectRoots( this.GetRoot(), neighbor.GetRoot() ); OnConnectedServer(); return true; }
		}

		return false;

	}

	private void OnConnectedServer()
	{
		if(Game.IsServer) OnConnectedClient(To.Everyone);
	}

	[ConCmd.Client( "add_entry_client", CanBeCalledFromServer = true )]
	private static void OnConnectedClient()
	{
		Actionfeed.AddEntryClient( Game.LocalClient.Name + " made a connection!" );
	}

	/// <summary>
	/// Find a neighboring piece that is close enough to connect to.
	/// </summary>
	/// <param name="neighbor"></param>
	/// <returns></returns>
	private bool FindCloseNeighbor(out PuzzlePiece neighbor)
	{
		int scale = JigsawGame.PieceScale;
		float dot = 0;
			
		//DebugOverlay.Line( Position, Position + (Transform.Rotation.Backward * scale), Color.Green );
		if ( GetNeighbor( -1, 0, out neighbor ) ) { dot = Vector3.Dot( neighbor.Rotation.Forward, Rotation.Forward ); }
		if ( neighbor != null && JigsawGame.Current.Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Backward * scale), neighbor.Position, Color.White ); }
		
		if ( !ConnectedBottom && neighbor != null && dot >= 0.95f )
		{
			if ( (Position + (Transform.Rotation.Backward * scale) - neighbor.Position).Length < ConnectionDistance )
			{
				return true;
			}
		}

		if ( GetNeighbor( 1, 0, out neighbor ) ) { dot = Vector3.Dot( neighbor.Rotation.Forward, Rotation.Forward ); }
		if ( neighbor != null && JigsawGame.Current.Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Forward * scale), neighbor.Position, Color.White ); }
		
		if ( !ConnectedTop && neighbor != null && dot > 0.95f )
		{
			if ( (Position + (Transform.Rotation.Forward * scale) - neighbor.Position).Length < ConnectionDistance )
			{
				return true;
			}
		}


		if ( GetNeighbor( 0, 1, out neighbor ) ) { dot = Vector3.Dot( neighbor.Rotation.Forward, Rotation.Forward ); }
		if ( neighbor != null && JigsawGame.Current.Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Left * scale), neighbor.Position, Color.White ); }

		if ( !ConnectedLeft && neighbor != null && dot > 0.95f )
		{
			if ( (Position + (Transform.Rotation.Left * scale) - neighbor.Position).Length < ConnectionDistance )
			{
				return true;
			}	
		}


		if ( GetNeighbor( 0, -1, out neighbor ) ) { dot = Vector3.Dot( neighbor.Rotation.Forward, Rotation.Forward ); }
		if ( neighbor != null && JigsawGame.Current.Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Right * scale), neighbor.Position, Color.White ); }

		if ( !ConnectedRight && neighbor != null && dot > 0.95f )
		{
			if ( (Position + (Transform.Rotation.Right * scale) - neighbor.Position).Length < ConnectionDistance )
			{
				return true;
			}
		}

		neighbor = null;
		return false;

	}

	private bool GetNeighbor( int dirX, int dirY, out PuzzlePiece piece )
	{
		if ( dirX != 0 &&
			X + dirX >= 0 &&
			X + dirX < JigsawGame.Current.PieceCountX )
		{
			piece = JigsawGame.Current.GetPieceEntity(X + dirX, Y);

			// Don't connect pieces that are already connected.
			if ( piece.GetRoot() == GetRoot() ) { return false; }

			return true;
		}

		if ( dirY != 0 &&
			Y + dirY >= 0 &&
			Y + dirY < JigsawGame.Current.PieceCountY )
		{
			piece = JigsawGame.Current.GetPieceEntity( X, Y + dirY );

			// Don't connect pieces that are already connected.
			if ( piece.GetRoot() == GetRoot() ) { return false; }

			return true;
		}

		piece = null;
		return false;
	}

	private void ConnectRoots(PuzzlePiece piece, PuzzlePiece other)
	{
		if ( Game.IsClient ) return;

		// connect all pieces.
		PuzzlePiece thisRoot = piece.GetRoot();
		PuzzlePiece otherRoot = other.GetRoot();

		HeldBy.ClearActivePiece();
		HeldBy = null;

		#region Piece Side Checks

		IEnumerable<Entity> pNew = thisRoot.Children.Append( thisRoot );
		IEnumerable<Entity> pOther = otherRoot.Children.Append( otherRoot );

		// For each piece being connected
		foreach ( PuzzlePiece n in pNew )
		{
			// Check against all pieces in other root
			foreach ( PuzzlePiece o in pOther )
			{
				TryConnectSides( n, o );
			}
		}

		#endregion

		// COLLAPSE GROUP and PARENT

		// This code seems redundant, but doing it any other way causes weird behaviour. I'm just glad it works...
		PuzzlePiece[] group = thisRoot.GetGroupPieces().Where( x => x != thisRoot ).ToArray();
		thisRoot.Parent = otherRoot;
		thisRoot.rootPiece = otherRoot;
		thisRoot.Rotation = Rotation.Identity;
		thisRoot.LocalRotation = Rotation.Identity;
		thisRoot.LocalPosition = new Vector3( (thisRoot.X - otherRoot.X) * JigsawGame.PieceScale, (thisRoot.Y - otherRoot.Y) * JigsawGame.PieceScale );

		foreach (PuzzlePiece p in group)
		{
			// Set null, because otherwise it won't change the hierarchy
			// (parent is already otherRoot, even though parent is another piece that is parented to otherRoot)
			p.Parent = null; 
			
			p.Parent = otherRoot;
			p.rootPiece = otherRoot;
			p.Rotation = Rotation.Identity;
			p.LocalRotation = Rotation.Identity;
			p.LocalPosition = new Vector3( (p.X - otherRoot.X) * JigsawGame.PieceScale, (p.Y - otherRoot.Y) * JigsawGame.PieceScale );
		}

		// Transfer Collision boxes
		IEnumerable<PhysicsShape> shapes = thisRoot.PhysicsBody.Shapes;
		foreach( PhysicsShape s in shapes)
		{
			otherRoot.PhysicsBody.AddCloneShape( s );
		}
		thisRoot.PhysicsClear();

		// Check completion state of the puzzle.
		JigsawManager.CheckPuzzleCompletionRelative( otherRoot );

		// // // // //

		// Check if piece has a neighboring side with this piece, and connect them.
		void TryConnectSides( PuzzlePiece piece, PuzzlePiece other )
		{
			if(piece == other ) { return; }

			Vector2 dir = new Vector2( other.X - piece.X, other.Y - piece.Y );		
			if ( dir.Length > 1 ) { return; } // piece is not a direct neighbor.
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
					break;
				case 1:
					//Log.Error( "Connect Top" );
					piece.ConnectedTop = true;
					other.ConnectedBottom = true;
					break;
				case 2:
					//Log.Error( "Connect Left" );
					piece.ConnectedLeft = true;
					other.ConnectedRight = true;
					break;
				case 3:
					//Log.Error( "Connect Down" );
					piece.ConnectedBottom = true;
					other.ConnectedTop = true;
					break;
			}
		}
	}

	public void EnableGroupPhysics( bool enable = true )
	{
		if(GetRoot() != this) { GetRoot().EnableGroupPhysics( enable ); }

		PhysicsBody[] bodies = PhysicsGroup.Bodies.ToArray();
		foreach ( PhysicsBody b in bodies )
		{
			b.GravityEnabled = enable;
		}

		//e.PhysicsEnabled = enable;
		UsePhysicsCollision = enable;
		EnableAllCollisions = enable;
		EnableSolidCollisions = enable;
		EnableTraceAndQueries = true;

	}

	public bool OnUse( Entity user )
	{
		return false;
	}

	public virtual bool IsUsable( Entity user )
	{
		return Owner == null;
	}

}
