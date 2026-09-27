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
| ComparePositions | 1.592 ns | 0.0163 ns | 0.0009 ns |  0.18 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 9.063 ns | 3.0441 ns | 0.1669 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.975 ns | 1.6304 ns | 0.0894 ns |  0.77 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.696 ns | 1.4523 ns | 0.0796 ns |  0.74 |    0.01 | 0.0019 |      32 B |        1.33 |
