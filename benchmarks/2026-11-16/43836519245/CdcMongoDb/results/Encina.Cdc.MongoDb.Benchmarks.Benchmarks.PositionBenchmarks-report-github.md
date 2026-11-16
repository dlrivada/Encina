```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 141.125 ns | 41.475 ns | 2.2734 ns | 16.56 |    0.25 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.524 ns |  1.068 ns | 0.0585 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 657.282 ns | 42.315 ns | 2.3194 ns | 77.11 |    0.52 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 569.071 ns | 16.982 ns | 0.9309 ns | 66.76 |    0.41 | 0.0601 |    1008 B |       42.00 |
