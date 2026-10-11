```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.008 ns | 0.0009 ns | 0.0014 ns |  0.14 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 7.150 ns | 0.0660 ns | 0.0988 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.705 ns | 0.3006 ns | 0.4499 ns |  1.08 |    0.06 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 8.853 ns | 0.3607 ns | 0.5173 ns |  1.24 |    0.07 | 0.0004 |      32 B |        1.33 |
