using Sandbox;
using System.Collections;
using System.Collections.Generic;

public static class Vector2Extension
{
    public static void Set(this Vector2 v, float x, float y) {
		v.x = x;
		v.y = y;
    }


	/// <summary>
	/// Takes a vector direction and returns an integer representing a quadrant (0 ... 3)
	/// </summary>
	/// <param name="dir"></param>
	/// <returns></returns>
	public static int DirectionToQuadrant(this Vector2 dir )
	{

		dir = dir.Normal;
		if(dir.Length == 0) { Log.Warning( "No direction!" ); return -1; }

		return ((dir.Degrees / 90) + 0.25f).FloorToInt();
	}

}
