using Sandbox;
using System;

public static class EntityExtension
{
	public static Vector3 Up( this Sandbox.Entity entity ) => entity.Rotation * Vector3.Up;

	public static Vector3 Down( this Sandbox.Entity entity ) => -entity.Up();

	public static Vector3 Right( this Sandbox.Entity entity ) => entity.Rotation * Vector3.Right;

	public static Vector3 Left( this Sandbox.Entity entity ) => -entity.Right();

}
