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
| ComparePositions | 0.4658 ns | 0.8034 ns | 0.0440 ns |  0.13 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.6951 ns | 0.4658 ns | 0.0255 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.6593 ns | 0.9135 ns | 0.0501 ns |  0.99 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.5030 ns | 4.4228 ns | 0.2424 ns |  0.95 |    0.06 | 0.0019 |      32 B |        1.33 |
