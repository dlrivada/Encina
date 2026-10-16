```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 136.12 ns |   7.803 ns | 0.428 ns | 12.60 |    0.44 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  10.81 ns |   7.789 ns | 0.427 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 672.16 ns |  89.994 ns | 4.933 ns | 62.24 |    2.18 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 632.49 ns | 130.005 ns | 7.126 ns | 58.57 |    2.09 | 0.0601 |    1008 B |       42.00 |
