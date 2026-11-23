```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 106.311 ns |  29.274 ns | 1.6046 ns | 15.86 |    0.28 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.704 ns |   1.708 ns | 0.0936 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 467.022 ns |  99.892 ns | 5.4754 ns | 69.68 |    1.10 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 457.630 ns | 154.463 ns | 8.4666 ns | 68.27 |    1.37 | 0.0601 |    1008 B |       42.00 |
