```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.973 ns |  0.0633 ns | 0.0035 ns |   0.51 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.784 ns |  1.1665 ns | 0.0639 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   768.972 ns | 70.2932 ns | 3.8530 ns |  98.79 |    0.83 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,162.484 ns | 27.2794 ns | 1.4953 ns | 149.35 |    1.08 | 0.0286 |     504 B |       12.60 |
