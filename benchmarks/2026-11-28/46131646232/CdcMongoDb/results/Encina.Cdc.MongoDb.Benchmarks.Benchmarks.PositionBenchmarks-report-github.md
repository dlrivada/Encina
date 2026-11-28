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
| ComparePositions | 103.962 ns |  5.973 ns | 0.3274 ns | 15.70 |    0.44 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.625 ns |  3.830 ns | 0.2099 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 475.516 ns | 61.245 ns | 3.3570 ns | 71.82 |    2.03 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 429.553 ns | 21.822 ns | 1.1962 ns | 64.88 |    1.80 | 0.0601 |    1008 B |       42.00 |
