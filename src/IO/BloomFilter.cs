namespace Rope.IO;

using System.Collections;
using System.Diagnostics.Contracts;
using System.Numerics;
using Rope;

public record class BloomFilter
{
	/// <summary>
	/// A token that is used as a text terminator. It is classified as UnicodeCategory.PrivateUse
	/// </summary>
	private const char EndOfTextToken = (char)0xF000;

	private BitArray bits;

	public BloomFilter(int size, int hashCount, string runLengthEncodedBits, SupportedOperationFlags supportedOperations)
	{
		this.Size = size;
		this.HashCount = hashCount;
		this.SerializedBits = runLengthEncodedBits;
        this.SupportedOperations = supportedOperations;
    }
	
	public BloomFilter(int size, int hashCount, SupportedOperationFlags supportedOperations, bool[]? bits = null)
	{
		this.Size = size;
		this.HashCount = hashCount;
		this.SupportedOperations = supportedOperations;
	   	this.bits = new BitArray(bits ?? new bool[size]);
	}

	/// <summary>
	/// Gets the number of iterations the bloom filter will perform when hashing values.
	/// </summary>
	public int HashCount { get; init; }
	
	/// <summary>
	/// Gets the number of bits in the bloom filter.
	/// </summary>
	public int Size { get; init; }

	/// <summary>
	/// Gets the bits from the bloom filter.
	/// </summary>
	public bool[] Bits
	{
		get 
		{
			var x = new bool[this.bits.Length];
			this.bits.CopyTo(x, 0);
			return x;
		}
	}
	
	/// <summary>
	/// Gets the fill ratio as a number between 0.0 and 1.0. A bloom filter should generally have a fill rate below 0.5 to be effective. 
	/// Above 0.5 (50% fill ratio), the false positive rate grows exponentially.
	/// </summary>
	public double FillRatio => this.Bits.Length > 0 ? this.FillCount / (double)this.Bits.Length : 0;
	
	/// <summary>
	/// Gets the number of used bits in the bloom filter.
	/// </summary>
	public int FillCount
	{
		get
		{
			int[] ints = new int[(this.bits.Length + 31) / 32];
			this.bits.CopyTo(ints, 0);

			int count = 0;
			for (int i = 0; i < ints.Length; i++)
			{
				count += BitOperations.PopCount((uint)ints[i]);
			}

			return count;
		}
	}

	/// <summary>
	/// Gets or sets the bits as a run-length byte array which is then encoded as a Base-64 string.
	/// </summary>
	public string SerializedBits
    {
        get => Convert.ToBase64String(RunLengthEncode(this.Bits));
        set
        {
            byte[] rleData = Convert.FromBase64String(value);
            this.bits = new BitArray(RunLengthDecode(rleData, Size));
        }
    }

    public SupportedOperationFlags SupportedOperations { get; }

    public static byte[] RunLengthEncode(bool[] data)
    {
        var result = Rope<byte>.Empty;
        int count = 1;
        bool current = data[0];

		for (int i = 1; i < data.Length; i++)
		{
			if (data[i] == current && count < 255)
			{
				count++;
			}
			else
			{
				result += (byte)(current ? count | 0x80 : count);
				current = data[i];
				count = 1;
			}
		}
		result += (byte)(current ? count | 0x80 : count);

		return result.ToArray();
	}

	public static bool[] RunLengthDecode(Rope<byte> rleData, int originalLength)
	{
		var result = new bool[originalLength];
		int index = 0;

		foreach (byte b in rleData)
		{
			bool value = (b & 0x80) != 0;
			int count = b & 0x7F;

			for (int i = 0; i < count && index < originalLength; i++)
			{
				result[index++] = value;
			}
		}

		return result;
	}

	public void Add(string item)
    {
		if (this.SupportedOperations.HasFlag(SupportedOperationFlags.Contains))
		{
			for (int i = 0; i < Math.Min(item.Length, this.Size); i++)
			{
				AddCharAtIndex(item[i], int.MaxValue);
			}
		}
		else
		{
			if (this.SupportedOperations.HasFlag(SupportedOperationFlags.StartsWith))
			{
				for (int i = 0; i < Math.Min(item.Length, this.Size); i++)
				{
					AddCharAtIndex(item[i], i + 1);
				}

				if (this.SupportedOperations.HasFlag(SupportedOperationFlags.Equals)) 
				{
					AddCharAtIndex(EndOfTextToken, item.Length + 1);
				}
			}

			if (this.SupportedOperations.HasFlag(SupportedOperationFlags.EndsWith))
			{
				for (int i = 0; i < Math.Min(item.Length, this.Size); i++)
				{
					AddCharAtIndex(item[i], -(item.Length-i));
				}
			}
		}
    }

	public bool MightContain(string text)
	{
		if (!this.SupportedOperations.HasFlag(SupportedOperationFlags.Contains))
		{
			throw new InvalidOperationException("Contains operation is not supported by this filter.");
		}

		for (int i = 0; i < text.Length; i++)
		{
			if (!MightContainCharAtIndex(text[i], int.MaxValue))
			{
				return false;
			}
		}
		
		return true;
	}

	public bool MightStartWith(string prefix)
	{
		if (!this.SupportedOperations.HasFlag(SupportedOperationFlags.StartsWith))
		{
			throw new InvalidOperationException("StartsWith operation requires at least StartsWith operation.");
		}

		if (this.SupportedOperations.HasFlag(SupportedOperationFlags.Contains))
		{
			return this.MightContain(prefix);
		}

		for (int i = 0; i < prefix.Length; i++)
		{
			if (!MightContainCharAtIndex(prefix[i], i + 1))
			{
				return false;
			}
		}
		
		return true;
	}

	public bool MightEndWith(string suffix)
	{
		if (!this.SupportedOperations.HasFlag(SupportedOperationFlags.EndsWith))
		{
			throw new InvalidOperationException("EndsWith operation is not supported by this filter.");
		}

		if (this.SupportedOperations.HasFlag(SupportedOperationFlags.Contains))
		{
			return this.MightContain(suffix);
		}

		for (int i = suffix.Length - 1; i >= 0; i--)
		{
			if (!MightContainCharAtIndex(suffix[i], -(suffix.Length-i)))
			{
				return false;
			}
		}
		
		return true;
	}

	public bool MightEqual(string text)
	{
		if (this.MightStartWith(text))
		{
			if (this.SupportedOperations.HasFlag(SupportedOperationFlags.Equals) && !MightContainCharAtIndex(EndOfTextToken, text.Length + 1))
			{
				return false;
			}

			return true;
		}

		return false;
	}

    private void AddCharAtIndex(char c, int index)
	{
		var primaryHash = HashInt32((uint)c);
		var secondaryHash = HashInt32((uint)index << 16);
		for (int i = 1; i <= this.HashCount; i++)
		{
			int hash = ComputeHash(primaryHash, secondaryHash, i);
			this.bits[hash] = true;
		}
	}

	private bool MightContainCharAtIndex(char c, int index)
	{
		var primaryHash = HashInt32((uint)c);
		var secondaryHash = HashInt32((uint)index << 16);
		for (int i = 1; i <= this.HashCount; i++)
		{
			int hash = ComputeHash(primaryHash, secondaryHash, i);
			if (!bits[hash])
			{
				return false;
			}
		}
		
		return true;
	}

	/// <summary>
	/// Performs Dillinger and Manolios double hashing. 
	/// </summary>
	/// <param name="primaryHash"> The primary hash. </param>
	/// <param name="secondaryHash"> The secondary hash. </param>
	/// <param name="i"> The i. </param>
	/// <returns> The <see cref="int"/>. </returns>
	private int ComputeHash(int primaryHash, int secondaryHash, int i)
	{
		int resultingHash = (primaryHash + (i * secondaryHash)) % this.Size;
		return Math.Abs(resultingHash);
	}

	/// <summary>
	/// Hashes a 32-bit signed int using Thomas Wang's method v3.1 original link (http://www.concentric.net/~Ttwang/tech/inthash.htm).
	/// Runtime is suggested to be 11 cycles. 
	/// Analysis - https://burtleburtle.net/bob/hash/integer.html
	/// </summary>
	/// <param name="input">The integer to hash.</param>
	/// <returns>The hashed result.</returns>
	private static int HashInt32(uint x)
	{
		unchecked
		{
			x = ~x + (x << 15); // x = (x << 15) - x- 1, as (~x) + y is equivalent to y - x - 1 in two's complement representation
			x = x ^ (x >> 12);
			x = x + (x << 2);
			x = x ^ (x >> 4);
			x = x * 2057; // x = (x + (x << 3)) + (x<< 11);
			x = x ^ (x >> 16);
			return (int)x;
		}
	}
}
