using Sandbox;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jigsaw;

public static class EntityExtension
{
	public static Vector3 Up( this Sandbox.Entity entity ) => entity.Rotation * Vector3.Up;

	public static Vector3 Down( this Sandbox.Entity entity ) => -entity.Up();

	public static Vector3 Right( this Sandbox.Entity entity ) => entity.Rotation * Vector3.Right;

	public static Vector3 Left( this Sandbox.Entity entity ) => -entity.Right();

	public static async Task PlaySoundDelayed(this Entity e, string sound, int milisecondsDelay = 0, string attachment = null )
	{
		await Task.Delay( milisecondsDelay );
		e.PlaySound( sound, attachment );
	}

}
