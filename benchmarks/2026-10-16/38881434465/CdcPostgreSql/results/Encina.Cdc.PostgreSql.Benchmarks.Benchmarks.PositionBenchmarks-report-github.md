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
| ComparePositions | 1.028 ns | 0.1202 ns | 0.0066 ns |  0.19 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.277 ns | 0.1772 ns | 0.0097 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.518 ns | 4.8429 ns | 0.2655 ns |  1.24 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.528 ns | 5.7525 ns | 0.3153 ns |  1.05 |    0.05 | 0.0019 |      32 B |        1.33 |
