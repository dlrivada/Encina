```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.76GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 139.500 ns | 13.7808 ns | 0.7554 ns | 16.27 |    0.10 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.576 ns |  0.7669 ns | 0.0420 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 608.996 ns | 54.8653 ns | 3.0074 ns | 71.01 |    0.43 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 582.470 ns | 20.4677 ns | 1.1219 ns | 67.92 |    0.31 | 0.0601 |    1008 B |       42.00 |
