```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean         | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.528 ns | 0.1223 ns | 0.1754 ns |   0.36 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.735 ns | 0.2460 ns | 0.3367 ns |   1.00 |    0.05 | 0.0016 |      40 B |        1.00 |
| FromBytes            |   879.057 ns | 5.6070 ns | 7.8602 ns |  90.41 |    3.19 | 0.0267 |     688 B |       17.20 |
| ToBytes              | 1,367.178 ns | 5.1550 ns | 7.7157 ns | 140.61 |    4.87 | 0.0191 |     504 B |       12.60 |
