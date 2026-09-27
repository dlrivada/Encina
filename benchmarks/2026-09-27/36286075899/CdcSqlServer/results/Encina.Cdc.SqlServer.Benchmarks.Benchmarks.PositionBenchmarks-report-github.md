```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.366 ns | 0.0251 ns | 0.0369 ns | 1.394 ns |  0.21 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.482 ns | 0.1656 ns | 0.2427 ns | 6.467 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.803 ns | 0.4019 ns | 0.6015 ns | 7.847 ns |  1.21 |    0.10 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.527 ns | 0.0968 ns | 0.1419 ns | 6.584 ns |  1.01 |    0.04 | 0.0019 |      32 B |        1.33 |
