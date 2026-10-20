```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4455 ns | 0.2043 ns | 0.0112 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.4412 ns | 1.3959 ns | 0.0765 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.8581 ns | 4.0298 ns | 0.2209 ns |  1.12 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.6758 ns | 1.5177 ns | 0.0832 ns |  1.07 |    0.03 | 0.0019 |      32 B |        1.33 |
