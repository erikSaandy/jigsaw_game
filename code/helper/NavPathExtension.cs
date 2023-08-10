using Sandbox;
using System;
using static Sandbox.Material;
using System.Runtime.CompilerServices;
using Saandy;
using Jigsaw;

public static class NavPathExtension
{

	public static void Draw(this NavPath path, Color c, float duration = 0, bool depthTest = true )
	{
		NavPathSegment last = path.Segments[0];
		for ( int j = 1; j < path.Segments.Count; j++ )
		{
			DebugOverlay.Line( last.Position, path.Segments[j].Position, c, duration, depthTest );

			last = path.Segments[j];
		}
	}
}
