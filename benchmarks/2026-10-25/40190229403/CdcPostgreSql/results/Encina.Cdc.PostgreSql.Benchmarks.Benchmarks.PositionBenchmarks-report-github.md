```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.370 ns | 0.9309 ns | 0.0510 ns |  0.22 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.323 ns | 1.0665 ns | 0.0585 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.477 ns | 3.4128 ns | 0.1871 ns |  1.18 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.994 ns | 5.0497 ns | 0.2768 ns |  1.26 |    0.04 | 0.0019 |      32 B |        1.33 |
