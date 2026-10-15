```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.317 ns | 0.1410 ns | 0.0077 ns |  0.14 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 9.100 ns | 1.0203 ns | 0.0559 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.691 ns | 0.9221 ns | 0.0505 ns |  0.85 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.613 ns | 3.3667 ns | 0.1845 ns |  0.84 |    0.02 | 0.0019 |      32 B |        1.33 |
