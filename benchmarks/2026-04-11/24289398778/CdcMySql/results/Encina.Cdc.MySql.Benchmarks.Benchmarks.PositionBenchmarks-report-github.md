```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean         | Error      | StdDev     | Median       | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|-----------:|-------------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.573 ns |  0.0614 ns |  0.0819 ns |     3.515 ns |   0.42 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.468 ns |  0.1711 ns |  0.2507 ns |     8.482 ns |   1.00 |    0.04 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   728.730 ns | 14.1501 ns | 19.8365 ns |   741.924 ns |  86.13 |    3.41 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,050.838 ns |  7.3690 ns | 10.0868 ns | 1,053.016 ns | 124.21 |    3.80 | 0.0286 |     504 B |       12.60 |
