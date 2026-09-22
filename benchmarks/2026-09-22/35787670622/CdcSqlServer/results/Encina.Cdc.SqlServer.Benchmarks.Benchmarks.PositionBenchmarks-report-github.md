```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.012 ns | 0.0514 ns | 0.0028 ns |  0.16 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.428 ns | 0.6853 ns | 0.0376 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 8.698 ns | 5.6437 ns | 0.3093 ns |  1.35 |    0.04 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 8.114 ns | 2.6954 ns | 0.1477 ns |  1.26 |    0.02 | 0.0004 |      32 B |        1.33 |
