```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    633.2 ns |   201.1 ns |  11.02 ns |  1.00 |    0.02 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    997.6 ns |   143.3 ns |   7.86 ns |  1.58 |    0.03 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 34,672.1 ns | 3,610.4 ns | 197.90 ns | 54.76 |    0.86 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 45,679.0 ns | 3,358.0 ns | 184.07 ns | 72.15 |    1.11 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,346.9 ns |   367.7 ns |  20.15 ns |  8.45 |    0.13 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  7,668.4 ns |   419.0 ns |  22.97 ns | 12.11 |    0.18 |    4 | 0.1221 |    2288 B |        8.94 |
