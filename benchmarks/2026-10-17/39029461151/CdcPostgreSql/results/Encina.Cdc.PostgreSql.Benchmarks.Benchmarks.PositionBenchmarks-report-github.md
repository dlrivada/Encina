```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.025 ns | 0.0548 ns | 0.0030 ns |  0.23 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.524 ns | 2.7419 ns | 0.1503 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.050 ns | 3.4523 ns | 0.1892 ns |  1.34 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.497 ns | 2.9443 ns | 0.1614 ns |  1.22 |    0.05 | 0.0019 |      32 B |        1.33 |
