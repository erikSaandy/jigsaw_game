using Saandy;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Jigsaw;

public partial class JigsawGame : GameManager
{

	// All PuzzlePiece entities (NETWORKED)
	[Net] public IList<PuzzlePiece> PieceEntities { get; set; } = null;

	// All piece models (CLIENT)
	public Model[] PieceModels { get; set; } = null;

	// // //

	[Net] public string PuzzleTextureURL { get; set; } = "https://previews.dropbox.com/p/thumb/AB4i65-H6QmvrFcgZSheLt4cE4evHLO7IaZEpSvh7cr0732SjmfGJJiP1lnLyEHrCOelc8CIX13FGk3QSC_cyTRyUFAvDArR1gHCVoyaKTyqPK52LrWwk8SjzBku20NvMqJsF9ZWIYrPR0T8x6282QIQswCqamGVyV3XWG7QhwnpNtpnz0M9nJZL1iBNf8fA3ciE8DYwYdMeGFkz9N0jrV4gmLfJ7BDDYijp0qMJ0EnXv-_0MYXx99xzgaqP8VxfYa1Il0_LVpjl65gLziYDa13omM5w4AMDWu-guN-btyAc8ip1IqWpxRLZn46GSBIhZ6BDlEzfxGFXMgnK8UmV4x4yuHw8eNCtOqup9mLmIjLNDFSFB_FF8QDGpk1N51ExGFg/p.png";
	public Texture PuzzleTexture { get; private set; } = null;

	public Material PuzzleMaterial { get; private set; } = null;
	public Material BacksideMaterial { get; private set; } = null;

	//[ConCmd.Client( "new_image" )]
	//public void NewImageFromDisk( string parameter )
	//{
	//	Log.Error( "yo" );
	//	Texture t = Texture.Load( FileSystem.Mounted, TextureAdress );
	//	string pathP = "textures/" + parameter;

	//	//Couldn't find image.
	//	if ( t == null )
	//	{
	//		Log.Error( pathP + " does not excist." );
	//		return;
	//	}

	//	TextureAdress = pathP;
	//	GetDimensions( t, out int pc );

	//	//TODO destroy last puzzle.
	//}

	public void PuzzleLoaderInit()
	{
		if ( Game.IsServer )
		{
			LoadEntities();
		}

	}

	/// <summary>
	/// Load all data we need to spawn in the puzzle pieces.
	/// </summary>
	public async void LoadEntities()
	{

		if ( Game.IsClient ) return;

		// Load texture on server.
		PuzzleTexture = await Texture.LoadAsync( FileSystem.Mounted, "textures/kittens.png" ); 
		//Texture.LoadAsync( null, PuzzleTextureURL );

		if ( PuzzleTexture == null ) { OnPuzzleTextureLoadFailed(); return; }

		// Get PieceCountX and PieceCountY
		await Task.RunInThreadAsync( () => GetDimensions( PuzzleTexture, out int pc ) );

		Log.Info( "Puzzle dimensions: " + PieceCountX + ", " + PieceCountY );
		Log.Info( "Puzzle piece count: " + PieceCountX * PieceCountY );

		// Spawning entity pieces //

		SpawnPuzzleEntities();

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
		int count = PieceCountX * PieceCountY;
		PieceEntities = new PuzzlePiece[count];

		List<PieceSpawner> spawners = Entity.All.OfType<PieceSpawner>().ToList();

		for ( int i = 0; i < count; i++ )
		{
			// Get x and y of piece.
			Math2d.FlattenedArrayIndex( i, PieceCountX, out int x, out int y ); 
			// Generate piece.
			PuzzlePiece ent = new PuzzlePiece( x, y );

			// Place piece //

			if ( spawners.Count > 0 )
			{
				ent.Position = GetSpawnPosition( ref spawners );
				ent.Rotation = new Rotation( 0, 0, 180, Rand.Next( 0, 360 ) );
			}
			else
			{
				// TODO: Use navmesh here
				Log.Warning( "There are no piece spawn points for this map! Piling in map center." );
			}

			PieceEntities[i] = ent; 
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

		// Remove single spawners from the list, except if it's the last spawner available (to avoid crashes :D)
		if ( spawners.Count > 1 && !spawners[id].MultipleSpawns )
		{
			spawners.RemoveAt( id );
		}

		return pos;
	}

	// Load materials, meshes and assign to piece entities.
	public async void LoadClientPieces()
	{
		if ( Game.IsServer ) return;

		PuzzleTexture = null;
		// Load texture on client.
		PuzzleTexture = Texture.Load( FileSystem.Mounted, "textures/kittens.png" );

		// Load materials.
		LoadPuzzleMaterials();
		// Generate meshes.
		await Task.RunInThreadAsync( () => GeneratePuzzle() );

		foreach ( PuzzlePiece piece in PieceEntities )
		{
			piece.GenerateClient();
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
		// Only load backside mat if it hasn't been loaded on client yet.
		BacksideMaterial = null;
		BacksideMaterial = Material.Load( "materials/jigsaw/jigsaw_back/jigsaw_back.vmat" );
		// Only load puzzle mat if it hasn't been loaded on client yet.
		PuzzleMaterial = null;
		PuzzleMaterial = Material.Load( "materials/jigsaw/default_image/jigsaw_default.vmat" );
		// Set puzzle texture.
		PuzzleMaterial.Set( "Color", PuzzleTexture );
	}

	/// <summary>
	/// Delete all piece entities, probably before creating new ones.
	/// </summary>
	private void DeletePieceEntities()
	{
		foreach(PuzzlePiece p in PieceEntities)
		{
			p.Delete();
		}
	}

}
