```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.78GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.9451 ns | 0.1391 ns | 0.0076 ns |  0.14 |      - |         - |        0.00 |
| CreatePosition   | 6.7355 ns | 0.4939 ns | 0.0271 ns |  1.00 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 6.6601 ns | 1.1783 ns | 0.0646 ns |  0.99 | 0.0010 |      24 B |        1.00 |
| ToBytes          | 8.1484 ns | 0.3648 ns | 0.0200 ns |  1.21 | 0.0013 |      32 B |        1.33 |
