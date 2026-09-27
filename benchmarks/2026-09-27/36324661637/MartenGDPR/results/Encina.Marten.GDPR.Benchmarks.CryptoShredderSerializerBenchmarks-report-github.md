```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   622.2 ns |  44.77 ns |  2.45 ns |  1.00 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   683.3 ns |  13.88 ns |  0.76 ns |  1.10 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 8,916.4 ns | 608.57 ns | 33.36 ns | 14.33 |    0.07 |    2 | 0.1678 |    2869 B |       10.55 |
