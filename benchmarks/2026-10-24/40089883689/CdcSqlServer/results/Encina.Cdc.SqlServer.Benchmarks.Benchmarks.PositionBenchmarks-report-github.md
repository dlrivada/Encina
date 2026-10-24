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
| ComparePositions | 1.187 ns | 0.0366 ns | 0.0020 ns |  0.24 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.996 ns | 2.2576 ns | 0.1237 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.275 ns | 1.6543 ns | 0.0907 ns |  1.26 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.528 ns | 0.9246 ns | 0.0507 ns |  1.11 |    0.03 | 0.0019 |      32 B |        1.33 |
