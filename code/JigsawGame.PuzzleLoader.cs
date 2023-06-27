using jigsaw;
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
	[Net, Predicted] public IList<PuzzlePiece> PieceEntities { get; set; } = null;

	public PuzzlePiece GetPieceEntity(int x, int y)
	{
		int i = Math2d.ArrayIndex( x, y, PieceCountX, PieceCountY );
		return PieceEntities[i];
	}

	// All piece models (CLIENT)
	public Model[] PieceModels { get; set; } = null;		

	// // //

	[Net] public string PuzzleTextureURL { get; set; } = "https://images3.alphacoders.com/116/1163888.jpg";
	//https://lumiere-a.akamaihd.net/v1/images/p_ratatouille_19736_0814231f.jpeg

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
		//PuzzleTexture = await Texture.LoadAsync( FileSystem.Mounted, "textures/kittens.png" );   
		PuzzleTexture = await ImageLoader.LoadWebImage( PuzzleTextureURL );

		if ( PuzzleTexture == null ) { OnPuzzleTextureLoadFailed(); return; }

		// Get PieceCountX and PieceCountY
		await Task.RunInThreadAsync( () => GeneratePuzzle() ); //GetDimensions( PuzzleTexture, out int pc ) );

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

			// If map has spawners, prioritize.
			if ( spawners.Count > 0 )
			{
				ent.Position = GetSpawnPosition( ref spawners );
				ent.Rotation = new Rotation( 0, 0, 180, Rand.Next( 0, 360 ) );
			}
			else
			{
				// Get random nav area
				NavArea a = NavMesh.GetNavAreas().OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

				//ent.Position = (Vector3)NavMesh.GetClosestPoint( new Vector3(Rand.Next(-1024, 1024 ), Rand.Next( -1024, 1024 ), Rand.Next(64, 256)) );
				ent.Position = a.FindRandomSpot();
				ent.Rotation = new Rotation( 0, 0, 180, Rand.Next( 0, 360 ) );
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

		// Remove spawner from the list.
		spawners.RemoveAt( id );

		return pos;
	}

	// Load materials, meshes and assign to piece entities.
	public async void LoadClientPieces()
	{
		if ( Game.IsServer ) return;

		// Load texture on client.
		PuzzleTexture = null;
		PuzzleTexture = await ImageLoader.LoadWebImage( PuzzleTextureURL );

		// Load materials.
		LoadPuzzleMaterials();

		// Generate meshes.
		//await Task.RunInThreadAsync( () => GeneratePuzzle() );
		GeneratePuzzle();

		foreach ( PuzzlePiece piece in PieceEntities )
		{
			await Task.RunInThreadAsync(() => piece.GenerateClient());
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
		//BacksideMaterial = null;
		BacksideMaterial = Material.Load( "materials/jigsaw/jigsaw_back/jigsaw_back.vmat" );
		// Only load puzzle mat if it hasn't been loaded on client yet.
		//PuzzleMaterial = null;
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
