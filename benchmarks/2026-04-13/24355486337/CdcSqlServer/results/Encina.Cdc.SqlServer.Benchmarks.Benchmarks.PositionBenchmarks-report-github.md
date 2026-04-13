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
| ComparePositions |  1.286 ns | 0.0028 ns | 0.0039 ns |  0.14 |    0.02 |      - |         - |        0.00 |
| CreatePosition   |  9.058 ns | 0.8719 ns | 1.2504 ns |  1.02 |    0.20 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  7.333 ns | 0.1181 ns | 0.1731 ns |  0.82 |    0.11 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 12.104 ns | 3.5729 ns | 5.3477 ns |  1.36 |    0.62 | 0.0019 |      32 B |        1.33 |
