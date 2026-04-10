```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.279 ns | 0.0669 ns | 0.0037 ns |  0.16 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 8.053 ns | 3.6704 ns | 0.2012 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.439 ns | 1.0166 ns | 0.0557 ns |  0.92 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.991 ns | 0.2785 ns | 0.0153 ns |  0.87 |    0.02 | 0.0019 |      32 B |        1.33 |
