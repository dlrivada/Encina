```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 139.15 ns |   9.317 ns | 0.511 ns | 13.69 |    0.18 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  10.17 ns |   2.755 ns | 0.151 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 635.27 ns | 163.904 ns | 8.984 ns | 62.50 |    1.11 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 572.38 ns |  73.087 ns | 4.006 ns | 56.31 |    0.80 | 0.0601 |    1008 B |       42.00 |
