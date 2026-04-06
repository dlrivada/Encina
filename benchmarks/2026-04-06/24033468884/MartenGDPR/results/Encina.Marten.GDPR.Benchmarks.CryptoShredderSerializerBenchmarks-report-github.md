```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                    | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   600.6 ns |   3.83 ns |   5.37 ns |   600.5 ns |  1.00 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   664.3 ns |   8.72 ns |  12.78 ns |   672.2 ns |  1.11 |    0.02 |    2 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 8,766.0 ns | 240.38 ns | 352.34 ns | 8,477.4 ns | 14.60 |    0.59 |    3 | 0.1678 |    2869 B |       10.55 |
