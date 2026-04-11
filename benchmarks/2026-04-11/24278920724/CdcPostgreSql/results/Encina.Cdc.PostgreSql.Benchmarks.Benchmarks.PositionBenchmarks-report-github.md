```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.304 ns | 0.0022 ns | 0.0030 ns |  0.12 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 11.199 ns | 0.2583 ns | 0.3704 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.659 ns | 0.1285 ns | 0.1923 ns |  0.60 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.005 ns | 0.0825 ns | 0.1209 ns |  0.63 |    0.02 | 0.0019 |      32 B |        1.33 |
