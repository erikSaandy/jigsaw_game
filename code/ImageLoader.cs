using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Jigsaw;

public static class ImageLoader
{

	public static bool IsURL(string uri)
	{
		//Log.Error( "URL?" );
		//Uri uriResult;
		//bool v = Uri.TryCreate( uri, UriKind.Absolute, out uriResult );
		//Log.Error( uriResult );
		bool v = Uri.IsWellFormedUriString( uri, UriKind.Absolute );
		if ( v )
		{
			Log.Error( "is url" );
		}

		return v;
	}

	public static async Task<Texture> LoadWebImage( string URL )
	{
		Texture image = await Texture.LoadAsync( null, URL );

		// CHECK IF IMAGE IS GOOD.
		// CHECK ASPECT RATIO
		// CHECK SIZE

		return image;
	}

}
