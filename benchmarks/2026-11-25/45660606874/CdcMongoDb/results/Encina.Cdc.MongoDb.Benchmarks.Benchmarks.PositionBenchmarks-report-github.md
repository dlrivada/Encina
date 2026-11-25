```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 108.039 ns |   1.237 ns | 0.0678 ns | 16.62 |    0.46 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.504 ns |   3.877 ns | 0.2125 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 447.059 ns |  49.759 ns | 2.7275 ns | 68.78 |    1.95 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 436.765 ns | 104.667 ns | 5.7371 ns | 67.20 |    2.03 | 0.0601 |    1008 B |       42.00 |
