```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4920 ns | 0.1676 ns | 0.0092 ns |  0.14 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.5288 ns | 0.5982 ns | 0.0328 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.8600 ns | 3.4491 ns | 0.1891 ns |  1.09 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.5624 ns | 1.0013 ns | 0.0549 ns |  1.01 |    0.02 | 0.0019 |      32 B |        1.33 |
