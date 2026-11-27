```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.993 ns |   0.5626 ns | 0.0308 ns |   0.52 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.628 ns |   1.0082 ns | 0.0553 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   746.765 ns |  73.4689 ns | 4.0271 ns |  97.90 |    0.77 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,169.135 ns | 179.7042 ns | 9.8502 ns | 153.27 |    1.48 | 0.0286 |     504 B |       12.60 |
