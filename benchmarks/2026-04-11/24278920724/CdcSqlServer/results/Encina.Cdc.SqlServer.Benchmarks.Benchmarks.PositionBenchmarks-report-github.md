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
| ComparePositions | 1.330 ns | 0.0026 ns | 0.0037 ns |  0.21 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.411 ns | 0.2055 ns | 0.3075 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.225 ns | 0.1477 ns | 0.2118 ns |  1.13 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.450 ns | 0.3385 ns | 0.4854 ns |  1.16 |    0.09 | 0.0019 |      32 B |        1.33 |
