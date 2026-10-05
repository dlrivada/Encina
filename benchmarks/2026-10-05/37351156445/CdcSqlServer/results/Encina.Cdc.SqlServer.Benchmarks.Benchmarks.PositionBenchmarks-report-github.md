```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.011 ns | 0.0093 ns | 0.0005 ns |  0.17 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.075 ns | 0.8210 ns | 0.0450 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.102 ns | 0.5179 ns | 0.0284 ns |  1.17 |    0.01 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.974 ns | 4.2477 ns | 0.2328 ns |  1.31 |    0.03 | 0.0004 |      32 B |        1.33 |
