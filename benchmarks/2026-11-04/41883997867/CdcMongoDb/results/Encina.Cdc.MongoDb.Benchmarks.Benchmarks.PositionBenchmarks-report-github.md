```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 135.18 ns |   1.732 ns | 0.095 ns | 13.38 |    0.63 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  10.12 ns |  10.205 ns | 0.559 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 660.82 ns | 103.596 ns | 5.678 ns | 65.40 |    3.11 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 632.35 ns |   2.268 ns | 0.124 ns | 62.58 |    2.94 | 0.0601 |    1008 B |       42.00 |
