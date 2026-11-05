```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 137.33 ns |  14.16 ns |  0.776 ns | 12.03 |    0.66 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  11.44 ns |  12.92 ns |  0.708 ns |  1.00 |    0.08 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 694.34 ns |  51.97 ns |  2.849 ns | 60.83 |    3.34 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 596.07 ns | 223.43 ns | 12.247 ns | 52.22 |    3.01 | 0.0601 |    1008 B |       42.00 |
