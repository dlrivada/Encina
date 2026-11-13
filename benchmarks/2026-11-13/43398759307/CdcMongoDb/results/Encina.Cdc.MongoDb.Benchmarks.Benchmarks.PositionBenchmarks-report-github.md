```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.454 ns | 41.736 ns | 2.2877 ns | 16.46 |    0.26 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.167 ns |  1.017 ns | 0.0558 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 576.911 ns | 85.094 ns | 4.6643 ns | 70.64 |    0.65 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 540.773 ns | 41.125 ns | 2.2542 ns | 66.21 |    0.46 | 0.0601 |    1008 B |       42.00 |
