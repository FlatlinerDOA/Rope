namespace Rope;

public static class RopeExtensions
{
	public static Rope<char> ToRope(this string text)
	{
		return new Rope<char>(text.AsMemory());
	}

	public static Rope<T> ToRope<T>(this T[] array) where T : IEquatable<T>
	{
		return new Rope<T>(array.AsMemory());
	}
}