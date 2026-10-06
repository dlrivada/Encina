```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.316 ns | 0.0600 ns | 0.0033 ns |  0.13 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 10.414 ns | 9.9341 ns | 0.5445 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  7.142 ns | 1.2214 ns | 0.0670 ns |  0.69 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.685 ns | 2.6968 ns | 0.1478 ns |  0.74 |    0.04 | 0.0019 |      32 B |        1.33 |
