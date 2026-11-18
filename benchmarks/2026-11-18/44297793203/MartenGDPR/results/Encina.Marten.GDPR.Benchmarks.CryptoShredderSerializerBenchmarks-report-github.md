```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    605.6 ns |    37.06 ns |   2.03 ns |  1.00 |    0.00 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    961.3 ns |    88.58 ns |   4.86 ns |  1.59 |    0.01 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 29,547.5 ns |   810.12 ns |  44.41 ns | 48.79 |    0.16 |    4 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 40,388.9 ns | 3,819.64 ns | 209.37 ns | 66.70 |    0.36 |    5 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,908.3 ns |   668.48 ns |  36.64 ns |  9.76 |    0.06 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  6,634.2 ns | 2,766.72 ns | 151.65 ns | 10.96 |    0.22 |    3 | 0.1297 |    2288 B |        8.94 |
