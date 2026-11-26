```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.71GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.976 ns |  0.0934 ns | 0.0051 ns |   0.46 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.651 ns |  2.7634 ns | 0.1515 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   779.236 ns | 22.8753 ns | 1.2539 ns |  90.10 |    1.38 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,187.263 ns | 50.4328 ns | 2.7644 ns | 137.27 |    2.11 | 0.0286 |     504 B |       12.60 |
