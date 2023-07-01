using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Sandbox.UI;

namespace Jigsaw;

public static class ImageLoader
{

	public static readonly float MinimalAspectRatio = 0.125f;

	public static bool IsURL(string uri)
	{
		//Log.Error( "URL?" );
		//Uri uriResult;
		//bool v = Uri.TryCreate( uri, UriKind.Absolute, out uriResult );
		//Log.Error( uriResult );
		return Uri.IsWellFormedUriString( uri, UriKind.Absolute );
	}

	public static async Task<Texture> LoadWebImage( string URL )
	{
		Texture image = await Texture.LoadAsync( null, URL );

		return image;
	}

	public static bool TextureIsValid(Texture t, out string error)
	{
		error = "";

		if ( t == null ) { error = "Image is null!"; return false; } // Texture doesn't excist
		if(t.Width <= 1 && t.Height <= 1) { error = "Image has no pixels!"; return false; } // Texture doesn't have pixels.
		if ( t.IsRed32Error() ) { error = "Are you trying to break me? 😢"; return false; } // Special case for weird, red 32x32 errors texture (?)
		if(t.Aspect() < MinimalAspectRatio ) { error = "The aspect ratio is too whack!"; return false; } // Aspect ratio would break the puzzle (1:9+).

		//Log.Warning(t.GetPixels());
		return true;
	}

}
