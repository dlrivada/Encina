```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.013 ns |  0.2773 ns | 0.0152 ns |   0.46 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.747 ns |  2.0228 ns | 0.1109 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   777.613 ns | 41.4967 ns | 2.2746 ns |  88.91 |    0.99 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,162.677 ns | 56.4845 ns | 3.0961 ns | 132.94 |    1.48 | 0.0286 |     504 B |       12.60 |
