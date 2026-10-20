```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.327 ns | 0.0446 ns | 0.0024 ns |  0.19 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.820 ns | 3.3130 ns | 0.1816 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.635 ns | 9.6647 ns | 0.5298 ns |  1.12 |    0.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.408 ns | 4.4010 ns | 0.2412 ns |  0.94 |    0.04 | 0.0019 |      32 B |        1.33 |
