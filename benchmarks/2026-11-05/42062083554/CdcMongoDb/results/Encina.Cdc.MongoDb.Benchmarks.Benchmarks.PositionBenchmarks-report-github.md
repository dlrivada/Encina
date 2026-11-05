```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 109.464 ns | 21.900 ns | 1.2004 ns | 16.71 |    0.22 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.550 ns |  1.205 ns | 0.0660 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 484.808 ns |  6.282 ns | 0.3443 ns | 74.02 |    0.64 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 472.631 ns | 30.984 ns | 1.6983 ns | 72.16 |    0.67 | 0.0601 |    1008 B |       42.00 |
