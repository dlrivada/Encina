```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.309 ns | 0.0582 ns | 0.0032 ns |  0.12 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 11.086 ns | 9.7172 ns | 0.5326 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.332 ns | 0.6259 ns | 0.0343 ns |  0.57 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.988 ns | 1.4549 ns | 0.0797 ns |  0.72 |    0.03 | 0.0019 |      32 B |        1.33 |
