```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.994 ns |   0.7453 ns | 0.0409 ns |   0.51 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.781 ns |   1.4484 ns | 0.0794 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   747.321 ns | 110.7407 ns | 6.0701 ns |  96.05 |    1.09 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,174.419 ns |  73.5846 ns | 4.0334 ns | 150.94 |    1.41 | 0.0286 |     504 B |       12.60 |
