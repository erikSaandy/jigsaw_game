using Sandbox;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jigsaw;

public static class EntityExtension
{
	public static async Task PlaySoundDelayed(this Entity e, string sound, int milisecondsDelay = 0, string attachment = null )
	{
		await Task.Delay( milisecondsDelay );
		e.PlaySound( sound, attachment );
	}

}
