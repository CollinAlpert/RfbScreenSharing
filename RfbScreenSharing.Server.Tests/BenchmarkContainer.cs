using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace RfbScreenSharing.Server.Tests;

[MemoryDiagnoser]
public class BenchmarkContainer
{
	private static readonly byte[] Bytes = new byte[900_000];
	private static readonly Consumer Consumer = new();

	[GlobalSetup]
	public void Setup()
	{
		Random.Shared.NextBytes(Bytes);
	}

	[Benchmark(Baseline = true)]
	public void Benchmark()
	{
		foreach (var chunk in Bytes.Chunk(1_500))
		{
			Consumer.Consume(chunk[0]);
		}
	}

	[Benchmark]
	public void Benchmark2()
	{
		foreach (var chunk in SplitToChunks(Bytes, 1_500))
		{
			Consumer.Consume(chunk[0]);
		}
	}

	private static IEnumerable<byte[]> SplitToChunks(byte[] array, int chunkSize)
	{
		for (var i = 0; i < array.Length; i += chunkSize)
		{
			if (i + chunkSize >= array.Length)
			{
				yield return array[i..];

				yield break;
			}

			yield return array[i..(i + chunkSize)];
		}
	}
}