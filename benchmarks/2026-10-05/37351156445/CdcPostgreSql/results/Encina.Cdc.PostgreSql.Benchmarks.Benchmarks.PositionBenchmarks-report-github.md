```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.326 ns |  0.2480 ns | 0.0136 ns |  0.13 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 10.084 ns | 10.9113 ns | 0.5981 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  7.201 ns |  0.6487 ns | 0.0356 ns |  0.72 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.832 ns |  1.0088 ns | 0.0553 ns |  0.78 |    0.04 | 0.0019 |      32 B |        1.33 |
