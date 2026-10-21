```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.9106 ns | 0.2661 ns | 0.0146 ns |  0.17 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.2634 ns | 2.8880 ns | 0.1583 ns |  1.00 |    0.04 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 7.1367 ns | 2.6427 ns | 0.1449 ns |  1.36 |    0.04 | 0.0010 |      24 B |        1.00 |
| ToBytes          | 6.7947 ns | 1.7948 ns | 0.0984 ns |  1.29 |    0.04 | 0.0013 |      32 B |        1.33 |
