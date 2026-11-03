```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    459.3 ns |    19.88 ns |  1.09 ns |  1.00 |    0.00 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    707.3 ns |    80.28 ns |  4.40 ns |  1.54 |    0.01 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 21,090.1 ns |   728.39 ns | 39.93 ns | 45.92 |    0.12 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 30,126.2 ns | 1,707.59 ns | 93.60 ns | 65.59 |    0.22 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,780.3 ns |   563.81 ns | 30.90 ns |  8.23 |    0.06 |    3 | 0.2174 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  4,754.6 ns |   654.35 ns | 35.87 ns | 10.35 |    0.07 |    4 | 0.1297 |    2288 B |        8.94 |
