```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                    | Mean       | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|---------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   618.6 ns |  5.43 ns |   7.96 ns |  1.00 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   653.4 ns |  6.69 ns |   9.38 ns |  1.06 |    0.02 |    2 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 9,010.0 ns | 98.61 ns | 141.43 ns | 14.57 |    0.29 |    3 | 0.1678 |    2869 B |       10.55 |
