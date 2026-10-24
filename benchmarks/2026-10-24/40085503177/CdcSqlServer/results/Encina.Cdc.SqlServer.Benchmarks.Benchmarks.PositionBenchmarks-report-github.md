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
| ComparePositions | 1.277 ns | 0.0587 ns | 0.0032 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.407 ns | 0.6688 ns | 0.0367 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.274 ns | 0.6835 ns | 0.0375 ns |  1.14 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.256 ns | 2.8325 ns | 0.1553 ns |  0.98 |    0.02 | 0.0019 |      32 B |        1.33 |
