using Saandy;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Jigsaw;
public partial class JigsawGame : GameManager
{

	// All PuzzlePiece entities (NETWORKED)
	[Net] public IList<PuzzlePiece> PieceEntities { get; set; } = null;

	public PuzzlePiece GetPieceEntity(int x, int y)
	{
		int i = Math2d.ArrayIndex( x, y, PieceCountX, PieceCountY );
		return PieceEntities[i];
	}

	// All piece models (CLIENT)
	public Model[] PieceModels { get; set; } = null;		

	// // //

	[Net, Predicted] public string PuzzleTextureURL { get; set; } = "https://images3.alphacoders.com/116/1163888.jpg";

	public Texture PuzzleTexture { get; private set; } = null;

	public Material PuzzleMaterial { get; private set; } = null;
	public Material BacksideMaterial { get; private set; } = null;

	/// <summary>
	/// Load all data we need to spawn in the puzzle pieces.
	/// </summary>
	public async void LoadEntities()
	{
		if ( Game.IsClient ) return;

		// Load texture on server.
		PuzzleTexture = await Task.RunInThreadAsync( () => ImageLoader.LoadWebImage( PuzzleTextureURL ) );

		if ( PuzzleTexture == null ) { OnPuzzleTextureLoadFailed(); return; }

		GeneratePuzzle();

		Log.Info( "Puzzle dimensions: " + PieceCountX + ", " + PieceCountY );
		Log.Info( "Puzzle piece count: " + PieceCountX * PieceCountY );

		// Spawning entity pieces //

		//SpawnPuzzleEntities();
		SpawnPuzzlePiecesInGrid();

		await Task.Delay( 1000 );

		GameState = new PuzzlingGameState();

	}

	/// <summary>
	/// Spawn in the puzzle pieces on the server.
	/// </summary>
	public void SpawnPuzzleEntities()
	{
		// Only do this on server.
		if ( Game.IsClient ) return;
		DeletePieceEntities();

		int pieceCount = PieceCountX * PieceCountY;
		Current.PieceEntities = new PuzzlePiece[pieceCount];

		List<PieceSpawner> spawners = Entity.All.OfType<PieceSpawner>().ToList();
		IEnumerable<NavArea> areas = NavMesh.GetNavAreas();

		// Place piece //
		for ( int i = 0; i < pieceCount; i++ )
		{
			Math2d.FlattenedArrayIndex( i, PieceCountX, out int x, out int y );
			PuzzlePiece ent = new PuzzlePiece( x, y );

			// Get random nav area
			NavArea a = NavMesh.GetNavAreas().OrderBy( ( x ) => Guid.NewGuid() ).FirstOrDefault();

			//ent.Position = (Vector3)NavMesh.GetClosestPoint( new Vector3(Rand.Next(-1024, 1024 ), Rand.Next( -1024, 1024 ), Rand.Next(64, 256)) );
			ent.Position = a.FindRandomSpot() + (Vector3.Up * 64);
			ent.Rotation = new Rotation( 0, 0, 180, Rand.Next( 0, 360 ) );

			Current.PieceEntities[i] = ent;
		}

	}

	public void SpawnPuzzlePiecesInGrid(float spacing = 8f)
	{
		// Only do this on server.
		if ( Game.IsClient ) return;

		DeletePieceEntities();
		int count = PieceCountX * PieceCountY;
		Current.PieceEntities = new PuzzlePiece[count];

		for ( int i = 0; i < count; i++ )
		{
			// Get x and y of piece.
			Math2d.FlattenedArrayIndex( i, PieceCountX, out int x, out int y );
			// Generate piece.
			PuzzlePiece ent = new PuzzlePiece( x, y );

			// Place piece //
			Vector3 spawnArea = new Vector2( PieceCountX * (PieceScale + spacing), PieceCountY * (PieceScale + spacing) );
			Vector3 p = new Vector3( ent.X * (PieceScale + spacing), ent.Y * (PieceScale + spacing), 512 ) - (spawnArea / 2);
			ent.Position = Trace.Ray( p, p + Vector3.Down * 1024 ).StaticOnly().Run().EndPosition + (Vector3.Up * 4);

			Current.PieceEntities[i] = ent;
		}
	}

	/// <summary>
	/// Spawn puzzle pieces using map defined spawn points.
	/// </summary>
	/// <param name="spawners"></param>
	/// <returns></returns>
	private Vector3 GetSpawnPosition(ref List<PieceSpawner> spawners )
	{
		if(spawners.Count == 0)
		{
			return Vector3.Zero;
		}

		Vector3 pos;
		int id = Rand.Next( 0, spawners.Count - 1 );
		int radius = spawners[id].Radius - 16;

		int a = Rand.Next( 0, 360 );
		Vector3 dir = new Vector2( (float)Math.Sin( a ), (float)Math.Cos( a ) );
		int mag = Rand.Next( 0, radius );

		pos = spawners[id].Position + (dir * mag) + (Vector3.Up * Rand.Next( 2, 16 ));

		// Remove spawner from the list.
		spawners.RemoveAt( id );

		return pos;
	}

	public void LoadClientPieces()
	{

		Log.Warning( "loading pieces on client..." );

		// Load materials.
		//await Task.RunInThreadAsync( () => LoadPuzzleMaterials() );
		LoadPuzzleMaterials();

		// Generate meshes.
		//await Task.RunInThreadAsync( () => GeneratePuzzle() );
		GeneratePuzzle();

		for (int i = 0; i < Current.PieceEntities.Count; i++ )
		{
			Current.PieceEntities[i].GenerateClient();
		}

		Log.Info( "Loaded puzzle meshes on client!" );
	}

	private void OnPuzzleTextureLoadFailed()
	{
		Log.Error( "Puzzle Texture failed to load! (" + PuzzleTextureURL + ")" );
	}

	/// <summary>
	/// Load material on the client.
	/// </summary>
	public void LoadPuzzleMaterials()
	{
		PuzzleTexture = Task.RunInThreadAsync( () => ImageLoader.LoadWebImage( PuzzleTextureURL ) ).Result;
		//PuzzleTexture = await Task.RunInThreadAsync( () => ImageLoader.LoadWebImage( PuzzleTextureURL ) );

		// Only load backside mat if it hasn't been loaded on client yet.
		//BacksideMaterial = null;
		BacksideMaterial = Material.Load( "materials/jigsaw_backside/jigsaw_backside.vmat" );
		// Only load puzzle mat if it hasn't been loaded on client yet.
		//PuzzleMaterial = null;
		PuzzleMaterial = Material.Load( "materials/jigsaw_image/jigsaw_image.vmat" );
		// Set puzzle texture.
		PuzzleMaterial.Set( "Color", PuzzleTexture );
	}

	/// <summary>
	/// Delete all piece entities, probably before creating new ones.
	/// </summary>
	private void DeletePieceEntities()
	{
		foreach(PuzzlePiece p in Current.PieceEntities )
		{
			p?.Delete();
		}
	}

}
