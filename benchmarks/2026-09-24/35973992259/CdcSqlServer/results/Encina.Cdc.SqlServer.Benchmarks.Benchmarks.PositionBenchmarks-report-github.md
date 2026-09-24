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
| ComparePositions | 1.276 ns | 0.0791 ns | 0.0043 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 8.489 ns | 5.1176 ns | 0.2805 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.571 ns | 0.2612 ns | 0.0143 ns |  0.77 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.946 ns | 0.7111 ns | 0.0390 ns |  0.70 |    0.02 | 0.0019 |      32 B |        1.33 |
