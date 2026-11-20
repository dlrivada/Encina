```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 108.538 ns | 36.254 ns | 1.9872 ns | 16.11 |    0.38 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.740 ns |  2.492 ns | 0.1366 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 480.267 ns | 16.290 ns | 0.8929 ns | 71.28 |    1.25 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 467.832 ns | 88.431 ns | 4.8472 ns | 69.43 |    1.36 | 0.0601 |    1008 B |       42.00 |
