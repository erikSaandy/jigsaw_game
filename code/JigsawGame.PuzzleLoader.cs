using Saandy;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Jigsaw;
public partial class JigsawGame
{

	// All PuzzlePiece entities (NETWORKED)
	[Net] public IList<PuzzlePiece> PieceEntities { get; set; }

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
	public async void LoadServerPieces()
	{
		if ( Game.IsClient ) return;

		if ( !NavMesh.IsLoaded )
		{
			ChatBox.SayWarning( "This map doesn't have a NavMesh! This gamemode needs a NavMesh to work." );
			ChatBox.SayWarning( "Contact the map maker or pick another map." );
			return;
		}

		// Load texture on server.
		PuzzleTexture = await Task.RunInThreadAsync( () => ImageLoader.LoadWebImage( PuzzleTextureURL ) );

		if ( PuzzleTexture == null ) { OnPuzzleTextureLoadFailed(); return; }

		GeneratePuzzle();

		Log.Info( "Puzzle dimensions: " + PieceCountX + ", " + PieceCountY );
		Log.Info( "Puzzle piece count: " + PieceCountX * PieceCountY );

		// Spawning entity pieces //
		//SpawnPuzzlePiecesOnNavMesh();
		SpawnPuzzlePieces();

		await Task.Delay( 1000 );

		GameState = new PuzzlingGameState();

	}


	public void PositionPuzzlePiecesOnNavMesh() { Task.RunInThreadAsync( () => PositionPuzzlePiecesOnNavMeshAsync() ); }

	/// <summary>
	/// place puzzle pieces on the nav mesh.
	/// </summary>
	private async Task PositionPuzzlePiecesOnNavMeshAsync()
	{
		// Only do this on server.
		if ( Game.IsClient ) return;
		if ( PieceEntities == null ) return;

		int count = PieceCountX * PieceCountY;

		foreach(PuzzlePiece p in PieceEntities )
		{
			p.EnableDrawing = false;
		}

		//List<PieceSpawner> spawners = Entity.All.OfType<PieceSpawner>().ToList();
		IEnumerable<NavArea> areas = NavMesh.GetNavAreas();
		NavArea a = areas.FirstOrDefault();
		Vector3 center = NavMesh.GetClosestPoint( areas.FirstOrDefault().Center ).Value;

		DebugOverlay.Sphere( center, 64, Color.Yellow, 64 );

		// Place pieces //
		NavPath pth;
		for ( int i = 0; i < count; i++ )
		{

			// Get random nav area
			a = areas.OrderBy( ( x ) => Guid.NewGuid() ).FirstOrDefault();
			Vector3 pos = a.FindRandomConnectedPoint( center, NavAgentHull.Agent1, 4096, 64 );

			try
			{

				pth = await Task.RunInThreadAsync( () => NavMesh.PathBuilder( pos )
				.WithPartialPaths()
				.WithMaxClimbDistance(32)
				.WithMaxDropDistance(32)
				.Build( center ) );

				if ((pth.Segments.Last().Position - center).Length > 30)
				{
					i--;
					continue;
				}

			}
			catch
			{
				i--;
				continue;
			}

			if ( JigsawGame.Current.Debug )
			{
				Color c = Color.Random;
				pth.Draw( c, 20 );
			}

			PlacePiece( pos );

			void PlacePiece( Vector3 p )
			{
				PieceEntities[i].Position = NavMesh.GetClosestPoint(p).Value + Vector3.Up * 4;
				PieceEntities[i].Rotation = new Rotation( 0, 0, 180, Rand.Next( 0, 360 ) );
			}

		}

		foreach ( PuzzlePiece p in PieceEntities )
		{
			p.EnableDrawing = true;
			p.PhysicsBody.AutoSleep = false;
		}

	}

	public void SpawnPuzzlePieces(float spacing = 8f)
	{
		// Only do this on server.
		if ( Game.IsClient ) return;

		DeletePieceEntities();
		int count = PieceCountX * PieceCountY;

		PieceEntities = new List<PuzzlePiece>();
		for ( int i = 0; i < count; i++ )
		{
			// Get x and y of piece.
			Math2d.FlattenedArrayIndex( i, PieceCountX, out int x, out int y );
			// Generate piece.
			PuzzlePiece ent = new PuzzlePiece( x, y );

			PieceEntities.Add( ent );
			ent.PhysicsBody.AutoSleep = false;

			// Place piece //
			Vector3 spawnArea = new Vector2( PieceCountX * (PieceScale + spacing), PieceCountY * (PieceScale + spacing) );
			Vector3 p = new Vector3( ent.X * (PieceScale + spacing), ent.Y * (PieceScale + spacing), 512 ) - (spawnArea / 2);
			//ent.Position = Trace.Ray( p, p + Vector3.Down * 1024 ).StaticOnly().Run().EndPosition + (Vector3.Down * 512);
			ent.Position = NavMesh.GetNavAreas().FirstOrDefault().Center + Vector3.Up * 4;
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
		Log.Info( "Loading client puzzle pieces..." );
		
		// Load materials.
		//await Task.RunInThreadAsync( () => LoadPuzzleMaterials() );
		Current.Task.RunInThreadAsync(() => Current.LoadPuzzleMaterials());

		// Generate meshes.
		//await Task.RunInThreadAsync( () => GeneratePuzzle() );
		Current.GeneratePuzzle();

		for ( int i = 0; i < Current.PieceEntities.Count; i++ )
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
	public async Task LoadPuzzleMaterials()
	{
		PuzzleTexture = await Task.RunInThreadAsync( () => ImageLoader.LoadWebImage( PuzzleTextureURL ) );
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
