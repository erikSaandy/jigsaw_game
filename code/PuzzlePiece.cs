using Sandbox;
using System;
using System.Linq;
using Saandy;

namespace Jigsaw;

public partial class PuzzlePiece : ModelEntity
{
	[Net] private PuzzlePiece rootPiece { get; set; }
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
	public JigsawPawn HeldBy { get; set; } = null;

	private readonly int ConnectionDistance = 4;
	public bool ConnectedLeft, ConnectedRight, ConnectedTop, ConnectedBottom = false;

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

		}
	}

	public void CheckForConnections()
	{
		if(GetRoot() != this ) { GetRoot().CheckForConnections(); return; }

		// This is root
		PuzzlePiece neighbor = null;

		if( FindNeighbor(out neighbor ) ) { ConnectToPiece( neighbor ); return; }

		foreach(PuzzlePiece c in Children)
		{
			if ( c.FindNeighbor( out neighbor ) ) { ConnectToPiece( neighbor ); return; }
		}
	}

	private bool FindNeighbor(out PuzzlePiece neighbor)
	{
		int scale = JigsawGame.PieceScale;
		float dot = 0;

		//DebugOverlay.Line( Position, Position + (Transform.Rotation.Backward * scale), Color.Green );
		if ( GetNeighbor( -1, 0, out neighbor ) ) { dot = Vector3.Dot( neighbor.Rotation.Forward, Rotation.Forward ); }


		if ( neighbor != null && Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Backward * scale), neighbor.Position, Color.White ); }
		if ( !ConnectedTop && neighbor != null && dot >= 0.95f )
		{
			if ( (Position + (Transform.Rotation.Backward * scale) - neighbor.Position).Length < ConnectionDistance )
			{
				//if ( connectPhysically ) { ConnectToPiece( -1, 0 ); return true; }
				//else if ( root != neighbor.root ) { return false; }

				ConnectedTop = true;
				neighbor.ConnectedBottom = true;
				return true;
			}
		}

		if ( GetNeighbor( 1, 0, out neighbor ) ) { dot = Vector3.Dot( neighbor.Rotation.Forward, Rotation.Forward ); }

		if ( neighbor != null && Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Forward * scale), neighbor.Position, Color.White ); }
		if ( neighbor != null && !ConnectedBottom && dot > 0.95f )
		{
			if ( (Position + (Transform.Rotation.Forward * scale) - neighbor.Position).Length < ConnectionDistance )
			{
				//if ( connectPhysically ) { ConnectToPiece( 0, 1 ); return true; }
				//else if ( root != neighbor.root ) { return false; }

				ConnectedBottom = true;
				neighbor.ConnectedTop = true;
				return true;
			}
		}


		if ( GetNeighbor( 0, 1, out neighbor ) ) { dot = Vector3.Dot( neighbor.Rotation.Forward, Rotation.Forward ); }

		if ( neighbor != null && Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Left * scale), neighbor.Position, Color.White ); }
		if ( !ConnectedRight && neighbor != null && dot > 0.95f )
		{
			if ( (Position + (Transform.Rotation.Left * scale) - neighbor.Position).Length < ConnectionDistance )
			{
				DebugOverlay.Line( Position + (Transform.Rotation.Right * scale), neighbor.Position, Color.Red );
				//if ( connectPhysically ) { ConnectToPiece( 1, 0 ); return true; }
				//else if ( root != neighbor.root ) { return false; }

				ConnectedRight = true;
				neighbor.ConnectedLeft = true;

				return true;
			}	
		}


		if ( GetNeighbor( 0, -1, out neighbor ) ) { dot = Vector3.Dot( neighbor.Rotation.Forward, Rotation.Forward ); }

		if ( neighbor != null && Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Right * scale), neighbor.Position, Color.White ); }
		if ( !ConnectedLeft && neighbor != null && dot > 0.95f )
		{

			if ( (Position + (Transform.Rotation.Right * scale) - neighbor.Position).Length < ConnectionDistance )
			{
				//if ( connectPhysically ) { ConnectToPiece( 0, -1 ); return true; }
				//else if ( root != neighbor.root ) { return false; }

				ConnectedLeft = true;
				neighbor.ConnectedRight = true;

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

	private void ConnectToPiece(PuzzlePiece piece)
	{
		PuzzlePiece newRoot = piece.GetRoot();

		HeldBy.ActivePiece = null;
		HeldBy = null;

		PuzzlePiece root = GetRoot();
		root.PhysicsEnabled = false;

		root.Parent = newRoot;
		root.LocalRotation = new Angles( 0, 0, 0 ).ToRotation();
		root.LocalPosition = new Vector3( (root.X - newRoot.X) * JigsawGame.PieceScale, (root.Y - newRoot.Y) * JigsawGame.PieceScale );
		root.rootPiece = newRoot;

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
