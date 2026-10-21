```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.7229 ns | 0.3473 ns | 0.0190 ns |  0.08 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 8.5234 ns | 2.9132 ns | 0.1597 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 8.4712 ns | 2.8438 ns | 0.1559 ns |  0.99 |    0.02 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.9606 ns | 3.1489 ns | 0.1726 ns |  0.93 |    0.02 | 0.0004 |      32 B |        1.33 |
