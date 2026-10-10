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
| ComparePositions |  1.319 ns | 0.0244 ns | 0.0013 ns |  0.12 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 11.071 ns | 5.7132 ns | 0.3132 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.938 ns | 1.5089 ns | 0.0827 ns |  0.63 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.602 ns | 4.6116 ns | 0.2528 ns |  0.69 |    0.03 | 0.0019 |      32 B |        1.33 |
