```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.277 ns | 0.0456 ns | 0.0025 ns |  0.13 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 9.540 ns | 8.9511 ns | 0.4906 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 8.985 ns | 1.8259 ns | 0.1001 ns |  0.94 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.228 ns | 0.6480 ns | 0.0355 ns |  0.65 |    0.03 | 0.0019 |      32 B |        1.33 |
