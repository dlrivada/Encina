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
| ComparePositions | 1.309 ns | 0.0876 ns | 0.0048 ns |  0.14 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 9.432 ns | 3.6025 ns | 0.1975 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.216 ns | 1.5171 ns | 0.0832 ns |  0.66 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.975 ns | 0.6510 ns | 0.0357 ns |  0.74 |    0.01 | 0.0019 |      32 B |        1.33 |
