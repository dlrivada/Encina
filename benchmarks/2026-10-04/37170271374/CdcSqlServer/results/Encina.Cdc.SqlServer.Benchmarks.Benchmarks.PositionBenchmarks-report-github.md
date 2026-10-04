```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.060 ns | 0.0018 ns | 0.0026 ns |  0.21 |    0.02 |      - |         - |        0.00 |
| CreatePosition   | 5.052 ns | 0.3742 ns | 0.5600 ns |  1.01 |    0.16 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.520 ns | 0.2019 ns | 0.3022 ns |  1.11 |    0.14 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.680 ns | 0.2424 ns | 0.3627 ns |  1.14 |    0.15 | 0.0019 |      32 B |        1.33 |
