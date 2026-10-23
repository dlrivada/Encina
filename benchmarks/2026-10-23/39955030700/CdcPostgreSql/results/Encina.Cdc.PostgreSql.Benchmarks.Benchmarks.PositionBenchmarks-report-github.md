```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4666 ns | 0.6481 ns | 0.0355 ns |  0.13 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.5867 ns | 1.5330 ns | 0.0840 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.9201 ns | 3.3047 ns | 0.1811 ns |  1.09 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 4.0149 ns | 2.3549 ns | 0.1291 ns |  1.12 |    0.04 | 0.0019 |      32 B |        1.33 |
