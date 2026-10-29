```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 135.04 ns |  10.157 ns | 0.557 ns | 11.86 |    0.07 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  11.39 ns |   1.015 ns | 0.056 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 624.25 ns | 109.502 ns | 6.002 ns | 54.81 |    0.51 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 608.99 ns | 118.501 ns | 6.495 ns | 53.47 |    0.54 | 0.0601 |    1008 B |       42.00 |
