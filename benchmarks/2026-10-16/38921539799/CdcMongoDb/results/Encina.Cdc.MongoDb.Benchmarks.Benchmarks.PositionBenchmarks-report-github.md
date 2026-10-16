```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 133.52 ns | 11.38 ns | 0.624 ns |  8.12 |    0.28 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  16.46 ns | 11.41 ns | 0.626 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 661.57 ns | 82.21 ns | 4.506 ns | 40.24 |    1.37 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 587.88 ns | 13.61 ns | 0.746 ns | 35.76 |    1.20 | 0.0601 |    1008 B |       42.00 |
