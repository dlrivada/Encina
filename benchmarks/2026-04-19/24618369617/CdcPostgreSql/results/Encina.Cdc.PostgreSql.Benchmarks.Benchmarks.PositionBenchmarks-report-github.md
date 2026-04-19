```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.305 ns | 0.0042 ns | 0.0061 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.281 ns | 0.0434 ns | 0.0649 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.876 ns | 0.0267 ns | 0.0391 ns |  1.09 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.857 ns | 0.0462 ns | 0.0663 ns |  1.09 |    0.02 | 0.0019 |      32 B |        1.33 |
