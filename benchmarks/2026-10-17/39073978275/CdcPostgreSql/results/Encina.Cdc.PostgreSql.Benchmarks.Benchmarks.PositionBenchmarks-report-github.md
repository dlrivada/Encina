```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.05GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.313 ns |  0.0685 ns | 0.0038 ns |  0.16 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 8.425 ns | 12.1227 ns | 0.6645 ns |  1.00 |    0.10 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.864 ns |  0.6696 ns | 0.0367 ns |  0.82 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.831 ns |  0.3141 ns | 0.0172 ns |  0.81 |    0.06 | 0.0019 |      32 B |        1.33 |
