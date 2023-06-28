using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jigsaw;

public static class ImageLoader
{

	public static async Task<Texture> LoadWebImage(string URL )
	{
		Texture image = null;

		image = await Texture.LoadAsync( null, URL );

		return image;
	}



}
