```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.898 ns |  0.0123 ns | 0.0007 ns |   0.40 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.689 ns |  2.2685 ns | 0.1243 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   797.299 ns | 66.7453 ns | 3.6585 ns |  82.30 |    0.97 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,174.544 ns | 71.8089 ns | 3.9361 ns | 121.24 |    1.39 | 0.0286 |     504 B |       12.60 |
