```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.273 ns | 0.0670 ns | 0.0037 ns |  0.14 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 9.266 ns | 1.1300 ns | 0.0619 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.604 ns | 4.7272 ns | 0.2591 ns |  0.82 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.478 ns | 5.4467 ns | 0.2986 ns |  0.70 |    0.03 | 0.0019 |      32 B |        1.33 |
