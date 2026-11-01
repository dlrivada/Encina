```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.231 ns | 0.0930 ns | 0.1392 ns |  0.15 |    0.02 |      - |         - |        0.00 |
| CreatePosition   | 8.247 ns | 0.3664 ns | 0.5484 ns |  1.00 |    0.09 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.859 ns | 0.4278 ns | 0.6136 ns |  0.96 |    0.10 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.130 ns | 0.4040 ns | 0.5921 ns |  0.87 |    0.09 | 0.0019 |      32 B |        1.33 |
