```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.280 ns | 0.0844 ns | 0.0046 ns |  0.19 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.895 ns | 1.6579 ns | 0.0909 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.318 ns | 1.5215 ns | 0.0834 ns |  1.06 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.841 ns | 1.2326 ns | 0.0676 ns |  0.99 |    0.01 | 0.0019 |      32 B |        1.33 |
