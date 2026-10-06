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
| ComparePositions |  1.315 ns | 0.0659 ns | 0.0036 ns |  0.12 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 10.609 ns | 8.2479 ns | 0.4521 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.894 ns | 1.5550 ns | 0.0852 ns |  0.65 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.427 ns | 0.3621 ns | 0.0198 ns |  0.70 |    0.03 | 0.0019 |      32 B |        1.33 |
