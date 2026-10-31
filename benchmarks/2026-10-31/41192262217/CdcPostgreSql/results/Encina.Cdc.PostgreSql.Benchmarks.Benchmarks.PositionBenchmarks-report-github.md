```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.025 ns | 0.0130 ns | 0.0007 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.194 ns | 0.9210 ns | 0.0505 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.876 ns | 0.2113 ns | 0.0116 ns |  1.32 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.626 ns | 3.1014 ns | 0.1700 ns |  1.08 |    0.03 | 0.0019 |      32 B |        1.33 |
