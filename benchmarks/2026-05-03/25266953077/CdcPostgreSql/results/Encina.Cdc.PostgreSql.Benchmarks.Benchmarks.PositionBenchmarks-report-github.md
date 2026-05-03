```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.340 ns | 0.0018 ns | 0.0023 ns | 1.340 ns |  0.19 |    0.02 |      - |         - |        0.00 |
| CreatePosition   | 7.055 ns | 0.4972 ns | 0.7441 ns | 6.639 ns |  1.01 |    0.15 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.642 ns | 0.2910 ns | 0.4356 ns | 7.427 ns |  1.09 |    0.13 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.842 ns | 0.3853 ns | 0.5402 ns | 7.881 ns |  1.12 |    0.14 | 0.0019 |      32 B |        1.33 |
