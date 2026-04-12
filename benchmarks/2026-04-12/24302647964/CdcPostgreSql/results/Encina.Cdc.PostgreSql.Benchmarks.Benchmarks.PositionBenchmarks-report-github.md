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
| ComparePositions |  1.306 ns | 0.0026 ns | 0.0038 ns |  0.12 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 10.931 ns | 0.7321 ns | 1.0958 ns |  1.01 |    0.14 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.171 ns | 0.1767 ns | 0.2645 ns |  0.57 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.917 ns | 0.0538 ns | 0.0806 ns |  0.64 |    0.06 | 0.0019 |      32 B |        1.33 |
