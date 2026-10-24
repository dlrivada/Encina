```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 131.662 ns |   6.631 ns | 0.3635 ns | 13.17 |    0.09 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   9.995 ns |   1.302 ns | 0.0714 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 632.259 ns | 159.985 ns | 8.7693 ns | 63.26 |    0.85 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 574.917 ns |  20.668 ns | 1.1329 ns | 57.52 |    0.37 | 0.0601 |    1008 B |       42.00 |
