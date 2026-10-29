```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.63GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.8702 ns | 0.0320 ns | 0.0018 ns |  0.15 |      - |         - |        0.00 |
| CreatePosition   | 5.7563 ns | 0.7554 ns | 0.0414 ns |  1.00 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 6.7558 ns | 0.6381 ns | 0.0350 ns |  1.17 | 0.0010 |      24 B |        1.00 |
| ToBytes          | 6.7250 ns | 0.8027 ns | 0.0440 ns |  1.17 | 0.0013 |      32 B |        1.33 |
