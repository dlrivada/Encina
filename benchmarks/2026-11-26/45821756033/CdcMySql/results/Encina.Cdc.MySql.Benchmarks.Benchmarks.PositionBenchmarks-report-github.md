```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.77GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.365 ns |   0.6092 ns | 0.0334 ns |   0.38 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.882 ns |   0.6375 ns | 0.0349 ns |   1.00 |    0.00 | 0.0016 |      40 B |        1.00 |
| FromBytes            |   855.532 ns |  42.8330 ns | 2.3478 ns |  96.32 |    0.40 | 0.0267 |     688 B |       17.20 |
| ToBytes              | 1,366.406 ns | 140.7119 ns | 7.7129 ns | 153.84 |    0.92 | 0.0191 |     504 B |       12.60 |
