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
	/// Takes a vector direction and returns an integer from 0 to 3.
	/// </summary>
	/// <param name="dir"></param>
	/// <returns></returns>
	public static int ToInt(this Vector2 dir )
	{
		dir = dir.Normal;
		return ((dir.Degrees / 90) + 0.25f).FloorToInt();
	}

}
