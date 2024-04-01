public static class MemorySize
{
	public static int SizeOf<T>() => System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
}
