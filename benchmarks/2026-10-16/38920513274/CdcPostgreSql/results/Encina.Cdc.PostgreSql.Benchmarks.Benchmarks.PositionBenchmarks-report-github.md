```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.6129 ns | 0.2846 ns | 0.0156 ns |  0.08 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 7.9816 ns | 4.8789 ns | 0.2674 ns |  1.00 |    0.04 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 8.2553 ns | 0.5817 ns | 0.0319 ns |  1.04 |    0.03 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.3829 ns | 4.0682 ns | 0.2230 ns |  0.93 |    0.04 | 0.0004 |      32 B |        1.33 |
