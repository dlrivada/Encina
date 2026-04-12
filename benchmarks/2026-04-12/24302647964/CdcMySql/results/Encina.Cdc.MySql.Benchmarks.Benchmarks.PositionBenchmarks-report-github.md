```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean         | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.275 ns |  0.0039 ns |  0.0055 ns |   0.43 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.559 ns |  0.1179 ns |  0.1691 ns |   1.00 |    0.03 | 0.0016 |      40 B |        1.00 |
| FromBytes            |   870.050 ns |  3.9802 ns |  5.8342 ns | 115.15 |    2.63 | 0.0267 |     688 B |       17.20 |
| ToBytes              | 1,339.033 ns | 12.3433 ns | 18.4749 ns | 177.22 |    4.57 | 0.0191 |     504 B |       12.60 |
