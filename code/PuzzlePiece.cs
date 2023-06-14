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

	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		base.Spawn();
	}

	public PuzzlePiece() : base() {  }

	public PuzzlePiece(int x, int y) : base()
	{
		this.X = x;
		this.Y = y;
		Index = Math2d.ArrayIndex( x, y, JigsawGame.Current.PieceCountX, JigsawGame.Current.PieceCountY );
		Tags.Add( "puzzlepiece" );

		// Generate

		GetBoundingBox( out Vector3 mins, out Vector3 maxs );
		SetupPhysicsFromOBB( PhysicsMotionType.Dynamic, mins, maxs );

		PhysicsEnabled = true;
		UsePhysicsCollision = true;

	}

	public void GenerateClient()
	{
		Model = JigsawGame.Current.PieceModels[Index];

		GetBoundingBox( out Vector3 mins, out Vector3 maxs );
		SetupPhysicsFromOBB( PhysicsMotionType.Dynamic, mins, maxs );
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

	[GameEvent.Tick]
	void Tick()
	{
		if ( Game.IsClient )
		{
			DebugOverlay.Text( "[" + X + ", " + Y + "]", Position );

			Vector3 center = Position + (Transform.Rotation.Up * (JigsawGame.PieceThickness*16));
			DebugOverlay.Line( center, center + (Transform.Rotation.Forward * 32), Color.Blue );
			DebugOverlay.Line( center, center + (Transform.Rotation.Right * 32), Color.Red );
			DebugOverlay.Line( center, center + (Transform.Rotation.Up * 32), Color.Green );

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
