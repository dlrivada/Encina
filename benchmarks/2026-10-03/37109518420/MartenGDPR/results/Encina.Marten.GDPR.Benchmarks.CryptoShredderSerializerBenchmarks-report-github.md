```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   345.3 ns | 376.15 ns | 20.62 ns |  1.00 |    0.07 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   355.3 ns |  77.70 ns |  4.26 ns |  1.03 |    0.05 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 4,260.4 ns | 215.11 ns | 11.79 ns | 12.37 |    0.62 |    2 | 0.1678 |    2869 B |       10.55 |
