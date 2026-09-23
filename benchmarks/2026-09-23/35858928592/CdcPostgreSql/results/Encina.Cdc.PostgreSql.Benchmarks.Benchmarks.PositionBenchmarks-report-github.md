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
| ComparePositions |  1.317 ns | 0.0630 ns | 0.0035 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 10.222 ns | 6.4295 ns | 0.3524 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.712 ns | 2.8565 ns | 0.1566 ns |  0.66 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.756 ns | 3.8722 ns | 0.2122 ns |  0.76 |    0.03 | 0.0019 |      32 B |        1.33 |
