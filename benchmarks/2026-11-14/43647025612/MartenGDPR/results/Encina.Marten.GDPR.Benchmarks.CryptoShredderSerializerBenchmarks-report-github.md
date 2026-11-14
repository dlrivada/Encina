```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    368.8 ns |    48.90 ns |   2.68 ns |  1.00 |    0.01 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    574.7 ns |    82.96 ns |   4.55 ns |  1.56 |    0.01 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 18,686.2 ns | 1,986.41 ns | 108.88 ns | 50.67 |    0.41 |    4 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 21,090.2 ns |   413.92 ns |  22.69 ns | 57.19 |    0.36 |    4 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,562.4 ns |   112.56 ns |   6.17 ns |  9.66 |    0.06 |    3 | 0.0420 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,673.2 ns |    35.92 ns |   1.97 ns |  9.96 |    0.06 |    3 | 0.0267 |    2288 B |        8.94 |
