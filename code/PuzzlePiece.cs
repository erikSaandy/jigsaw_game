using Sandbox;
using System;
using System.Linq;
using Saandy;
using Jigsaw;

public partial class PuzzlePiece : ModelEntity
{
	[Net]	
	public int Index { get; private set; } = 0;

	[Net]
	public int X { get; set; } = 0;
	[Net]
	public int Y { get; set; } = 0;

	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		base.Spawn();
	}

	public PuzzlePiece() : base() { }

	public PuzzlePiece(int x, int y) : base()
	{
		this.X = x;
		this.Y = y;
		Index = Math2d.ArrayIndex( x, y, JigsawGame.Current.PieceCountX, JigsawGame.Current.PieceCountY );
		Tags.Add( "solid" );

		// Generate

		GetCollision( out Vector3 mins, out Vector3 maxs );
		SetupPhysicsFromOBB( PhysicsMotionType.Dynamic, mins, maxs );

		PhysicsEnabled = true;
		UsePhysicsCollision = true;

	}

	public void GenerateClient()
	{
		Model = JigsawGame.Current.PieceModels[Index];

		GetCollision( out Vector3 mins, out Vector3 maxs );
		SetupPhysicsFromOBB( PhysicsMotionType.Dynamic, mins, maxs );
	}

	private void GetCollision(out Vector3 mins, out Vector3 maxs)
	{
		// TODO: Edge pieces are inaccurate, and pips are not accounted for.

		float wMin = JigsawGame.GetWobbleAt( X * JigsawGame.PieceScale, Y * JigsawGame.PieceScale );
		float wMax = JigsawGame.GetWobbleAt( (X+1) * JigsawGame.PieceScale, (Y+1) * JigsawGame.PieceScale );

		mins = new Vector3(
			-(JigsawGame.PieceScale / 2) + wMin,
			-(JigsawGame.PieceScale / 2) + wMax,
			-(JigsawGame.PieceScale * JigsawGame.PieceThickness) / 2
		);

		maxs = new Vector3(
			(JigsawGame.PieceScale/2) + wMax,
			(JigsawGame.PieceScale/2) + wMax,
			(JigsawGame.PieceScale * JigsawGame.PieceThickness) / 2
		);

	}

	[GameEvent.Tick]
	void Tick()
	{

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
