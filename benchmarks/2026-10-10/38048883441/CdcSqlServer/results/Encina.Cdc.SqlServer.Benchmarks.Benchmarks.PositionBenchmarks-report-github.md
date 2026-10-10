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
| ComparePositions | 1.283 ns | 0.1108 ns | 0.0061 ns |  0.18 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 7.015 ns | 3.2384 ns | 0.1775 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.254 ns | 1.4503 ns | 0.0795 ns |  0.89 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.214 ns | 1.4647 ns | 0.0803 ns |  0.89 |    0.02 | 0.0019 |      32 B |        1.33 |
