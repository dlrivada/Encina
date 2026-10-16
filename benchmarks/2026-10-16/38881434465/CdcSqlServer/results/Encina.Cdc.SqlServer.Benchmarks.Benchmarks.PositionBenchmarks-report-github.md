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
| ComparePositions | 1.282 ns | 0.0474 ns | 0.0026 ns |  0.19 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.919 ns | 0.9698 ns | 0.0532 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.687 ns | 1.2138 ns | 0.0665 ns |  1.11 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.711 ns | 3.6748 ns | 0.2014 ns |  0.97 |    0.03 | 0.0019 |      32 B |        1.33 |
