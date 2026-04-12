```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.289 ns | 0.0039 ns | 0.0055 ns |  0.15 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 8.425 ns | 0.2456 ns | 0.3677 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.862 ns | 0.1218 ns | 0.1823 ns |  0.82 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.227 ns | 0.1122 ns | 0.1644 ns |  0.74 |    0.04 | 0.0019 |      32 B |        1.33 |
