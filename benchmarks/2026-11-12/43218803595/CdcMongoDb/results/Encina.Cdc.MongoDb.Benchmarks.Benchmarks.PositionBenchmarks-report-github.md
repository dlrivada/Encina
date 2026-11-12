```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 149.461 ns |  3.373 ns | 0.1849 ns | 16.54 |    0.16 | 0.0010 |      96 B |        4.00 |
| CreatePosition   |   9.038 ns |  1.892 ns | 0.1037 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 602.446 ns | 32.306 ns | 1.7708 ns | 66.66 |    0.68 | 0.0114 |    1024 B |       42.67 |
| ToBytes          | 564.059 ns | 11.011 ns | 0.6036 ns | 62.42 |    0.62 | 0.0114 |    1008 B |       42.00 |
