```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.12GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.297 ns | 0.0036 ns | 0.0052 ns |  0.21 |    0.02 |      - |         - |        0.00 |
| CreatePosition   | 6.327 ns | 0.3198 ns | 0.4786 ns |  1.01 |    0.11 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.973 ns | 0.0822 ns | 0.1204 ns |  1.11 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.381 ns | 0.2830 ns | 0.4148 ns |  1.17 |    0.11 | 0.0019 |      32 B |        1.33 |
