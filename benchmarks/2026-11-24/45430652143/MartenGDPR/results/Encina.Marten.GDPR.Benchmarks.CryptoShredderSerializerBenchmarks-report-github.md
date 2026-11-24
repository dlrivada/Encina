```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|-------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    433.6 ns |     20.94 ns |   1.15 ns |  1.00 |    0.00 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    674.1 ns |    267.07 ns |  14.64 ns |  1.55 |    0.03 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 23,465.8 ns | 13,283.62 ns | 728.12 ns | 54.12 |    1.46 |    4 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 26,987.7 ns |  2,654.47 ns | 145.50 ns | 62.24 |    0.32 |    4 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  4,096.4 ns |    168.80 ns |   9.25 ns |  9.45 |    0.03 |    3 | 0.0381 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  4,443.1 ns |    230.74 ns |  12.65 ns | 10.25 |    0.03 |    3 | 0.0229 |    2288 B |        8.94 |
