```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 107.974 ns |  35.000 ns | 1.9185 ns | 16.85 |    0.29 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.407 ns |   1.116 ns | 0.0612 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 487.736 ns | 113.967 ns | 6.2469 ns | 76.13 |    1.05 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 474.382 ns |  48.446 ns | 2.6555 ns | 74.04 |    0.71 | 0.0601 |    1008 B |       42.00 |
