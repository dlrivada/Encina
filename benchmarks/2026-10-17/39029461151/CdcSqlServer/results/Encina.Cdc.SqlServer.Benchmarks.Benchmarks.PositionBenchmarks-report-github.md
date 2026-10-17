```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 1.277 ns | 0.1547 ns | 0.0085 ns |  0.16 |      - |         - |        0.00 |
| CreatePosition   | 8.003 ns | 0.4855 ns | 0.0266 ns |  1.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.094 ns | 1.2562 ns | 0.0689 ns |  0.89 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.286 ns | 1.6415 ns | 0.0900 ns |  0.79 | 0.0019 |      32 B |        1.33 |
