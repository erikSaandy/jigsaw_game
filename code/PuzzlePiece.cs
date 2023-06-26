using Sandbox;
using System;
using System.Linq;
using Saandy;

namespace Jigsaw;

public partial class PuzzlePiece : ModelEntity
{
	[Net] private PuzzlePiece rootPiece { get; set; } = null;
	public PuzzlePiece RootPiece => GetRoot();
	public PuzzlePiece GetRoot()
	{
		if ( rootPiece == null ) { rootPiece = this; }
		return rootPiece;
	}

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

	[Net]
	public TimeSince TimeSincePickedUp { get; set; } = 0;

	private readonly int ConnectionDistance = 4;

	[Net] public bool ConnectedLeft { get; set; } = false;
	[Net] public bool ConnectedRight { get; set; } = false;
	[Net] public bool ConnectedTop { get; set; } = false;
	[Net] public bool ConnectedBottom { get; set; } = false;

	public static bool Debug { get; set; } = false;

	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		base.Spawn();
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


		GetBoundingBox( out Vector3 mins, out Vector3 maxs );
		SetupPhysicsFromOBB( PhysicsMotionType.Dynamic, mins, maxs );
		GeneratePipCollision();

		PhysicsEnabled = true;
		UsePhysicsCollision = true;

	}

	public void GenerateClient()
	{
		Model = JigsawGame.Current.PieceModels[Index];

		GetBoundingBox( out Vector3 mins, out Vector3 maxs );
		SetupPhysicsFromOBB( PhysicsMotionType.Dynamic, mins, maxs );
		GeneratePipCollision();
	}

	private void GetBoundingBox(out Vector3 mins, out Vector3 maxs)
	{
		// TODO: Edge pieces are inaccurate, and pips are not accounted for.

		float wMin = JigsawGame.GetWobbleAt( X * JigsawGame.PieceScale, Y * JigsawGame.PieceScale );
		float wMax = JigsawGame.GetWobbleAt( (X+1) * JigsawGame.PieceScale, (Y+1) * JigsawGame.PieceScale );

		mins = new Vector3(
			-(JigsawGame.PieceScale / 2) + ((X == 0) ? 0 : wMin),
			-(JigsawGame.PieceScale / 2) + ((Y == 0) ? 0 : wMin),
			-(JigsawGame.PieceScale * JigsawGame.PieceThickness) / 2
		);

		maxs = new Vector3(
			(JigsawGame.PieceScale/2) + ((X == JigsawGame.Current.PieceCountX - 1) ? 0 : wMax),
			(JigsawGame.PieceScale/2) + ((Y == JigsawGame.Current.PieceCountY - 1) ? 0 : wMax),
			(JigsawGame.PieceScale * JigsawGame.PieceThickness) / 2
		);

	}

	private void GeneratePipCollision()
	{
		foreach ( Vector2 c in JigsawGame.Current.PieceMeshData[X, Y].pipCenters )
		{
			if ( c != Vector2.Zero )
			{
				PhysicsBody.AddBoxShape( c, Rotation.Identity, (new Vector3( JigsawGame.PipScale, JigsawGame.PipScale, JigsawGame.PieceThickness ) * JigsawGame.PieceScale / 2) );
			}
		}
	}

	[GameEvent.Tick]
	void Tick()
	{
		if ( Game.IsClient && Debug )
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
	}

	public void CheckForConnections()
	{
		// Get the root of active piece.
		if(GetRoot() != this ) { GetRoot().CheckForConnections(); return; }

		// This is root

		PuzzlePiece neighbor = null;

		// Find close neighbor with active piece root.
		if( FindCloseNeighbor(out neighbor ) ) { ConnectToPiece( neighbor ); return; }

		// Find close neighbor with pieces connected to active piece root.
		foreach(PuzzlePiece c in Children )
		{
			if ( c.FindCloseNeighbor( out neighbor ) ) { c.ConnectToPiece( neighbor ); return; }
		}
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
		if ( neighbor != null && Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Backward * scale), neighbor.Position, Color.White ); }
		
		if ( !ConnectedBottom && neighbor != null && dot >= 0.95f )
		{
			if ( (Position + (Transform.Rotation.Backward * scale) - neighbor.Position).Length < ConnectionDistance )
			{
				return true;
			}
		}

		if ( GetNeighbor( 1, 0, out neighbor ) ) { dot = Vector3.Dot( neighbor.Rotation.Forward, Rotation.Forward ); }
		if ( neighbor != null && Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Forward * scale), neighbor.Position, Color.White ); }
		
		if ( !ConnectedTop && neighbor != null && dot > 0.95f )
		{
			if ( (Position + (Transform.Rotation.Forward * scale) - neighbor.Position).Length < ConnectionDistance )
			{
				return true;
			}
		}


		if ( GetNeighbor( 0, 1, out neighbor ) ) { dot = Vector3.Dot( neighbor.Rotation.Forward, Rotation.Forward ); }
		if ( neighbor != null && Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Left * scale), neighbor.Position, Color.White ); }

		if ( !ConnectedLeft && neighbor != null && dot > 0.95f )
		{
			if ( (Position + (Transform.Rotation.Left * scale) - neighbor.Position).Length < ConnectionDistance )
			{
				return true;
			}	
		}


		if ( GetNeighbor( 0, -1, out neighbor ) ) { dot = Vector3.Dot( neighbor.Rotation.Forward, Rotation.Forward ); }
		if ( neighbor != null && Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Right * scale), neighbor.Position, Color.White ); }

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

	private void ConnectToPiece(PuzzlePiece other)
	{

		// connect all pieces.
		PuzzlePiece activeRoot = GetRoot();
		PuzzlePiece newRoot = other.GetRoot();

		HeldBy.ActivePiece = null;
		HeldBy = null;

		// Check root to new root.
		TryConnectSides( activeRoot, newRoot );

		// For each piece in the held group...
		foreach ( PuzzlePiece newRootPiece in newRoot.Children )
		{
			// check to each piece in the new group.
			foreach ( PuzzlePiece activeRootPiece in activeRoot.Children )
			{
				TryConnectSides( activeRootPiece, newRootPiece );
			}

			// also check the root piece to every piece under new root.
			TryConnectSides( activeRoot, newRootPiece );
		}

		// Set piece transform relative to root.
		activeRoot.Parent = newRoot;
		activeRoot.LocalRotation = new Angles( 0, 0, 0 ).ToRotation();
		activeRoot.LocalPosition = new Vector3( (activeRoot.X - newRoot.X) * JigsawGame.PieceScale, (activeRoot.Y - newRoot.Y) * JigsawGame.PieceScale );
		activeRoot.rootPiece = newRoot;

		// Check if piece has a neighboring side with this piece, and connect them.
		void TryConnectSides( PuzzlePiece piece, PuzzlePiece other )
		{
			Vector2 dir = new Vector2( other.X - piece.X, other.Y - piece.Y );
			int deg = dir.ToInt();

			// piece is not a direct neighbor.
			if ( dir.Length > 1 ) { return; }

			//Log.Error( "pos: " + new Vector2( X, Y ) + ", other pos: " + new Vector2( pn.X, pn.Y ) + ", dir: " + dir + ", deg: " + deg );

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

	public bool OnUse( Entity user )
	{
		return false;
	}

	public virtual bool IsUsable( Entity user )
	{
		return Owner == null;
	}

}
