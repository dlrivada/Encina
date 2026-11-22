```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                            | Mean        | Error     | StdDev      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|----------:|------------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    622.3 ns |   5.89 ns |     8.45 ns |  1.00 |    0.02 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    984.9 ns |   6.65 ns |     9.95 ns |  1.58 |    0.03 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 30,618.2 ns | 775.59 ns | 1,061.64 ns | 49.21 |    1.80 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 40,011.3 ns | 874.62 ns | 1,282.01 ns | 64.30 |    2.20 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,430.1 ns |  16.58 ns |    24.30 ns |  8.73 |    0.12 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  6,484.1 ns |  19.95 ns |    25.94 ns | 10.42 |    0.15 |    4 | 0.1297 |    2288 B |        8.94 |
