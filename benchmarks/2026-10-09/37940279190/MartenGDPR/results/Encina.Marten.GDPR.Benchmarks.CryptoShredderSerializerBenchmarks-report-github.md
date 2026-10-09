```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    449.4 ns |    24.96 ns |   1.37 ns |  1.00 |    0.00 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    714.3 ns |   190.84 ns |  10.46 ns |  1.59 |    0.02 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 22,776.6 ns | 1,608.07 ns |  88.14 ns | 50.69 |    0.22 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 35,245.9 ns | 3,408.63 ns | 186.84 ns | 78.44 |    0.42 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,955.8 ns |   526.43 ns |  28.86 ns |  8.80 |    0.06 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  5,361.6 ns |    86.34 ns |   4.73 ns | 11.93 |    0.03 |    4 | 0.1297 |    2288 B |        8.94 |
