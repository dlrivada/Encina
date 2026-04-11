```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.342 ns | 0.0031 ns | 0.0040 ns |  0.22 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.138 ns | 0.1576 ns | 0.2359 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.401 ns | 0.1528 ns | 0.2287 ns |  1.21 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.829 ns | 0.1398 ns | 0.2093 ns |  1.28 |    0.06 | 0.0019 |      32 B |        1.33 |
