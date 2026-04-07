```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                    | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   603.5 ns |  5.54 ns |  7.76 ns |  1.00 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   689.9 ns | 28.82 ns | 43.14 ns |  1.14 |    0.07 |    2 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 8,561.6 ns | 61.09 ns | 91.43 ns | 14.19 |    0.23 |    3 | 0.1678 |    2869 B |       10.55 |
