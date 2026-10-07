```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 1.001 ns | 0.0176 ns | 0.0010 ns |  0.17 |      - |         - |        0.00 |
| CreatePosition   | 6.006 ns | 0.7244 ns | 0.0397 ns |  1.00 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 8.032 ns | 0.3488 ns | 0.0191 ns |  1.34 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.648 ns | 0.8439 ns | 0.0463 ns |  1.27 | 0.0004 |      32 B |        1.33 |
