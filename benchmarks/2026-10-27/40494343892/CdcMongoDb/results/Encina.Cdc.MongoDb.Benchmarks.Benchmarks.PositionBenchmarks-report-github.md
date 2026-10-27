```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.80 ns |  9.972 ns | 0.547 ns | 12.45 |    1.09 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  10.90 ns | 18.913 ns | 1.037 ns |  1.01 |    0.12 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 632.47 ns | 51.574 ns | 2.827 ns | 58.42 |    5.10 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 570.78 ns | 64.362 ns | 3.528 ns | 52.72 |    4.60 | 0.0601 |    1008 B |       42.00 |
