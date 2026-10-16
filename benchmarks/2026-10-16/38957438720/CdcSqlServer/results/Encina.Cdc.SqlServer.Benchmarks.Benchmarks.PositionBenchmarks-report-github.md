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
| ComparePositions | 0.9810 ns | 0.1629 ns | 0.0089 ns |  0.18 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.4082 ns | 1.0951 ns | 0.0600 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.4284 ns | 3.6630 ns | 0.2008 ns |  1.37 |    0.03 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 5.2192 ns | 0.8677 ns | 0.0476 ns |  0.97 |    0.01 | 0.0004 |      32 B |        1.33 |
