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
| ComparePositions | 1.285 ns | 0.0334 ns | 0.0018 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.404 ns | 1.2377 ns | 0.0678 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.860 ns | 1.3227 ns | 0.0725 ns |  1.07 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.398 ns | 2.2855 ns | 0.1253 ns |  1.00 |    0.02 | 0.0019 |      32 B |        1.33 |
