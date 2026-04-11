```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   641.1 ns |   3.43 ns |   4.81 ns |  1.00 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   663.2 ns |   5.17 ns |   7.57 ns |  1.03 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 8,706.9 ns | 249.42 ns | 341.40 ns | 13.58 |    0.53 |    3 | 0.1678 |    2869 B |       10.55 |
