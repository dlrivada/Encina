```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.976 ns |  0.0309 ns | 0.0017 ns |   0.43 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.351 ns |  0.4814 ns | 0.0264 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   785.459 ns |  6.3183 ns | 0.3463 ns |  84.00 |    0.21 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,166.121 ns | 34.3314 ns | 1.8818 ns | 124.71 |    0.35 | 0.0286 |     504 B |       12.60 |
