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
	private readonly float DotThreshold = 0.90f;


	[Net] private PuzzlePiece rootPiece { get; set; } = null;
	public PuzzlePiece RootPiece => GetRoot();
	public PuzzlePiece GetRoot()
	{
		if ( rootPiece == null ) { rootPiece = this; }
		return rootPiece;
	}

	public void SetRoot(PuzzlePiece root) { rootPiece = root; }

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

		Position = Vector3.Zero;

		GetBoundingBox(X, Y, out Vector3 mins, out Vector3 maxs );
		SetupPhysicsFromOBB( PhysicsMotionType.Dynamic, mins, maxs );

		//GeneratePipCollision();

		PhysicsEnabled = false;
		UsePhysicsCollision = true;
		EnableSolidCollisions = true;
		EnableTraceAndQueries = true;

	}

	public void GenerateClient()
	{
		Model = JigsawGame.Current.PieceModels?[Index];

		GetBoundingBox( X, Y, out Vector3 mins, out Vector3 maxs );
		SetupPhysicsFromOBB( PhysicsMotionType.Dynamic, mins, maxs );

		//GeneratePipCollision();

	}

	public static void GetBoundingBox( int X, int Y, out Vector3 mins, out Vector3 maxs )
	{

		Vector2 negate = new Vector2( (0.5f + X) * JigsawGame.PieceScale, (0.5f + Y) * JigsawGame.PieceScale );

		PieceMeshData data = JigsawGame.PieceMeshData[X, Y];
		Vector2 bl = data.polygon.contour.points[data.contourSideStartID[0]] - negate;
		Vector2 tl = data.polygon.contour.points[data.contourSideStartID[1]] - negate;
		Vector2 tr = data.polygon.contour.points[data.contourSideStartID[2]] - negate;
		Vector2 br = data.polygon.contour.points[data.contourSideStartID[3]] - negate;

		// Might look like I want to use Min here too, but only using Max produces hitboxes that fit together better.
		float left = Math.Max( bl.x, tl.x );
		float top = Math.Max( tl.y, tr.y );
		float right = Math.Max( tr.x, br.x );
		float bottom = Math.Max( bl.y, br.y );

		mins = new Vector3( left, bottom, -(JigsawGame.PieceScale * JigsawGame.PieceThickness / 2) );
		maxs = new Vector3( right, top, (JigsawGame.PieceScale * JigsawGame.PieceThickness / 2) );
		//Log.Error( "mins: ( " + mins.x + ", " + mins.y + " ), maxs: ( " + maxs.x + ", " + maxs.y + " )" );

	}

	private void GeneratePipCollision()
	{
		foreach ( Vector2 c in JigsawGame.PieceMeshData[X, Y].pipCenters )
		{
			if ( c != Vector2.Zero )
			{
				PhysicsShape shape = PhysicsBody?.AddBoxShape( c, Rotation.Identity, (new Vector3( JigsawGame.PipScale, JigsawGame.PipScale, JigsawGame.PieceThickness ) * JigsawGame.PieceScale / 2) );
				shape.DisableAllCollision();
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

	public bool TryConnecting( out PuzzlePiece neighbor )
	{
		neighbor = null;

		// Get the root of active piece.
		if (GetRoot() != this ) { return GetRoot().TryConnecting( out neighbor ); }

		// This is root

		// Find close neighbor with active piece root.
		if(FindCloseNeighbor( out neighbor )) { return true; }

		// Find close neighbor with pieces connected to active piece root.
		foreach ( PuzzlePiece c in Children )
		{
			if ( c.FindCloseNeighbor( out neighbor ) ) { return true; }
		}

		return false;

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
		
		if ( !ConnectedBottom && neighbor != null && dot >= DotThreshold )
		{
			if ( (Position + (Transform.Rotation.Backward * scale) - neighbor.Position).Length < ConnectionDistance )
			{
				return true;
			}
		}

		if ( GetNeighbor( 1, 0, out neighbor ) ) { dot = Vector3.Dot( neighbor.Rotation.Forward, Rotation.Forward ); }
		if ( neighbor != null && JigsawGame.Current.Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Forward * scale), neighbor.Position, Color.White ); }

		if ( !ConnectedTop && neighbor != null && dot > DotThreshold )
		{
			if ( (Position + (Transform.Rotation.Forward * scale) - neighbor.Position).Length < ConnectionDistance )
			{
				return true;
			}
		}


		if ( GetNeighbor( 0, 1, out neighbor ) ) { dot = Vector3.Dot( neighbor.Rotation.Forward, Rotation.Forward ); }
		if ( neighbor != null && JigsawGame.Current.Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Left * scale), neighbor.Position, Color.White ); }

		if ( !ConnectedLeft && neighbor != null && dot > DotThreshold )
		{
			if ( (Position + (Transform.Rotation.Left * scale) - neighbor.Position).Length < ConnectionDistance )
			{
				return true;
			}	
		}


		if ( GetNeighbor( 0, -1, out neighbor ) ) { dot = Vector3.Dot( neighbor.Rotation.Forward, Rotation.Forward ); }
		if ( neighbor != null && JigsawGame.Current.Debug ) { DebugOverlay.Line( Position + (Transform.Rotation.Right * scale), neighbor.Position, Color.White ); }

		if ( !ConnectedRight && neighbor != null && dot > DotThreshold )
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

	public void EnableGroupPhysics( bool enable = true )
	{
		if(GetRoot() != this) { GetRoot().EnableGroupPhysics( enable ); }

		PhysicsBody[] bodies = PhysicsGroup.Bodies.ToArray();
		foreach ( PhysicsBody b in bodies )
		{
			b.GravityEnabled = enable;
		}

		// When enable = true, transmit default. else transmit always.
		int transmit = (Convert.ToInt32( enable ) - 1) * -1;
		//Log.Error( transmit );
		Transmit = (TransmitType)transmit;
		PhysicsBody.AutoSleep = enable;

		//UsePhysicsCollision = enable;
		//EnableAllCollisions = enable;

		//EnableSolidCollisions = true;
		//EnableTraceAndQueries = true;

	}

	public bool OnUse( Entity user )
	{
		return false;
	}

	public virtual bool IsUsable( Entity user )
	{
		return Owner == null;
	}

	public void FreezeGroup(bool freeze = true)
	{
		IEnumerable<PuzzlePiece> group = GetGroupPieces();

		foreach(PuzzlePiece p in group)
		{
			p.PhysicsEnabled = !freeze;

			if ( p.PhysicsBody != null )
			{
				p.PhysicsBody.Enabled = !freeze;
				p.PhysicsBody.BodyType = freeze ? PhysicsBodyType.Static : PhysicsBodyType.Dynamic;

				if ( freeze )
				{
					p.PhysicsBody.ClearForces();
				}
			}
		}
	}

}
