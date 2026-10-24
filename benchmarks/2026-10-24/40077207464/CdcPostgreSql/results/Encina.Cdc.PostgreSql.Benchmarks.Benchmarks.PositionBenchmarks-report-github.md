```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4787 ns | 0.1880 ns | 0.0103 ns |  0.13 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.5757 ns | 2.9868 ns | 0.1637 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.6960 ns | 0.7147 ns | 0.0392 ns |  1.04 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.6699 ns | 4.1885 ns | 0.2296 ns |  1.03 |    0.07 | 0.0019 |      32 B |        1.33 |
