```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.061 ns | 0.0018 ns | 0.0024 ns |  0.22 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.870 ns | 0.1998 ns | 0.2991 ns |  1.00 |    0.09 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.174 ns | 0.1058 ns | 0.1517 ns |  1.27 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.774 ns | 0.1221 ns | 0.1827 ns |  1.19 |    0.08 | 0.0019 |      32 B |        1.33 |
