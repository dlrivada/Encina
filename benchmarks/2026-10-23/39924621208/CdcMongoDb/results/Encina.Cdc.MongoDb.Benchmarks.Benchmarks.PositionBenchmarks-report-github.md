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
| ComparePositions | 135.18 ns |  6.179 ns | 0.339 ns | 12.67 |    1.33 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  10.78 ns | 24.663 ns | 1.352 ns |  1.01 |    0.15 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 650.32 ns | 56.582 ns | 3.101 ns | 60.96 |    6.38 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 616.70 ns | 30.083 ns | 1.649 ns | 57.81 |    6.05 | 0.0601 |    1008 B |       42.00 |
