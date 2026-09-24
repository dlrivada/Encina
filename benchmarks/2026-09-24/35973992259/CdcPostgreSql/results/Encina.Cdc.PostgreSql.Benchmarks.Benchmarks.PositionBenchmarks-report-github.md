```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4322 ns | 0.0476 ns | 0.0026 ns |  0.14 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.2041 ns | 1.9856 ns | 0.1088 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.3441 ns | 1.3484 ns | 0.0739 ns |  1.04 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.4737 ns | 0.9575 ns | 0.0525 ns |  1.08 |    0.03 | 0.0019 |      32 B |        1.33 |
