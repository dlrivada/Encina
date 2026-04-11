```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean         | Error      | StdDev     | Median       | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|-----------:|-------------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.971 ns |  0.0028 ns |  0.0040 ns |     3.970 ns |   0.47 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.397 ns |  0.1407 ns |  0.2063 ns |     8.335 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   790.356 ns |  4.1828 ns |  5.9989 ns |   791.546 ns |  94.17 |    2.33 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,152.077 ns | 10.4716 ns | 13.9793 ns | 1,141.790 ns | 137.27 |    3.63 | 0.0286 |     504 B |       12.60 |
