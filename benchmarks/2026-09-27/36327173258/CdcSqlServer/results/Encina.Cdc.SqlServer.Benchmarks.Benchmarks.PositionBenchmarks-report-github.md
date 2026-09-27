```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.057 ns | 0.0549 ns | 0.0030 ns |  0.20 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.211 ns | 3.0837 ns | 0.1690 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.656 ns | 1.6643 ns | 0.0912 ns |  1.09 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.706 ns | 2.2991 ns | 0.1260 ns |  1.10 |    0.04 | 0.0019 |      32 B |        1.33 |
