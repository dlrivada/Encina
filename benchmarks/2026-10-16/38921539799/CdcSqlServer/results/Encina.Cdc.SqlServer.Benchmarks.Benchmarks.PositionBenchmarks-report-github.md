```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.76GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.286 ns | 0.0461 ns | 0.0025 ns |  0.16 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 8.188 ns | 4.9348 ns | 0.2705 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.627 ns | 0.5525 ns | 0.0303 ns |  0.93 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.014 ns | 3.1035 ns | 0.1701 ns |  0.86 |    0.03 | 0.0019 |      32 B |        1.33 |
