```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.058 ns | 0.0251 ns | 0.0014 ns |  0.23 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 4.664 ns | 0.2563 ns | 0.0140 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.887 ns | 2.8275 ns | 0.1550 ns |  1.26 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.321 ns | 1.2689 ns | 0.0696 ns |  1.14 |    0.01 | 0.0019 |      32 B |        1.33 |
