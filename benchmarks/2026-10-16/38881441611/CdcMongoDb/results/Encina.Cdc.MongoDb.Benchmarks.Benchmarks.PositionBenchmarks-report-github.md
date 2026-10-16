```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 136.37 ns |  30.11 ns | 1.650 ns |  8.30 |    0.45 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  16.46 ns |  18.86 ns | 1.034 ns |  1.00 |    0.08 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 658.04 ns | 118.87 ns | 6.516 ns | 40.07 |    2.17 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 590.36 ns |  83.18 ns | 4.559 ns | 35.95 |    1.94 | 0.0601 |    1008 B |       42.00 |
