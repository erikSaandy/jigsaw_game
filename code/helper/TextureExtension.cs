using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

static class TextureExtension
{
	/// <summary>
	/// Red32 is a fully red 32x32 pixels texture that occurs when loading URL images in weird ways.
	/// </summary>
	/// <param name="t"></param>
	/// <returns></returns>
	public static bool IsRed32Error(this Texture t)
	{
		if ( t.Width != 32 || t.Height != 32 ) { return false; }

		Color32 red = new Color32( 255, 0, 0 );
		for ( int i = 0; i < t.Width; i++ )
		{
			if ( t.GetPixel( i, 2 ) != red ) { return false; }
		}

		return true;
	}

	public static float Aspect(this Texture t)
	{
		float a, b = 0;

		if(t.Height < t.Width )
		{
			a = t.Height;
			b = t.Width;
		}
		else
		{
			a = t.Width;
			b = t.Height;
		}
		return a / b;

	}
}
