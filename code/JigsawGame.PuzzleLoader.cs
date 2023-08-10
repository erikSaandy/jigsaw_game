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
	[Net, Predicted] public IList<PuzzlePiece> PieceEntities { get; set; } = null;

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

		// Load texture on server.
		PuzzleTexture = await Task.RunInThreadAsync( () => ImageLoader.LoadWebImage( PuzzleTextureURL ) );

		if ( PuzzleTexture == null ) { OnPuzzleTextureLoadFailed(); return; }

		GeneratePuzzle();

		Log.Info( "Puzzle dimensions: " + PieceCountX + ", " + PieceCountY );
		Log.Info( "Puzzle piece count: " + PieceCountX * PieceCountY );
			
		// Spawning entity pieces //
		//SpawnPuzzlePiecesOnNavMesh();
		SpawnPuzzlePiecesInGrid();

		await Task.Delay( 3000 );

		GameState = new PuzzlingGameState();

	}

	/// <summary>
	/// place puzzle pieces on the nav mesh.
	/// </summary>
	public async void PositionPuzzlePiecesOnNavMesh()
	{
		// Only do this on server.
		if ( Game.IsClient ) return;
		if ( Current.PieceEntities == null ) return;

		int count = PieceCountX * PieceCountY;

		foreach(PuzzlePiece p in Current.PieceEntities)
		{
			p.EnableDrawing = false;
		}

		//List<PieceSpawner> spawners = Entity.All.OfType<PieceSpawner>().ToList();
		IEnumerable<NavArea> areas = NavMesh.GetNavAreas();
		NavArea a = areas.FirstOrDefault();
		Vector3 center = NavMesh.GetClosestPoint( areas.FirstOrDefault().Center ).Value;

		/*
		// Place piece //
		Vector3[] path;

		for ( int i = 0; i < count; i++ )
		{
			// Get random nav area
			a = areas.OrderBy( ( x ) => Guid.NewGuid() ).FirstOrDefault();
			Vector3 pos = a.FindRandomSpot();

			//// path

			try
			{
				path = await Task.RunInThreadAsync( () => NavMesh.BuildPath( pos, center ) );

				// path didn't reach center
				if ( (path[path.Length-1] - center).Length > 64 )
				{
					i--;
					continue;
				}


				//float dst = (path[path.Length - 1] - center).Length;

			}
			catch
			{
				i--;
				continue;
			}

			if ( JigsawGame.Current.Debug )
			{
				Color c = Color.Random;
				path.DrawAsPath( c, 100 );
			}

			Current.PieceEntities[i].Position = pos;
			Current.PieceEntities[i].Rotation = new Rotation( 0, 0, 180, Rand.Next( 0, 360 ) );
			Current.PieceEntities[i].EnableDrawing = true;

		}
		*/

		NavPath pth;

		// Place piece //
		for ( int i = 0; i < count; i++ )
		{
			// Get random nav area
			a = areas.OrderBy( ( x ) => Guid.NewGuid() ).FirstOrDefault();
			Vector3 pos = a.FindRandomSpot();

			//// path

			try
			{
				pth = NavMesh.PathBuilder( pos )
				.WithMaxClimbDistance( 24 )
				.WithMaxDropDistance( 24 )
				.WithAgentHull( NavAgentHull.Agent1 )
				.WithStepHeight( 12 )
				.WithNoOptimization()
				.WithPartialPaths()
				.Build( center ) ;

				if((pth.Segments.Last().Position - center).Length > 64)
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
				pth.Draw( c, 100 );
			}

			Current.PieceEntities[i].Position = pos;
			Current.PieceEntities[i].Rotation = new Rotation( 0, 0, 180, Rand.Next( 0, 360 ) );

		}

		foreach ( PuzzlePiece p in Current.PieceEntities )
		{
			p.EnableDrawing = true;
		}

	}

	public void SpawnPuzzlePiecesInGrid(float spacing = 8f)
	{
		// Only do this on server.
		if ( Game.IsClient ) return;

		using ( Prediction.Off() )
		{
			DeletePieceEntities();
			int count = PieceCountX * PieceCountY;
			Current.PieceEntities = new PuzzlePiece[count];
			for ( int i = 0; i < count; i++ )
			{
				// Get x and y of piece.
				Math2d.FlattenedArrayIndex( i, PieceCountX, out int x, out int y );
				// Generate piece.
				PuzzlePiece ent = new PuzzlePiece( x, y );

				Current.PieceEntities[i] = ent;

				// Place piece //
				Vector3 spawnArea = new Vector2( PieceCountX * (PieceScale + spacing), PieceCountY * (PieceScale + spacing) );
				Vector3 p = new Vector3( ent.X * (PieceScale + spacing), ent.Y * (PieceScale + spacing), 512 ) - (spawnArea / 2);
				ent.Position = Trace.Ray( p, p + Vector3.Down * 1024 ).StaticOnly().Run().EndPosition + (Vector3.Up * 4);
			}
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
