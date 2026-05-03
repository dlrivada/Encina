```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.326 ns | 0.0060 ns | 0.0086 ns |  0.20 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.595 ns | 0.1430 ns | 0.2140 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.875 ns | 0.1600 ns | 0.2395 ns |  1.20 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.281 ns | 0.1109 ns | 0.1659 ns |  1.11 |    0.04 | 0.0019 |      32 B |        1.33 |
