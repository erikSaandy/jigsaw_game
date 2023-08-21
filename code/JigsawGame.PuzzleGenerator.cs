using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Saandy;
using Sandbox.Utility;

namespace Jigsaw;

public partial class JigsawGame
{

	public static readonly int BacksideUVTiling = 8;

	public static readonly float PieceThickness = 0.12f;

	public static int PieceScale = 24;
	public static readonly float PipScale = 0.35f;

	private static readonly int PipPointCount = 24;
	private static readonly int BodyPointCount = 32;

	private static bool IsGenerated { get; set; } = false;

	[Net] public int PieceCountX { get; set; } = 0;
	[Net] public int PieceCountY { get; set; } = 0;

	public static readonly float wobbleAmount = 0.15f;
	public static PieceMeshData[,] PieceMeshData { get; set; }

	public int divisionLevel = 1;

	private static MeshBuilder mesher = null;

	/// <summary>
	/// Generates puzzle piece meshes and populates PuzzleHelper.PieceModels on client.
	/// </summary>
    public void GeneratePuzzle() {

		float t = Time.Now;

        if (mesher == null)
            mesher = new MeshBuilder();

		if ( Game.IsServer )
		{
			// Get Piece scale
			Current.Controller = Entity.All.OfType<JigsawController>().FirstOrDefault();
			if ( Controller != null )
			{
				PieceScale = Math.Clamp( Current.Controller.PieceScale, 8, 64 );
			}
			else
			{
				// Just to simplify things. Use controller standard settings.
				Controller = new JigsawController();
			}
		}

		GetDimensions( PuzzleTexture, out int pc );

		PieceMeshData = new PieceMeshData[PieceCountX, PieceCountY];
		PieceModels = new Model[PieceCountX * PieceCountY];

		//Iterate through the array "randomly".
		int p = 37;
		int s = PieceCountX * PieceCountY;
		int q = p % s;
		for ( int i = 0; i < s; i++ )
		{
			GeneratePiece( q % PieceCountX, q / PieceCountX );
			q = (q + p) % s;
		}

		//for (int i = 0; i < PieceCountX * PieceCountY; i++ )
		//{
		//	Math2d.FlattenedArrayIndex( i, PieceCountX, out int x, out int y );
		//	GeneratePiece( x, y );
		//}

		IsGenerated = true;

		//GeneratePiece( Rand.Int( 0, width - 1 ), Rand.Int( 0, height - 1 ) );

		//GeneratePiece( 0, 0 );
	}


	public void GetDimensions( Texture tex, out int pieceCount )
	{

		int pictureWidth = tex == null? 10 : tex.Width;
		int pictureHeight = tex == null ? 10 : tex.Height;

		int maxD = pictureWidth > pictureHeight ? pictureWidth : pictureHeight;
		while(maxD > 20)
		{
			pictureWidth /= 2;
			pictureHeight /= 2;
			maxD /= 2;
		}

		PieceCountX = pictureWidth;
		PieceCountY = pictureHeight;

		//TODO: make this a bit nicer?
		// too many
		while ( PieceCountX * PieceCountY > 200 )
		{
			PieceCountX = (int)((float)PieceCountX * 0.75f );
			PieceCountY = (int)((float)PieceCountY * 0.75f );
		}

		// too few
		while ( PieceCountX * PieceCountY <= 50 )
		{
			PieceCountX = (int)((float)PieceCountX * 1.5f);
			PieceCountY = (int)((float)PieceCountY * 1.5f);
		}
		//


		pieceCount = PieceCountX * PieceCountY;
	}

	public void GeneratePiece( int x, int y ) {

		int wobbleLeft = x == 0 ? 0 : 1;
		int wobbleRight = x == PieceCountX - 1 ? 0 : 1;
		int wobbleBottom = y == 0 ? 0 : 1;
		int wobbleTop = y == PieceCountY - 1 ? 0 : 1;

		PieceMeshData pieceData = new PieceMeshData(
			x, y,

			// Edge data
			new Vector2(
				(x * PieceScale),
				(y * PieceScale) 
			),
			new Vector2(
				(x * PieceScale) ,
				(y * PieceScale) + PieceScale
			),
			new Vector2(
				(x * PieceScale) + PieceScale,
				(y * PieceScale) + PieceScale
			),
			new Vector2(
				(x * PieceScale) + PieceScale,
				(y * PieceScale)
			),

			wobbleLeft == 0,
			wobbleRight == 0,
			wobbleTop == 0,
			wobbleBottom == 0
		);


		GetBodyPoints( ref pieceData );

		EarClipping.Process( pieceData.polygon.contour, out pieceData.tris );

		// Mesh building //

		Vector3 pos = new Vector3( x, y ) * PieceScale;
		mesher.GenerateMesh( pieceData, pos, out Mesh[] m );

		// Set materials
		m[0].Material = PuzzleMaterial;
		m[1].Material = BacksideMaterial;

		int index = Math2d.ArrayIndex( x, y, PieceCountX, PieceCountY );

		// Create models
		PieceModels[index] = new ModelBuilder()
			.AddMeshes( m )
			.AddCollisionBox( new Vector3( Scale / 2, PieceScale / 2, PieceThickness / 2 ) )
			.WithMass( 50 )
			.WithSurface( "wood" )
			.Create();

		//// Add to piece data for reference from subsequent puzzle pieces.
		PieceMeshData[x, y] = pieceData;
	}

    private void GetBodyPoints(ref PieceMeshData piece) {

        int sidePointCount = BodyPointCount / 4;

        for (int sideId = 0; sideId < 4; sideId++) {
            piece.contourSideStartID[sideId] = piece.polygon.contour.Count;

            Math2d.Line side = piece.GetSide(sideId);


			Vector2 pointA = GetWobblePositionAt( side.pointA );
			Vector2 pointB = GetWobblePositionAt( side.pointB );
			// Switch points if adjacent side is on the edge. Don't use wobble.
			if ( piece.SideIsOnEdge( Math2d.ClampListIndex( sideId - 1, 4 ) ) ) { pointA = side.pointA; }
			if ( piece.SideIsOnEdge( Math2d.ClampListIndex( sideId + 1, 4 ) ) ) { pointB = side.pointB; }

			Vector2 sideDir = piece.GetSideNormal(sideId);
            bool needsPip = !piece.SideIsOnEdge(sideId); //  needs pip if side isn't on edge.

			// SIDE NEIGHBORS OTHER PIECE. Is piece generated yet?
			if ( needsPip && PieceMeshData[piece.x + (int)sideDir.x, piece.y + (int)sideDir.y] != null) {

				PieceMeshData neighbor = PieceMeshData[piece.x + (int)sideDir.x, piece.y + (int)sideDir.y];
                int neighborSideID = Math2d.ClampListIndex(sideId + 2, 4);
                int sideStartId = PieceMeshData[(int)neighbor.x, (int)neighbor.y].contourSideStartID[neighborSideID] + 1;

                neighborSideID = Math2d.ClampListIndex(sideId + 3, 4);
                int sideEndId = Math2d.ClampListIndex(PieceMeshData[(int)neighbor.x, (int)neighbor.y].contourSideStartID[neighborSideID] - 1, neighbor.polygon.contour.Count);            

                int sideLength = sideEndId - sideStartId + 1;

                List<Vector2> sidePoints = neighbor.polygon.contour.points.GetRange(sideStartId, sideLength);
                sidePoints.Reverse();

				piece.polygon.contour.points.Add( pointA );
				piece.polygon.contour.points.AddRange(sidePoints);
				piece.polygon.contour.points.Add( pointB );
				//piece.polygon.contour.points.Add( GetWobblePositionAt(side.pointB) );

			}
            else { // GENERATE SIDE

				float step = side.Magnitude / sidePointCount;

                float j = 0f;
                float pipStart = 0.43f * PieceScale;
                float pipEnd = 0.53f * PieceScale;

				#region new

				Color c = Color.Random;

				Vector2 tangentA, tangentB;

				// Get tangent of wobble at corners of piece
				if ( sideId == 0 ) // left
				{
					tangentA = GetWobbleVerticalTangent( piece.x, piece.y );
					tangentB = GetWobbleVerticalTangent( piece.x, piece.y + 1, true );
				}
				else if ( sideId == 1 ) // top
				{
					tangentA = GetWobbleHorizontalTangent( piece.x, piece.y + 1 );
					tangentB = GetWobbleHorizontalTangent( piece.x + 1, piece.y + 1, true );
				}
				else if ( sideId == 2 ) // right
				{
					tangentA = GetWobbleVerticalTangent( piece.x + 1, piece.y + 1, true );
					tangentB = GetWobbleVerticalTangent( piece.x + 1, piece.y );
				}
				else // bottom
				{
					tangentA = GetWobbleHorizontalTangent( piece.x + 1, piece.y, true );
					tangentB = GetWobbleHorizontalTangent( piece.x, piece.y );
				}

				bool wobble = true;
				if ( piece.SideIsOnEdge( sideId ) ) { wobble = false; }

				do
				{

					if ( needsPip && j + step >= pipStart )
					{
						Vector2 start = GetPointAlongCurve( pipStart / side.Magnitude );
						Vector2 end = GetPointAlongCurve( pipEnd / side.Magnitude );
						AddPipPoints( ref piece, sideId, start, end );

						j = pipEnd + 0.1f;
						needsPip = false;
					}

					Vector2 p = Vector2.Zero;

					if ( wobble ) { p = GetPointAlongCurve( j / side.Magnitude ); }
					else { p = side.pointA + (side.Direction * j); }

					//if ( Game.IsClient && wobble ) Math2d.DrawPoint( p, c, 35, 1f );
					//if ( Game.IsClient && !wobble ) Math2d.DrawPoint( p, Color.White, 35, 2f );

					piece.polygon.contour.Add( p );

					j += step;

				} while ( j < side.Magnitude);

				Vector2 GetPointAlongCurve(float t)
				{

					float dst = side.Magnitude * 0.15f;
					Vector2 a = pointA;
					Vector2 b = pointA + (tangentA * dst);
					Vector2 c = pointB + (tangentB * dst);
					Vector2 d = pointB;

					//Vector2 point = Math2d.QuadraticCurve( pointA, pointA + (side.Direction * side.Magnitude / 2), pointB, t );
					Vector2 point = Math2d.CubicCurve( a, b, c, d, t );


					/* DEBUGGING TANGENTS
					 
					Color col = Color.Random;

					switch ( sideId )
					{
						case 0:
							col = Color.Red;
							break;
						case 1:
							col = Color.Green;
							break;
						case 2:
							col = Color.Blue;
							break;
						case 3:
							col = Color.Yellow;
							break;
					}

					DebugOverlay.Line( pointA, pointA + tangentA * 9, Color.Red, 10 );
					DebugOverlay.Line( pointB + tangentB * 9, d, Color.Blue, 10 );
					Math2d.DrawPoint( point, col, 10, 2f );
					*/

					return point;
				}

				#endregion
			}

		}
    }

    private void AddPipPoints(ref PieceMeshData piece, int sideIndex, Vector2 start, Vector2 end) {
        Vector2 a, b, c, d;

        Vector2 up = Vector2.Zero;
        switch (sideIndex) {
            case 0:
                up = Vector2.Right;
                break;
            case 1:
                up = Vector2.Up;
                break;
            case 2:
                up = Vector2.Left;
                break;
            case 3:
                up = Vector2.Down;
                break;
        }

        Vector2 left = Math2d.RotateByAngle(up, -90f) * PieceScale;
        Vector2 right = Math2d.RotateByAngle(up, 90f ) * PieceScale;
		up *= PieceScale;

        Math2d.Line side = piece.GetSide(sideIndex);

		// Cublic curve for pips.
        a = start;
        b = a + (up * PipScale) + (left * PipScale);
        d = end;
        c = d + (up * PipScale) + (right * PipScale);

        int pointCount = PipPointCount;
        for (int i = 0; i <= pointCount; i++) {
            float t = ((i) / (float)pointCount);
            Vector2 p = Math2d.CubicCurve(a, b, c, d, t);
            piece.polygon.contour.Add(p);
			//Math2d.DrawPoint( p, Color.Red, 35, 1f );
		}

		piece.pipCenters[sideIndex] = ((a + b + c + d) / 4) - (new Vector2(piece.x, piece.y) * PieceScale) - (Vector2.One * (PieceScale/2));
	}

	public static Vector2 GetWobbleHorizontalTangent(float x, float y, bool reverse = false)
	{
		float dst = 0.15f;
			
		Vector2 dir = new Vector2( 1, 0 );
		Vector2 pos = new Vector2( x, y );
		Vector2 a = GetWobbleAt( pos - (dir * dst / 2) );
		Vector2 b = GetWobbleAt( pos + (dir * dst / 2) );

		float adj = dst;
		float opp = ( b - a ).Length;
		float rad = MathF.Atan( opp / adj );
		Vector2 tangent = new Vector2( MathF.Cos( rad ), MathF.Sin( rad ) );

		if ( b.x > a.x ) { tangent.y = -tangent.y; }
		if(reverse) { tangent = -tangent; }

		return tangent;
	}

	public static Vector2 GetWobbleVerticalTangent( float x, float y, bool reverse = false )
	{
		float dst = 0.15f;

		Vector2 dir = new Vector2( 0, 1 );
		Vector2 pos = new Vector2( x, y );
		Vector2 a = GetWobbleAt( pos - (dir * dst / 2) );
		Vector2 b = GetWobbleAt( pos + (dir * dst / 2) );

		float adj = dst;
		float opp = (b - a).Length;
		float rad = MathF.Atan( adj / adj );
		Vector2 tangent = new Vector2( MathF.Cos( rad ), MathF.Sin( rad ) );

		if ( b.y < a.y ) { tangent.x = -tangent.x; }
		if ( reverse ) { tangent = -tangent; }

		return tangent;
	}

	public static Vector2 GetWobbleAt( Vector2 pos )
	{

		Vector2 v = new Vector2( (MathF.Sin( pos.x * JigsawGame.Current.PieceCountX * 0.2f ) * 0.9f), (MathF.Sin( pos.y * JigsawGame.Current.PieceCountX * 0.2f ) * 0.9f) );

		return v;

	}

	public static Vector2 GetWobblePositionAt( Vector2 pos )
	{

		Vector2 v = GetWobbleAt( pos );
		return pos + v;

	}

}

public partial class PieceMeshData {

    public int x, y;

    public Triangulation.Polygon polygon;

    public List<Triangle> tris;

    public Vector2[] corners;
    private readonly Math2d.Line[] straightSides;
    private readonly bool[] isEdge;

	public Vector2[] pipCenters;

    // What ID is the first point of the side?
    public int[] contourSideStartID;

    public PieceMeshData(int x, int y, Vector2 bl, Vector2 tl, Vector2 tr, Vector2 br, bool edgeLeft, bool edgeRight, bool edgeTop, bool edgeBottom) {
        this.x = x;
        this.y = y;
		
        polygon = new Triangulation.Polygon();

        corners = new Vector2[4] { bl, tl, tr, br };
		straightSides = new Math2d.Line[4] { new Math2d.Line(bl, tl), new Math2d.Line(tl, tr), new Math2d.Line(tr, br), new Math2d.Line(br, bl) };
		Color c = Color.Random;

		//DebugOverlay.Line( bl, tl, c, 50 );
		//DebugOverlay.Line( tl, tr, c, 50 );
		//DebugOverlay.Line( tr, br, c, 50 );
		//DebugOverlay.Line( br, bl, c, 50 );

		isEdge = new bool[4] { edgeLeft, edgeTop, edgeRight, edgeBottom };
        center.Set((corners[0].x + corners[3].x) / 2, (corners[1].y + corners[0].y) / 2);

		pipCenters = new Vector2[4];

        contourSideStartID = new int[4];
    }

    private Vector2 center = Vector2.Zero;
    public Vector2 Center => center;
  

    public Vector2 GetSideNormal(int index) {
        switch (index) {
            case 0:
                return new Vector2(-1, 0); // left
            case 1:
                return new Vector2(0, 1);
			case 2:
				return new Vector2(1, 0);  // right
            case 3:
                return new Vector2(0, -1);  // bottom
            default:
                return Vector2.Zero;
        } // top

    }

    // left, top, right, bottom
    public Math2d.Line GetSide(int index) { return straightSides[index]; }
    public bool SideIsOnEdge(int side) { 
		if ( side < 0 || side >= 4 ) { return false; } // outside array

		return isEdge[side]; 
	}

}

public class MeshBuilder
{
	public static VertexBuffer vb;
	private int vertexCount;
	public int mesh1VertexCount { get; private set; }

	public MeshBuilder() {
		vb = new VertexBuffer();
		vb.Init( true );
	}

	public void GenerateMesh( PieceMeshData piece, Vector2 position, out Mesh[] m )
	{
		vb.Clear();
		vertexCount = 0;

		int triCount = piece.tris.Count;

		// Front face.
		for ( int i = 0; i < triCount; i++ )
		{
			AddTri( piece.tris[i], position );
		}

		mesh1VertexCount = vertexCount;

		// Back face.
		for ( int i = 0; i < triCount; i++ )
		{
			AddTri( piece.tris[i], position, true );
		}

		AddTrim( position, piece );


		Material mat = JigsawGame.Current.PuzzleMaterial;
		Mesh m1 = new Mesh( mat );
		m1.Material = mat;
		m1.SetBounds( -JigsawGame.PieceScale / 2, JigsawGame.PieceScale / 2 );
		m1.CreateBuffers( vb, true );
		m1.SetIndexRange( 0, mesh1VertexCount );


		mat = JigsawGame.Current.BacksideMaterial;
		Mesh m2 = new Mesh( mat );
		m2.Material = mat;
		m2.SetBounds( -JigsawGame.PieceScale / 2, JigsawGame.PieceScale / 2 );
		m2.CreateBuffers( vb, true );
		m2.SetIndexRange( mesh1VertexCount, vertexCount );

		m = new Mesh[2] { m1, m2 };

	}

	private void AddTri(Saandy.Triangle tri, Vector3 position, bool backside = false)
	{
		AddTri( tri.v1, tri.v2, tri.v3, position, backside );
	}

	private void AddTri( Vector3 v1, Vector3 v2, Vector3 v3, Vector3 position, bool backside = false )
	{
		//position += new Vector3( PuzzleGenerator.scale, PuzzleGenerator.scale, 0 ) / 2;
		position += new Vector3( JigsawGame.PieceScale, JigsawGame.PieceScale, 0 ) / 2;
		Vector3 thicknessOffset = Vector3.Up * JigsawGame.PieceScale * (JigsawGame.PieceThickness / 2);

		float uMax = JigsawGame.PieceScale * JigsawGame.Current.PieceCountX;
		float vMax = JigsawGame.PieceScale * JigsawGame.Current.PieceCountY;

		if (backside)
		{
			vb.AddTriangle(
				new Sandbox.Vertex(
					v3 - position - thicknessOffset,
					Vector3.Down,
					Vector3.Backward,
					Vector2.One - new Vector2( v3.x / (uMax / JigsawGame.BacksideUVTiling), v3.y / (uMax / JigsawGame.BacksideUVTiling) ) ),

				new Sandbox.Vertex(
					v2 - position - thicknessOffset,
					Vector3.Down,
					Vector3.Backward,
					Vector2.One - new Vector2( v2.x / (uMax / JigsawGame.BacksideUVTiling), v2.y / (uMax / JigsawGame.BacksideUVTiling) ) ),

				new Sandbox.Vertex(
					v1 - position - thicknessOffset,
					Vector3.Down,
					Vector3.Backward,
					Vector2.One - new Vector2( v1.x / (uMax / JigsawGame.BacksideUVTiling), v1.y / (uMax / JigsawGame.BacksideUVTiling) ) )
			);
		}	
		else
		{
			vb.AddTriangle(
				new Sandbox.Vertex(
					v1 - position + thicknessOffset,
					Vector3.Up,
					Vector3.Forward,
					new Vector2( v1.x / uMax, 1 - (v1.y / vMax) ) ),

				new Sandbox.Vertex(
					v2 - position + thicknessOffset,
					Vector3.Up,
					Vector3.Forward,
					new Vector2( v2.x / uMax, 1 - (v2.y / vMax) ) ),

				new Sandbox.Vertex(
					v3 - position + thicknessOffset,
					Vector3.Up,
					Vector3.Forward,
					new Vector2( v3.x / uMax, 1 - (v3.y / vMax) ) )
			);
		}

		vertexCount += 3;

	}

	private void AddTrim( Vector2 position, PieceMeshData piece )
	{
		position += new Vector2( JigsawGame.PieceScale ) / 2;
		Vector3 thicknessOffset = Vector3.Up * JigsawGame.PieceScale * (JigsawGame.PieceThickness / 2);

		// Initial points on trim //
		Vector3 a = ( Vector3)piece.polygon.contour.points[0] - (Vector3)position - thicknessOffset;
		Vector3 b = ( Vector3)piece.polygon.contour.points[0] - (Vector3)position + thicknessOffset;
		Vector3 c = ( Vector3)piece.polygon.contour.points[1] - (Vector3)position + thicknessOffset;
		Vector3 d = ( Vector3)piece.polygon.contour.points[1] - (Vector3)position - thicknessOffset;

		Vector3 tangent = (d - a);
		Vector3 nrmlB = Vector3.Cross( tangent, (a - b) );

		int contourCount = piece.polygon.contour.Count;
		float uvStepX = 0.1f * (d - a).Length / JigsawGame.BacksideUVTiling;
		float uvStepY = JigsawGame.PieceThickness / 2f;

		vb.AddQuad(
			new Sandbox.Vertex( a, nrmlB, tangent, new Vector2( 0, 0 ) ),
			new Sandbox.Vertex( b, nrmlB, tangent, new Vector2( 0, uvStepY ) ),
			new Sandbox.Vertex( c, nrmlB, tangent, new Vector2( uvStepX, uvStepY )),
			new Sandbox.Vertex( d, nrmlB, tangent, new Vector2( uvStepX, 0 ) ) 
		);

		vertexCount += 4;

		float cStep = 0;
		float angle = 0;
		for ( int i = 0; i < contourCount; i++ )
		{

			nrmlB = Vector3.Cross( tangent, (a - b).Normal );

			a = d;
			b = c;
			c = (Vector3)piece.polygon.contour.points[Math2d.ClampListIndex( i + 1, contourCount )] - (Vector3)position + thicknessOffset;
			d = (Vector3)piece.polygon.contour.points[Math2d.ClampListIndex( i + 1, contourCount )] - (Vector3)position - thicknessOffset;

			tangent = (d - a);
			Vector3 nrmlA = Vector3.Cross( tangent, (a - b) );

			cStep += uvStepX;
			uvStepX = 0.1f * (d - a).Length / JigsawGame.BacksideUVTiling;

			vertexCount += 4;

			vb.AddQuad(
				new Sandbox.Vertex( a, nrmlA, tangent, new Vector2( cStep, 0 ) ),
				new Sandbox.Vertex( b, nrmlA, tangent, new Vector2( cStep, uvStepY ) ),
				new Sandbox.Vertex( c, nrmlA, tangent, new Vector2( cStep + uvStepX, uvStepY ) ),
				new Sandbox.Vertex( d, nrmlA, tangent, new Vector2( cStep + uvStepX, 0 ) )
			);

			angle = Math.Abs( Math2d.Angle3D( nrmlB, nrmlA, a-b ) );

		}
	}

}

